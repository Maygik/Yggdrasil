using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Application.Abstractions;
using Yggdrasil.Domain.Project;
using Yggdrasil.Domain.Rigging;
using Yggdrasil.Domain.Scene;
using Yggdrasil.Infrastructure.Import;

using Matrix4x4 = Yggdrasil.Types.Matrix4x4;
using Vector3 = Yggdrasil.Types.Vector3;

namespace Yggdrasil.Infrastructure.Export
{
    public class ProportionTrickService : IProportionTrickService
    {
        private AnimationTemplateStore? _animationStore;

        private readonly struct BoneRotationSnapshot
        {
            public BoneRotationSnapshot(Quaternion localRotation, Quaternion worldRotation)
            {
                LocalRotation = localRotation;
                WorldRotation = worldRotation;
            }

            public Quaternion LocalRotation { get; }

            public Quaternion WorldRotation { get; }
        }

        public ProportionTrickService()
        {
            _animationStore = new AnimationTemplateStore();
            _animationStore.Init();
        }
        
        // Builds the reference animations for proportion trick
        public ProportionTrickResult Build(Project project)
        {
            var result = new ProportionTrickResult();


            // Import the base proportions model from packaged resources
            result.Proportions = SmdImporter.ImportSmd(_animationStore.Get("proportions"), "proportions");
            Console.WriteLine("Imported proportions model");

            // Debug:



            // Adjust the proportions bone positions to match the project's rig mapping
            // If the proportions model has a bone that is missing in the rig mapping, move that proportions bone to its child bone's position,
            // and rotate it to match the direction of the child bone in the rig mapping, effectively collapsing that bone
            // This allows 2 spine setups to work, as well as missing clavicles, etc. The proportions model is just a guide for the bone positions and rotations, so it doesn't need to have the same bone hierarchy as the project rig mapping
            // To find the correct position:
            // For each bone, rotate so that the child bone's position is in the correct direction
            // The length of the bone doesn't matter for proportion trick, only the direction, so we can just move the bone to the rig bone position
            // Then replicate the effective rotation of the bone by rotating it to face towards the rig bone's main child, or the center of influence of weights if it has no children
            // This should be good enough
            // Scale isn't needed, children will just be moved to the correct position, so the length of the bone doesn't matter, only the direction

            // For each bone in the proportions model, find the corresponding bone in the rig mapping, and adjust the proportions bone position and rotation to match the rig mapping bone position and rotation

            if (result.Proportions.RootBone == null)
                throw new Exception("Proportions model must have a root bone");
            if (project.Scene.RootBone == null)
                throw new Exception("Project model must have a root bone to run proportion trick");

            // Build a bone map for the original proportions model as well as the project model
            Dictionary<string, Bone> proportionsBoneMap = result.Proportions.RootBone.GetAllDescendantsAndSelf().ToDictionary(b => b.Name);
            Dictionary<string, Bone> rigBoneMap = project.Scene.RootBone.GetAllDescendantsAndSelf().ToDictionary(b => b.Name);
            IReadOnlyDictionary<string, BoneRotationSnapshot> originalArmRotationSnapshots = CaptureOriginalArmRotationSnapshots(proportionsBoneMap);
            IReadOnlyDictionary<string, BoneRotationSnapshot> directReferenceRotationSnapshots = CaptureDirectReferenceRotationSnapshots(proportionsBoneMap);

            // Root bone just gets moved to the rig equivalent

            var rigSlot = TryGetMappedRigSlotForBoneName(project, result.Proportions.RootBone.Name);
            if (rigSlot != null)
            {
                var rigBone = ResolveRigBoneName(rigSlot, rigBoneMap);
                if (rigBone == null)
                {
                    throw new Exception($"Root bone \"{result.Proportions.RootBone.Name}\" of proportions model must be mapped to a rig slot in the project for proportion trick to work");
                }
                result.Proportions.RootBone.WorldPosition = rigBoneMap[rigBone].WorldPosition;
                Console.WriteLine($"Set world position of proportions root bone \"{result.Proportions.RootBone.Name}\" to rig bone \"{rigBone}\"");
                Console.WriteLine($"Proportions root bone \"{result.Proportions.RootBone.Name}\" world position: {result.Proportions.RootBone.WorldPosition}, rig bone \"{rigBone}\" world position: {rigBoneMap[rigBone].WorldPosition}");
            }
            else
            {
                throw new Exception($"Root bone \"{result.Proportions.RootBone.Name}\" of proportions model must be mapped to a rig slot in the project for proportion trick to work");
            }

            HashSet<Bone> collapsedBones = new HashSet<Bone>();

            foreach (var proportionsBone in result.Proportions.RootBone.GetAllDescendantsAndSelf().Skip(1))
            {
                if (collapsedBones.Contains(proportionsBone))
                {
                    Console.WriteLine($"Skipping already-collapsed bone \"{proportionsBone.Name}\"");
                    continue;
                }

                try
                {
                    ProcessBone(proportionsBone, project, rigBoneMap, collapsedBones, originalArmRotationSnapshots);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error processing {proportionsBone.Name} for proportion trick: {ex.Message}");
                }
            }// Rotate both "ValveBiped.Bip01_L_Hand" and "ValveBiped.Bip01_R_Hand" such that they point between their respective Finger2 and Finger3 bone children
                // They should be the only thing to rotate
                // Children should not be moved by this change

            Console.WriteLine("Finished initial processing of proportions bones");

            Console.WriteLine("Applying additional adjustments to proportions armature");

            ApplyDedicatedArmSolve(project, proportionsBoneMap, originalArmRotationSnapshots);

            ApplyHandAndFingerAdjustments(proportionsBoneMap);

            // Toes should be rotated to be perpendicular to the ground
            // Ankles should be rotated by the same offset as the toes
            {
                // Rotate ValveBiped.Bip01_L_Toe0 and ValveBiped.Bip01_R_Toe0 to be perpendicular to the ground (pointing straight down)
                // Then rotate ValveBiped.Bip01_L_Foot and ValveBiped.Bip01_R_Foot by the same rotation delta as their respective toe bones
                // They should be rotated around their local z axis, which should be point roughly global X, so that the toes point as straight down as they can
                // Then rotate around the local x axis so that the soles of the feet are flat on the ground, which should be roughly global Y 
                // No bones should be moved, only rotated around the spot
                // Rotation should be applied world space so that it doesn't affect the position of the bones, only their rotation

                var leftToeBoneName = "ValveBiped.Bip01_L_Toe0";
                var rightToeBoneName = "ValveBiped.Bip01_R_Toe0";
                var leftFootBoneName = "ValveBiped.Bip01_L_Foot";
                var rightFootBoneName = "ValveBiped.Bip01_R_Foot";

                if (proportionsBoneMap.ContainsKey(leftToeBoneName) && proportionsBoneMap.ContainsKey(rightToeBoneName) && proportionsBoneMap.ContainsKey(leftFootBoneName) && proportionsBoneMap.ContainsKey(rightFootBoneName))
                {
                    var leftToe = proportionsBoneMap[leftToeBoneName];
                    var rightToe = proportionsBoneMap[rightToeBoneName];
                    var leftFoot = proportionsBoneMap[leftFootBoneName];
                    var rightFoot = proportionsBoneMap[rightFootBoneName];

                    // Save the original direction so we can fix the roll
                    var leftFootOriginalForward = leftFoot.WorldRotation.Rotate(new Vector3(1, 0, 0));
                    var rightFootOriginalForward = rightFoot.WorldRotation.Rotate(new Vector3(1, 0, 0));
                    var leftToeOriginalForward = leftToe.WorldRotation.Rotate(new Vector3(1, 0, 0));
                    var rightToeOriginalForward = rightToe.WorldRotation.Rotate(new Vector3(1, 0, 0));

                    Vector3 leftToeForward = leftToe.WorldRotation.Rotate(new Vector3(1, 0, 0));
                    Vector3 rightToeForward = rightToe.WorldRotation.Rotate(new Vector3(1, 0, 0));

                    Quaternion leftRotationDelta = Quaternion.FromToRotation(leftToeForward, new Vector3(0, -1, 0));
                    Quaternion rightRotationDelta = Quaternion.FromToRotation(rightToeForward, new Vector3(0, -1, 0));

                    float leftRotationAngle = Vector3.Angle(leftToeForward, new Vector3(0, -1, 0));
                    float rightRotationAngle = Vector3.Angle(rightToeForward, new Vector3(0, -1, 0));

                    Console.WriteLine($"Rotating left toe by {leftRotationAngle} degrees, right toe by {rightRotationAngle} degrees to be perpendicular to the ground");



                    var leftFootDescendantTransforms = new List<(Bone bone, Vector3 worldPos, Quaternion worldRot)>();
                    var rightFootDescendantTransforms = new List<(Bone bone, Vector3 worldPos, Quaternion worldRot)>();
                    var leftToeDescendantTransforms = new List<(Bone bone, Vector3 worldPos, Quaternion worldRot)>();
                    var rightToeDescendantTransforms = new List<(Bone bone, Vector3 worldPos, Quaternion worldRot)>();

                    foreach (var child in leftToe.Children)
                    {
                        if (child is Bone childBone)
                        {
                            CollectDescendantTransforms(childBone, leftToeDescendantTransforms);
                        }
                    }
                    foreach (var child in rightToe.Children)
                    {
                        if (child is Bone childBone)
                        {
                            CollectDescendantTransforms(childBone, rightToeDescendantTransforms);
                        }
                    }
                    List<string> leftFootIgnore = new();
                    leftFootIgnore.Add(leftToeBoneName);
                    foreach (var child in leftFoot.Children)
                    {
                        if (child is Bone childBone)
                        {
                            CollectDescendantTransforms(childBone, leftFootDescendantTransforms, leftFootIgnore);
                        }
                    }
                    List<string> rightFootIgnore = new();
                    rightFootIgnore.Add(rightToeBoneName);
                    foreach (var child in rightFoot.Children)
                    {
                        if (child is Bone childBone)
                        {
                            CollectDescendantTransforms(childBone, rightFootDescendantTransforms, rightFootIgnore);
                        }
                    }


                    leftFoot.WorldRotation = leftRotationDelta * leftFoot.WorldRotation;
                    rightFoot.WorldRotation = rightRotationDelta * rightFoot.WorldRotation;

                    // Restore descendant transforms to prevent movement
                    foreach (var (bone, worldPos, worldRot) in leftFootDescendantTransforms)
                    {
                        bone.WorldPosition = worldPos;
                        bone.WorldRotation = worldRot;
                    }
                    foreach (var (bone, worldPos, worldRot) in rightFootDescendantTransforms)
                    {
                        bone.WorldPosition = worldPos;
                        bone.WorldRotation = worldRot;
                    }

                    leftToe.WorldRotation = leftRotationDelta * leftToe.WorldRotation;
                    rightToe.WorldRotation = rightRotationDelta * rightToe.WorldRotation;

                    foreach (var (bone, worldPos, worldRot) in leftToeDescendantTransforms)
                    {
                        bone.WorldPosition = worldPos;
                        bone.WorldRotation = worldRot;
                    }
                    foreach (var (bone, worldPos, worldRot) in rightToeDescendantTransforms)
                    {
                        bone.WorldPosition = worldPos;
                        bone.WorldRotation = worldRot;
                    }

                    // Additional rotation around local X axis to make feet more vertical
                    // Get the current up vector (local Y) in world space
                    Vector3 leftFootUp = leftFoot.WorldRotation.Rotate(new Vector3(0, 1, 0));
                    Vector3 rightFootUp = rightFoot.WorldRotation.Rotate(new Vector3(0, 1, 0));

                    // Project onto the XZ plane to get the forward component
                    Vector3 leftFootUpFlat = new Vector3(leftFootUp.X, 0, leftFootUp.Z).Normalised();
                    Vector3 rightFootUpFlat = new Vector3(rightFootUp.X, 0, rightFootUp.Z).Normalised();

                    // Calculate rotation needed to align with world up (0, 1, 0)
                    Quaternion leftXRotation = Quaternion.FromToRotation(leftFootUpFlat, new Vector3(0, 0, -1));
                    Quaternion rightXRotation = Quaternion.FromToRotation(rightFootUpFlat, new Vector3(0, 0, -1));

                    // Store descendant transforms again before applying X rotation
                    leftFootDescendantTransforms.Clear();
                    rightFootDescendantTransforms.Clear();
                    foreach (var child in leftFoot.Children)
                    {
                        if (child is Bone childBone)
                        {
                            CollectDescendantTransforms(childBone, leftFootDescendantTransforms, leftFootIgnore);
                        }
                    }
                    foreach (var child in rightFoot.Children)
                    {
                        if (child is Bone childBone)
                        {
                            CollectDescendantTransforms(childBone, rightFootDescendantTransforms, rightFootIgnore);
                        }
                    }

                    // Apply X-axis rotation
                    leftFoot.WorldRotation = leftXRotation * leftFoot.WorldRotation;
                    rightFoot.WorldRotation = rightXRotation * rightFoot.WorldRotation;

                    // Restore descendant transforms
                    foreach (var (bone, worldPos, worldRot) in leftFootDescendantTransforms)
                    {
                        bone.WorldPosition = worldPos;
                        bone.WorldRotation = worldRot;
                    }
                    foreach (var (bone, worldPos, worldRot) in rightFootDescendantTransforms)
                    {
                        bone.WorldPosition = worldPos;
                        bone.WorldRotation = worldRot;
                    }

                    leftToe.WorldRotation = leftXRotation * leftToe.WorldRotation;
                    rightToe.WorldRotation = rightXRotation * rightToe.WorldRotation;

                    foreach (var (bone, worldPos, worldRot) in leftToeDescendantTransforms)
                    {
                        bone.WorldPosition = worldPos;
                        bone.WorldRotation = worldRot;
                    }
                    foreach (var (bone, worldPos, worldRot) in rightToeDescendantTransforms)
                    {
                        bone.WorldPosition = worldPos;
                        bone.WorldRotation = worldRot;
                    }


                    // Now rotate around global y until we're as close to the original angle as possible

                    // Project these onto the XZ plane (removing Y component) to get the "flat" forward direction
                    var leftToeForwardFlat = new Vector3(leftToeOriginalForward.X, 0, leftToeOriginalForward.Z);
                    var rightToeForwardFlat = new Vector3(rightToeOriginalForward.X, 0, rightToeOriginalForward.Z);


                    if (leftToeForwardFlat.Length() > 0.001f)
                    {
                        leftToeForwardFlat = leftToeForwardFlat.Normalised();
                        float yRotationAngle = (float)Math.Atan2(leftToeForwardFlat.X, leftToeForwardFlat.Z);
                        Quaternion leftYRotation = Quaternion.FromAngleAxis(yRotationAngle, new Vector3(0, 0, 1));
                        var leftToeDescendants = new List<(Bone bone, Vector3 worldPos, Quaternion worldRot)>();
                        foreach (var child in leftToe.Children)
                        {
                            if (child is Bone childBone)
                            {
                                CollectDescendantTransforms(childBone, leftToeDescendants);
                            }
                        }
                        leftToe.WorldRotation = leftYRotation * leftToe.WorldRotation;
                        foreach (var (bone, worldPos, worldRot) in leftToeDescendants)
                        {
                            bone.WorldPosition = worldPos;
                            bone.WorldRotation = worldRot;
                        }
                    }

                    if (rightToeForwardFlat.Length() > 0.001f)
                    {
                        rightToeForwardFlat = rightToeForwardFlat.Normalised();
                        float yRotationAngle = (float)Math.Atan2(rightToeForwardFlat.X, rightToeForwardFlat.Z);
                        Quaternion rightYRotation = Quaternion.FromAngleAxis(yRotationAngle, new Vector3(0, 1, 0));
                        var rightToeDescendants = new List<(Bone bone, Vector3 worldPos, Quaternion worldRot)>();
                        foreach (var child in rightToe.Children)
                        {
                            if (child is Bone childBone)
                            {
                                CollectDescendantTransforms(childBone, rightToeDescendants);
                            }
                        }
                        rightToe.WorldRotation = rightYRotation * rightToe.WorldRotation;
                        foreach (var (bone, worldPos, worldRot) in rightToeDescendants)
                        {
                            bone.WorldPosition = worldPos;
                            bone.WorldRotation = worldRot;
                        }
                    }


                    Console.WriteLine($"Rotated toes and feet to be perpendicular to the ground and adjusted for vertical alignment");
                }
                else
                {
                    Console.WriteLine($"Could not find toe or foot bones for additional adjustments");
                }
            }

            ApplyDirectReferenceRotations(proportionsBoneMap, directReferenceRotationSnapshots);

            // Neck should point at the head with a slight forward tilt, and the head's local X axis
            // should be tilted forwards from world up to match the reference pose.
            {
                string neckBoneName = "ValveBiped.Bip01_Neck1";
                string headBoneName = "ValveBiped.Bip01_Head1";
                Bone? neckBone = proportionsBoneMap.GetValueOrDefault(neckBoneName);
                Bone? headBone = proportionsBoneMap.GetValueOrDefault(headBoneName);
                const float headForwardTiltDegrees = 15f;
                float headForwardTiltRadians = headForwardTiltDegrees * (float)Math.PI / 180f;

                if (neckBone != null && headBone != null)
                {
                    Vector3 directionToHead = (headBone.WorldPosition - neckBone.WorldPosition).Normalised();
                    Vector3 neckForwardDirection =
                        ((directionToHead * (float)Math.Cos(headForwardTiltRadians))
                        + (new Vector3(0, -1, 0) * (float)Math.Sin(headForwardTiltRadians)))
                        .Normalised();
                    AlignBoneLocalXAxisWithWorldDirection(
                        neckBone,
                        neckForwardDirection,
                        0.01f,
                        $"neck bone \"{neckBoneName}\" to point toward head \"{headBoneName}\" with a slight forward tilt");
                }
                else
                {
                    Console.WriteLine($"Could not find both neck bone \"{neckBoneName}\" and head bone \"{headBoneName}\" for neck adjustment");
                }

                if (headBone != null)
                {
                    // The desired head frame is rotated 90 degrees around world X from the
                    // original solve, so the "up/forward" basis becomes Z-up and -Y-forward.
                    Vector3 worldUp = new Vector3(0, 0, 1);
                    Vector3 worldForward = new Vector3(0, -1, 0);
                    Vector3 targetHeadXAxis =
                        ((worldUp * (float)Math.Cos(headForwardTiltRadians))
                        + (worldForward * (float)Math.Sin(headForwardTiltRadians)))
                        .Normalised();

                    AlignBoneLocalXAxisWithWorldDirection(
                        headBone,
                        targetHeadXAxis,
                        0.01f,
                        $"head bone \"{headBoneName}\" so local X is {headForwardTiltDegrees} degrees forward from world up");
                }
                else
                {
                    Console.WriteLine($"Could not find head bone \"{headBoneName}\" for head adjustment");
                }
            }


            Console.WriteLine("Adding additional bones to proportions armature");
            var finalProportionsBones = result.Proportions.RootBone.GetAllDescendantsAndSelf().ToDictionary(b => b.Name);
            int initialBoneCount = finalProportionsBones.Count;
            HashSet<string> warnedFallbackAttachments = new();
            Bone? EnsureBoneExists(Bone bone)
            {
                if (finalProportionsBones.TryGetValue(bone.Name, out var existingBone))
                {
                    return existingBone;
                }

                var missingChain = new Stack<Bone>();
                Bone? current = bone;
                Bone? attachmentParent = null;

                while (current != null)
                {
                    if (finalProportionsBones.TryGetValue(current.Name, out var foundBone))
                    {
                        attachmentParent = foundBone;
                        break;
                    }

                    missingChain.Push(current);
                    current = current.Parent as Bone;
                }

                if (attachmentParent == null)
                {
                    attachmentParent = result.Proportions.RootBone;
                    if (attachmentParent == null)
                    {
                        Console.WriteLine($"Could not add bone \"{bone.Name}\" to proportions model because there is no valid attachment root");
                        return null;
                    }

                    if (warnedFallbackAttachments.Add(bone.Name))
                    {
                        Console.WriteLine($"Attaching missing bone chain for \"{bone.Name}\" at proportions root \"{attachmentParent.Name}\"");
                    }
                }

                while (missingChain.Count > 0)
                {
                    var missingBone = missingChain.Pop();
                    if (finalProportionsBones.TryGetValue(missingBone.Name, out var alreadyAddedBone))
                    {
                        attachmentParent = alreadyAddedBone;
                        continue;
                    }

                    Bone newBone = new(missingBone.Name);
                    attachmentParent.AddChild(newBone);
                    newBone.WorldMatrix = missingBone.WorldMatrix;
                    finalProportionsBones[missingBone.Name] = newBone;
                    Console.WriteLine($"Added bone \"{missingBone.Name}\" to proportions model under parent \"{attachmentParent.Name}\"");
                    attachmentParent = newBone;
                }

                return attachmentParent;
            }

            void AddBones(Bone bone)
            {
                // Add any bones that are in the original proportions model but not in the final proportions model after processing
                if (!finalProportionsBones.ContainsKey(bone.Name))
                {
                    EnsureBoneExists(bone);
                }

                // Recursively add all children
                foreach (var child in bone.Children)
                {
                    if (child is Bone childBone)
                    {
                        AddBones(childBone);
                    }
                }
            }
            AddBones(project.Scene.RootBone);
            int finalBoneCount = finalProportionsBones.Count;
            Console.WriteLine($"Finished adding additional bones to proportions armature (added {finalBoneCount - initialBoneCount} bones)");


            // Import the reference animations from packaged resources
            result.ReferenceMale = SmdImporter.ImportSmd(_animationStore.Get("reference_male"), "reference_male");
            result.ReferenceFemale = SmdImporter.ImportSmd(_animationStore.Get("reference_female"), "reference_female");

            // Then copy the rotations of the proportions bones to the reference bones
            // Debug console down the children chain until the end for both references
            /*
            Console.WriteLine("Proportions bone hierarchy:");
            proportions.RootBone.PrintBoneHierarchy();
            Console.WriteLine("Male reference bone hierarchy:");
            reference_male?.RootBone?.PrintBoneHierarchy();
            Console.WriteLine("Female reference bone hierarchy:");
            reference_female?.RootBone?.PrintBoneHierarchy();
            */

            // Make sure they all have root bones
            if (result.Proportions.RootBone == null)
                throw new Exception("Proportions model must have a root bone");
            if (result.ReferenceMale.RootBone == null)
                throw new Exception("Reference male model must have a root bone");
            if (result.ReferenceFemale.RootBone == null)
                throw new Exception("Reference female model must have a root bone");

            // Copy the solved proportions rotations to the reference bones.
            // The reference animations stay in their baked base pose and are used as the subtract baseline.
            var proportionsBoneDict = result.Proportions.RootBone.GetAllDescendantsAndSelf().ToDictionary(b => b.Name);
            var referenceMaleBoneDict = result.ReferenceMale.RootBone.GetAllDescendantsAndSelf().ToDictionary(b => b.Name);
            var referenceFemaleBoneDict = result.ReferenceFemale.RootBone.GetAllDescendantsAndSelf().ToDictionary(b => b.Name);

            Console.WriteLine("Copying rotations from proportions bones to reference bones");

            foreach (var boneName in proportionsBoneDict.Keys)
            {
                if (referenceMaleBoneDict.ContainsKey(boneName))
                {
                    referenceMaleBoneDict[boneName].WorldRotation = proportionsBoneDict[boneName].WorldRotation;
                }
                if (referenceFemaleBoneDict.ContainsKey(boneName))
                {
                    referenceFemaleBoneDict[boneName].WorldRotation = proportionsBoneDict[boneName].WorldRotation;
                }
            }
            
            Console.WriteLine($"Finished copying rotations to {proportionsBoneDict.Count} reference bones");


            return result;
        }


        private static void ProcessBone(
            Bone proportionsBone,
            Project project,
            Dictionary<string, Bone> rigBoneMap,
            HashSet<Bone> collapsedBones,
            IReadOnlyDictionary<string, BoneRotationSnapshot> originalArmRotationSnapshots)
        {
            Console.WriteLine($"\n=== Processing bone: {proportionsBone.Name} ===");

            // Find the corresponding rig bone
            var rigSlot = TryGetMappedRigSlotForBoneName(project, proportionsBone.Name);
            if (rigSlot == null )
            {
                Console.WriteLine($"Proportions bone {proportionsBone.Name} does not have a corresponding rig slot, skipping");
                return;
            }
            var rigBoneName = rigSlot.AssignedBone;
            if (rigBoneName == null || !rigBoneMap.ContainsKey(rigBoneName))
            {
                rigBoneName = ResolveRigBoneName(rigSlot, rigBoneMap);
            }

            if (rigBoneName == null)
            {
                // Proportions bone is mapped to a rig slot, but that rig slot doesn't resolve to a rig bone
                // Collapse this proportions bone into its first mapped descendant bone, if it has one, otherwise just skip it
                Bone? firstMappedDescendant = FindFirstMappedDescendant(proportionsBone, project);
                if (firstMappedDescendant != null)
                {
                    ProcessBone(firstMappedDescendant, project, rigBoneMap, collapsedBones, originalArmRotationSnapshots);
                    proportionsBone.WorldMatrix = firstMappedDescendant.WorldMatrix;
            firstMappedDescendant.LocalPosition = Vector3.Zero;
                    firstMappedDescendant.LocalRotation = Quaternion.Identity;
            firstMappedDescendant.LocalScale = Vector3.One;
                    collapsedBones.Add(firstMappedDescendant);
                    Console.WriteLine($"Collapsed \"{proportionsBone.Name}\" into first child \"{firstMappedDescendant.Name}\"");
                }
                else
                {
                    Console.WriteLine($"Proportions bone \"{proportionsBone.Name}\" has no assigned rig bone and no mapped descendants, skipping");
                }
                return;
            }

            if (!rigBoneMap.ContainsKey(rigBoneName))
            {
                Console.WriteLine($"Rig bone \"{rigBoneName}\" mapped from proportions bone \"{proportionsBone.Name}\" does not exist in the rig bone map, skipping");
                return;
            }

            var rigBone = rigBoneMap[rigBoneName];

            // Move the proportions bone to the rig bone position
            proportionsBone.WorldPosition = rigBone.WorldPosition;
            Console.WriteLine($"Set WorldPosition to {rigBone.WorldPosition}");

            
            // Get the vector from this proportions bone to its first mapped descendant bone
            Bone? firstMappedProportionsDescendant = FindFirstMappedDescendant(proportionsBone, project);
            if (firstMappedProportionsDescendant == null)
            {
                Console.WriteLine($"No mapped descendant bones, skipping rotation");
                return;
            }

            if (IsDedicatedArmSolveBone(proportionsBone.Name)
                && originalArmRotationSnapshots.ContainsKey(proportionsBone.Name))
            {
                Console.WriteLine($"Skipping generic rotation for arm bone \"{proportionsBone.Name}\" so the dedicated arm solve can handle it");
                return;
            }

            if (IsDirectReferenceRotationBone(proportionsBone.Name))
            {
                Console.WriteLine($"Skipping generic rotation for reference-driven bone \"{proportionsBone.Name}\" so its rotation can be copied from the reference armature");
                return;
            }
            
            Bone? firstMappedRigDescendant = FindFirstMappedDescendant(rigBone, project);
            if (firstMappedRigDescendant == null)
            {
                Console.WriteLine($"Rig bone \"{rigBone.Name}\" has no mapped descendant bones, skipping rotation");
                return;
            }

            Vector3 proportionsDirection = (firstMappedProportionsDescendant.WorldPosition - proportionsBone.WorldPosition).Normalised();
            Vector3 rigDirection = (firstMappedRigDescendant.WorldPosition - rigBone.WorldPosition).Normalised();

            Console.WriteLine($"Proportions direction to {firstMappedProportionsDescendant.Name}: {proportionsDirection}");
            Console.WriteLine($"Rig direction to {firstMappedRigDescendant.Name}: {rigDirection}");

            // Calculate the world-space rotation needed
            Quaternion worldRotationDelta = Quaternion.FromToRotation(proportionsDirection, rigDirection);
            
            // Apply the rotation in world space
            Quaternion newWorldRotation = worldRotationDelta * proportionsBone.WorldRotation;
            proportionsBone.WorldRotation = newWorldRotation;
            
            Console.WriteLine($"Applied world rotation, new LocalRotation: {proportionsBone.LocalRotation}");
        }

        // Check if any child bone (recursively) is mapped to a rig slot
        private static bool HasMappedChild(Bone bone, Project project)
        {
            if (bone.Children.Count == 0)
                return false;
            foreach (var child in bone.Children)
            {
                if (child is Bone childBone)
                {
                    if (childBone.Name != null && TryGetMappedRigSlotForBoneName(project, childBone.Name) != null)
                    {
                        return true;
                    }
                    else if (HasMappedChild(childBone, project))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Find the first child bone (recursively) that is mapped to a rig slot, or null if there are no mapped descendants
        private static Bone? FindFirstMappedDescendant(Bone bone, Project project)
        {
            foreach (var child in bone.Children)
            {
                if (child is Bone childBone)
                {
                    if (childBone.Name != null && TryGetMappedRigSlotForBoneName(project, childBone.Name) != null)
                    {
                        return childBone;
                    }
                    else
                    {
                        var descendant = FindFirstMappedDescendant(childBone, project);
                        if (descendant != null)
                            return descendant;
                    }
                }
            }
            return null;
        }

        private static RigSlot? TryGetMappedRigSlotForBoneName(Project project, string boneName)
        {
            ArgumentNullException.ThrowIfNull(project);

            if (string.IsNullOrWhiteSpace(boneName))
            {
                return null;
            }

            RigSlot? rigSlot = project.RigMapping.TryGetRigSlotFromName(boneName);
            if (rigSlot != null)
            {
                return rigSlot;
            }

            foreach (RigSlot slot in project.RigMapping.GetSlots())
            {
                if (string.Equals(slot.AssignedBone, boneName, StringComparison.OrdinalIgnoreCase))
                {
                    return slot;
                }
            }

            return null;
        }

        // Capture the original arm rotations from the packaged proportions model so the arm solve can preserve its roll.
        private static IReadOnlyDictionary<string, BoneRotationSnapshot> CaptureOriginalArmRotationSnapshots(
            IReadOnlyDictionary<string, Bone> proportionsBoneMap)
        {
            ArgumentNullException.ThrowIfNull(proportionsBoneMap);

            string[] armSnapshotBoneNames =
            {
                "ValveBiped.Bip01_L_Clavicle",
                "ValveBiped.Bip01_L_UpperArm",
                "ValveBiped.Bip01_L_Forearm",
                "ValveBiped.Bip01_L_Hand",
                "ValveBiped.Bip01_R_Clavicle",
                "ValveBiped.Bip01_R_UpperArm",
                "ValveBiped.Bip01_R_Forearm",
                "ValveBiped.Bip01_R_Hand"
            };

            return CaptureRotationSnapshots(proportionsBoneMap, armSnapshotBoneNames, "original arm");
        }

        private static IReadOnlyDictionary<string, BoneRotationSnapshot> CaptureDirectReferenceRotationSnapshots(
            IReadOnlyDictionary<string, Bone> proportionsBoneMap)
        {
            ArgumentNullException.ThrowIfNull(proportionsBoneMap);

            string[] boneNames =
            {
                "ValveBiped.Bip01_Spine",
                "ValveBiped.Bip01_Spine1",
                "ValveBiped.Bip01_Spine2",
                "ValveBiped.Bip01_Spine4",
                "ValveBiped.Bip01_L_Clavicle",
                "ValveBiped.Bip01_R_Clavicle"
            };

            return CaptureRotationSnapshots(proportionsBoneMap, boneNames, "direct reference");
        }

        private static IReadOnlyDictionary<string, BoneRotationSnapshot> CaptureRotationSnapshots(
            IReadOnlyDictionary<string, Bone> proportionsBoneMap,
            IEnumerable<string> boneNames,
            string snapshotLabel)
        {
            ArgumentNullException.ThrowIfNull(proportionsBoneMap);
            ArgumentNullException.ThrowIfNull(boneNames);
            ArgumentException.ThrowIfNullOrWhiteSpace(snapshotLabel);

            Dictionary<string, BoneRotationSnapshot> snapshots = new(StringComparer.OrdinalIgnoreCase);
            foreach (string boneName in boneNames)
            {
                if (!proportionsBoneMap.TryGetValue(boneName, out Bone? bone))
                {
                    Console.WriteLine($"Could not capture {snapshotLabel} rotation snapshot for bone \"{boneName}\"");
                    continue;
                }

                snapshots[boneName] = new BoneRotationSnapshot(bone.LocalRotation, bone.WorldRotation);
            }

            return snapshots;
        }

        // Run a dedicated solve for the arm chains so they can inherit swing from the import but keep roll from the original proportions pose.
        private static void ApplyDedicatedArmSolve(
            Project project,
            IReadOnlyDictionary<string, Bone> proportionsBoneMap,
            IReadOnlyDictionary<string, BoneRotationSnapshot> originalArmRotationSnapshots)
        {
            ArgumentNullException.ThrowIfNull(project);
            ArgumentNullException.ThrowIfNull(proportionsBoneMap);
            ArgumentNullException.ThrowIfNull(originalArmRotationSnapshots);

            Console.WriteLine("Applying dedicated arm solve");

            string[] leftArmBoneNames =
            {
                "ValveBiped.Bip01_L_UpperArm",
                "ValveBiped.Bip01_L_Forearm"
            };
            string[] rightArmBoneNames =
            {
                "ValveBiped.Bip01_R_UpperArm",
                "ValveBiped.Bip01_R_Forearm"
            };

            ApplyDedicatedArmSolveForChain("left", leftArmBoneNames, project, proportionsBoneMap, originalArmRotationSnapshots);
            ApplyDedicatedArmSolveForChain("right", rightArmBoneNames, project, proportionsBoneMap, originalArmRotationSnapshots);
        }

        // Solve a single arm chain in parent-to-child order so each bone can aim at its mapped child.
        private static void ApplyDedicatedArmSolveForChain(
            string sideLabel,
            IEnumerable<string> boneNames,
            Project project,
            IReadOnlyDictionary<string, Bone> proportionsBoneMap,
            IReadOnlyDictionary<string, BoneRotationSnapshot> originalArmRotationSnapshots)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sideLabel);
            ArgumentNullException.ThrowIfNull(boneNames);
            ArgumentNullException.ThrowIfNull(project);
            ArgumentNullException.ThrowIfNull(proportionsBoneMap);
            ArgumentNullException.ThrowIfNull(originalArmRotationSnapshots);

            foreach (string boneName in boneNames)
            {
                if (!proportionsBoneMap.TryGetValue(boneName, out Bone? bone))
                {
                    Console.WriteLine($"Skipping {sideLabel} arm solve for \"{boneName}\" because the bone was not found");
                    continue;
                }

                if (!originalArmRotationSnapshots.TryGetValue(boneName, out BoneRotationSnapshot originalSnapshot))
                {
                    Console.WriteLine($"Skipping {sideLabel} arm solve for \"{boneName}\" because the original rotation snapshot was missing");
                    continue;
                }

                Bone? targetBone = FindFirstMappedDescendant(bone, project);
                if (targetBone == null)
                {
                    Console.WriteLine($"Skipping {sideLabel} arm solve for \"{boneName}\" because no mapped child target was found");
                    continue;
                }

                if (!TryBuildArmTargetWorldRotation(bone, targetBone, originalSnapshot, out Quaternion targetWorldRotation, out string failureReason))
                {
                    Console.WriteLine($"Skipping {sideLabel} arm solve for \"{boneName}\": {failureReason}");
                    continue;
                }

                // Preserve the current solved chain positions while replacing only this bone's rotation.
                var descendantTransforms = CaptureDescendantTransforms(bone);
                bone.WorldRotation = targetWorldRotation;
                RestoreDescendantTransforms(descendantTransforms);
                Console.WriteLine($"Applied dedicated {sideLabel} arm solve to \"{boneName}\" using target \"{targetBone.Name}\"");
            }
        }

        private static void ApplyDirectReferenceRotations(
            IReadOnlyDictionary<string, Bone> proportionsBoneMap,
            IReadOnlyDictionary<string, BoneRotationSnapshot> directReferenceRotationSnapshots)
        {
            ArgumentNullException.ThrowIfNull(proportionsBoneMap);
            ArgumentNullException.ThrowIfNull(directReferenceRotationSnapshots);

            string[] boneNamesInApplyOrder =
            {
                "ValveBiped.Bip01_Spine",
                "ValveBiped.Bip01_Spine1",
                "ValveBiped.Bip01_Spine2",
                "ValveBiped.Bip01_Spine4",
                "ValveBiped.Bip01_L_Clavicle",
                "ValveBiped.Bip01_R_Clavicle"
            };

            foreach (string boneName in boneNamesInApplyOrder)
            {
                if (!proportionsBoneMap.TryGetValue(boneName, out Bone? bone))
                {
                    Console.WriteLine($"Skipping direct reference rotation copy for \"{boneName}\" because the bone was not found");
                    continue;
                }

                if (!directReferenceRotationSnapshots.TryGetValue(boneName, out BoneRotationSnapshot snapshot))
                {
                    Console.WriteLine($"Skipping direct reference rotation copy for \"{boneName}\" because the snapshot was missing");
                    continue;
                }

                var descendantTransforms = CaptureDescendantTransforms(bone);
                bone.LocalRotation = snapshot.LocalRotation;
                RestoreDescendantTransforms(descendantTransforms);
                Console.WriteLine($"Copied local rotation for \"{boneName}\" from the reference armature");
            }
        }

        private static void ApplyHandAndFingerAdjustments(IReadOnlyDictionary<string, Bone> proportionsBoneMap)
        {
            ArgumentNullException.ThrowIfNull(proportionsBoneMap);

            ApplyHandAndFingerAdjustmentsForSide("L", proportionsBoneMap);
            ApplyHandAndFingerAdjustmentsForSide("R", proportionsBoneMap);
        }

        private static void ApplyHandAndFingerAdjustmentsForSide(
            string sideCode,
            IReadOnlyDictionary<string, Bone> proportionsBoneMap)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sideCode);
            ArgumentNullException.ThrowIfNull(proportionsBoneMap);

            string handBoneName = $"ValveBiped.Bip01_{sideCode}_Hand";
            string middleFingerName = $"ValveBiped.Bip01_{sideCode}_Finger2";
            string ringFingerName = $"ValveBiped.Bip01_{sideCode}_Finger3";

            if (!proportionsBoneMap.TryGetValue(handBoneName, out Bone? handBone))
            {
                Console.WriteLine($"Could not find hand bone \"{handBoneName}\" for wrist adjustment");
                return;
            }

            if (proportionsBoneMap.TryGetValue(middleFingerName, out Bone? middleFinger)
                && proportionsBoneMap.TryGetValue(ringFingerName, out Bone? ringFinger))
            {
                Vector3 averageFingerPosition = (middleFinger.WorldPosition + ringFinger.WorldPosition) * 0.5f;
                Vector3 directionToTarget = (averageFingerPosition - handBone.WorldPosition).Normalised();
                AlignBoneLocalXAxisWithWorldDirection(
                    handBone,
                    directionToTarget,
                    0.1f,
                    $"{sideCode} hand bone \"{handBoneName}\" to point between middle and ring fingers");
            }
            else
            {
                Console.WriteLine($"Could not find both \"{middleFingerName}\" and \"{ringFingerName}\" for hand aim adjustment");
            }

            if (!TryGetPreferredHandSpreadDirection(sideCode, proportionsBoneMap, out Vector3 spreadDirection, out string spreadPairLabel))
            {
                Console.WriteLine($"Could not find a preferred finger spread pair for hand roll on side \"{sideCode}\"; leaving hand and finger roll unchanged");
                return;
            }

            Vector3 handForward = handBone.WorldRotation.Rotate(new Vector3(1, 0, 0)).Normalised();
            Vector3 targetRollAxis = Vector3.Cross(handForward, spreadDirection).Normalised();
            if (targetRollAxis.LengthSquared() <= 1e-8f)
            {
                Console.WriteLine($"Computed a degenerate target roll axis for hand bone \"{handBoneName}\" using pair {spreadPairLabel}");
                return;
            }

            Quaternion handRollDelta;
            float handRollAngle;
            string handRollDescription;
            if (!TryCreateTwistRotationToAlignSecondaryAxis(
                handBone,
                targetRollAxis,
                out handRollDelta,
                out handRollAngle,
                out handRollDescription))
            {
                Console.WriteLine($"Leaving hand roll unchanged for \"{handBoneName}\": {handRollDescription}");
                return;
            }

            ApplyWorldRotationPreservingDescendants(
                handBone,
                handRollDelta * handBone.WorldRotation);
            Console.WriteLine($"Adjusted roll of hand bone \"{handBoneName}\" using spread pair {spreadPairLabel}");

            string[][] fingerChains =
            {
                new[] { $"ValveBiped.Bip01_{sideCode}_Finger1", $"ValveBiped.Bip01_{sideCode}_Finger11", $"ValveBiped.Bip01_{sideCode}_Finger12" },
                new[] { $"ValveBiped.Bip01_{sideCode}_Finger2", $"ValveBiped.Bip01_{sideCode}_Finger21", $"ValveBiped.Bip01_{sideCode}_Finger22" },
                new[] { $"ValveBiped.Bip01_{sideCode}_Finger3", $"ValveBiped.Bip01_{sideCode}_Finger31", $"ValveBiped.Bip01_{sideCode}_Finger32" },
                new[] { $"ValveBiped.Bip01_{sideCode}_Finger4", $"ValveBiped.Bip01_{sideCode}_Finger41", $"ValveBiped.Bip01_{sideCode}_Finger42" }
            };

            foreach (string[] fingerChain in fingerChains)
            {
                foreach (string fingerBoneName in fingerChain)
                {
                    if (!proportionsBoneMap.TryGetValue(fingerBoneName, out Bone? fingerBone))
                    {
                        Console.WriteLine($"Skipping propagated roll adjustment for missing finger bone \"{fingerBoneName}\"");
                        continue;
                    }

                    Vector3 fingerForwardAxis = fingerBone.WorldRotation.Rotate(new Vector3(1, 0, 0)).Normalised();
                    if (fingerForwardAxis.LengthSquared() <= 1e-8f)
                    {
                        Console.WriteLine($"Skipping propagated roll adjustment for finger bone \"{fingerBoneName}\" because its forward axis was zero");
                        continue;
                    }

                    Quaternion fingerRollDelta = Quaternion.FromAngleAxis(handRollAngle, fingerForwardAxis);
                    ApplyWorldRotationPreservingDescendants(
                        fingerBone,
                        fingerRollDelta * fingerBone.WorldRotation);
                }
            }

            Console.WriteLine($"Propagated hand roll adjustment through non-thumb fingers on side \"{sideCode}\"");
        }

        private static bool TryGetPreferredHandSpreadDirection(
            string sideCode,
            IReadOnlyDictionary<string, Bone> proportionsBoneMap,
            out Vector3 spreadDirection,
            out string spreadPairLabel)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sideCode);
            ArgumentNullException.ThrowIfNull(proportionsBoneMap);

            spreadDirection = Vector3.Zero;
            spreadPairLabel = string.Empty;

            (string firstBoneName, string secondBoneName, string label)[] spreadCandidates =
            {
                ($"ValveBiped.Bip01_{sideCode}_Finger1", $"ValveBiped.Bip01_{sideCode}_Finger2", "index/middle"),
                ($"ValveBiped.Bip01_{sideCode}_Finger4", $"ValveBiped.Bip01_{sideCode}_Finger3", "pinky/ring")
            };

            foreach (var (firstBoneName, secondBoneName, label) in spreadCandidates)
            {
                if (!proportionsBoneMap.TryGetValue(firstBoneName, out Bone? firstBone)
                    || !proportionsBoneMap.TryGetValue(secondBoneName, out Bone? secondBone))
                {
                    continue;
                }

                Vector3 candidateDirection = (secondBone.WorldPosition - firstBone.WorldPosition).Normalised();
                if (candidateDirection.LengthSquared() <= 1e-8f)
                {
                    continue;
                }

                spreadDirection = candidateDirection;
                spreadPairLabel = label;
                return true;
            }

            return false;
        }

        // Attempts to find twist rotation around the bone's forward axis that best aligns the bone's secondary axis with the target roll axis,
        // and returns the angle of that twist. The target roll axis should ideally be perpendicular to the forward axis,
        // but if it's not then the function will do its best to find a twist that gets as close as possible to aligning them.
        private static bool TryCreateTwistRotationToAlignSecondaryAxis(
            Bone bone,
            Vector3 targetRollAxis,
            out Quaternion rotationDelta,
            out float signedAngle,
            out string description)
        {
            ArgumentNullException.ThrowIfNull(bone);

            rotationDelta = Quaternion.Identity;
            signedAngle = 0f;
            description = string.Empty;

            Vector3 forwardAxis = bone.WorldRotation.Rotate(new Vector3(1, 0, 0)).Normalised();
            if (forwardAxis.LengthSquared() <= 1e-8f)
            {
                description = "bone forward axis was zero";
                return false;
            }

            Vector3 projectedTargetRollAxis = ProjectOntoPlane(targetRollAxis, forwardAxis);
            if (projectedTargetRollAxis.LengthSquared() <= 1e-8f)
            {
                description = "target roll axis was parallel to the forward axis";
                return false;
            }

            Vector3[] candidateAxes =
            {
                ProjectOntoPlane(bone.WorldRotation.Rotate(new Vector3(0, 1, 0)), forwardAxis),
                ProjectOntoPlane(bone.WorldRotation.Rotate(new Vector3(0, 0, 1)), forwardAxis),
                ProjectOntoPlane(bone.WorldRotation.Rotate(new Vector3(0, -1, 0)), forwardAxis),
                ProjectOntoPlane(bone.WorldRotation.Rotate(new Vector3(0, 0, -1)), forwardAxis)
            };

            bool foundCandidate = false;
            float bestSignedAngle = 0f;
            float smallestAngleMagnitude = float.MaxValue;

            foreach (Vector3 candidateAxis in candidateAxes)
            {
                if (candidateAxis.LengthSquared() <= 1e-8f)
                {
                    continue;
                }

                float candidateSignedAngle = SignedAngleAroundAxis(candidateAxis, projectedTargetRollAxis, forwardAxis);
                float angleMagnitude = MathF.Abs(candidateSignedAngle);
                if (angleMagnitude < smallestAngleMagnitude)
                {
                    smallestAngleMagnitude = angleMagnitude;
                    bestSignedAngle = candidateSignedAngle;
                    foundCandidate = true;
                }
            }

            if (!foundCandidate)
            {
                description = "no usable secondary axis was available";
                return false;
            }

            if (smallestAngleMagnitude <= 0.001f)
            {
                description = "roll was already aligned";
                return true;
            }

            signedAngle = bestSignedAngle;
            rotationDelta = Quaternion.FromAngleAxis(bestSignedAngle, forwardAxis);
            description = $"applied a {smallestAngleMagnitude} radian twist";
            return true;
        }

        // Build a target world rotation whose local X aims at the child while local roll stays aligned with the original proportions armature.
        private static bool TryBuildArmTargetWorldRotation(
            Bone bone,
            Bone targetBone,
            BoneRotationSnapshot originalSnapshot,
            out Quaternion targetWorldRotation,
            out string failureReason)
        {
            ArgumentNullException.ThrowIfNull(bone);
            ArgumentNullException.ThrowIfNull(targetBone);
            ArgumentNullException.ThrowIfNull(originalSnapshot.LocalRotation);
            ArgumentNullException.ThrowIfNull(originalSnapshot.WorldRotation);

            targetWorldRotation = Quaternion.Identity;
            failureReason = string.Empty;

            Vector3 desiredXAxis = (targetBone.WorldPosition - bone.WorldPosition).Normalised();
            if (desiredXAxis.LengthSquared() <= 1e-8f)
            {
                failureReason = "desired child direction was zero";
                return false;
            }

            // Keep the original proportions Y axis as the preferred roll reference, falling back to Z if it becomes parallel to the new X axis.
            Vector3 preservedYAxis = ProjectOntoPlane(
                originalSnapshot.WorldRotation.Rotate(new Vector3(0, 1, 0)),
                desiredXAxis);
            if (preservedYAxis.LengthSquared() <= 1e-8f)
            {
                preservedYAxis = ProjectOntoPlane(
                    originalSnapshot.WorldRotation.Rotate(new Vector3(0, 0, 1)),
                    desiredXAxis);
            }

            if (preservedYAxis.LengthSquared() <= 1e-8f)
            {
                failureReason = "could not construct a preserved secondary axis";
                return false;
            }

            Vector3 zAxis = Vector3.Cross(desiredXAxis, preservedYAxis).Normalised();
            if (zAxis.LengthSquared() <= 1e-8f)
            {
                failureReason = "constructed Z axis was degenerate";
                return false;
            }

            Vector3 yAxis = Vector3.Cross(zAxis, desiredXAxis).Normalised();
            if (yAxis.LengthSquared() <= 1e-8f)
            {
                failureReason = "constructed Y axis was degenerate";
                return false;
            }

            // Rebuild an orthonormal basis so the bone gets the new swing with the preserved roll.
            targetWorldRotation = BuildRotationFromBasis(desiredXAxis, yAxis, zAxis);
            return true;
        }

        // Arm bones use the dedicated arm pass instead of the generic rotation solve.
        private static bool IsDedicatedArmSolveBone(string boneName)
        {
            return boneName switch
            {
                "ValveBiped.Bip01_L_UpperArm" => true,
                "ValveBiped.Bip01_L_Forearm" => true,
                "ValveBiped.Bip01_R_UpperArm" => true,
                "ValveBiped.Bip01_R_Forearm" => true,
                _ => false
            };
        }

        private static bool IsDirectReferenceRotationBone(string boneName)
        {
            return boneName switch
            {
                "ValveBiped.Bip01_Spine" => true,
                "ValveBiped.Bip01_Spine1" => true,
                "ValveBiped.Bip01_Spine2" => true,
                "ValveBiped.Bip01_Spine4" => true,
                "ValveBiped.Bip01_L_Clavicle" => true,
                "ValveBiped.Bip01_R_Clavicle" => true,
                _ => false
            };
        }

        // Project a vector onto the plane perpendicular to the given normal and normalise the result.
        private static Vector3 ProjectOntoPlane(Vector3 vector, Vector3 planeNormal)
        {
            Vector3 normalisedPlaneNormal = planeNormal.Normalised();
            if (normalisedPlaneNormal.LengthSquared() <= 1e-8f)
            {
                return Vector3.Zero;
            }

            Vector3 projected = vector - (normalisedPlaneNormal * Vector3.Dot(vector, normalisedPlaneNormal));
            return projected.Normalised();
        }

        // Build a world rotation from orthonormal X, Y and Z basis vectors.
        private static Quaternion BuildRotationFromBasis(Vector3 xAxis, Vector3 yAxis, Vector3 zAxis)
        {
            Matrix4x4 rotationMatrix = new(
                xAxis.X, yAxis.X, zAxis.X, 0f,
                xAxis.Y, yAxis.Y, zAxis.Y, 0f,
                xAxis.Z, yAxis.Z, zAxis.Z, 0f,
                0f, 0f, 0f, 1f);

            return NormalizeQuaternion(Quaternion.FromMatrix(rotationMatrix));
        }

        // Normalise a quaternion and guard against zero-length inputs.
        private static Quaternion NormalizeQuaternion(Quaternion quaternion)
        {
            ArgumentNullException.ThrowIfNull(quaternion);

            float magnitude = MathF.Sqrt(
                (quaternion.W * quaternion.W)
                + (quaternion.X * quaternion.X)
                + (quaternion.Y * quaternion.Y)
                + (quaternion.Z * quaternion.Z));

            if (magnitude <= 1e-8f)
            {
                return Quaternion.Identity;
            }

            return quaternion / magnitude;
        }

        private static bool AlignBoneLocalXAxisWithWorldDirection(Bone bone, Vector3 targetDirection, float minAngleDegrees, string description)
        {
            ArgumentNullException.ThrowIfNull(bone);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);

            if (targetDirection.LengthSquared() <= 1e-8f)
            {
                Console.WriteLine($"Skipping {description} because the target direction was zero");
                return false;
            }

            Vector3 localXAxisDirection = bone.WorldRotation.Rotate(new Vector3(1, 0, 0));
            Vector3 normalisedTargetDirection = targetDirection.Normalised();
            float angleToTarget = Vector3.Angle(localXAxisDirection, normalisedTargetDirection);

            if (angleToTarget <= minAngleDegrees)
            {
                Console.WriteLine($"{description} was already aligned (angle to target was {angleToTarget} degrees)");
                return false;
            }

            Quaternion rotationDelta = Quaternion.FromToRotation(localXAxisDirection, normalisedTargetDirection);
            ApplyWorldRotationPreservingDescendants(bone, rotationDelta * bone.WorldRotation);
            Console.WriteLine($"Adjusted {description} (angle to target was {angleToTarget} degrees)");
            return true;
        }

        private static void ApplyWorldRotationPreservingDescendants(Bone bone, Quaternion targetWorldRotation)
        {
            ArgumentNullException.ThrowIfNull(bone);
            ArgumentNullException.ThrowIfNull(targetWorldRotation);

            var descendantTransforms = CaptureDescendantTransforms(bone);
            bone.WorldRotation = targetWorldRotation;
            RestoreDescendantTransforms(descendantTransforms);
        }

        private static float SignedAngleAroundAxis(Vector3 from, Vector3 to, Vector3 axis)
        {
            Vector3 normalisedFrom = from.Normalised();
            Vector3 normalisedTo = to.Normalised();
            Vector3 normalisedAxis = axis.Normalised();

            if (normalisedFrom.LengthSquared() <= 1e-8f
                || normalisedTo.LengthSquared() <= 1e-8f
                || normalisedAxis.LengthSquared() <= 1e-8f)
            {
                return 0f;
            }

            float sin = Vector3.Dot(normalisedAxis, Vector3.Cross(normalisedFrom, normalisedTo));
            float cos = Math.Clamp(Vector3.Dot(normalisedFrom, normalisedTo), -1f, 1f);
            return MathF.Atan2(sin, cos);
        }

        private static List<(Bone bone, Vector3 worldPos, Quaternion worldRot)> CaptureDescendantTransforms(Bone bone, List<string>? ignore = null)
        {
            var descendantTransforms = new List<(Bone bone, Vector3 worldPos, Quaternion worldRot)>();
            foreach (var child in bone.Children)
            {
                if (child is Bone childBone)
                {
                    CollectDescendantTransforms(childBone, descendantTransforms, ignore);
                }
            }

            return descendantTransforms;
        }

        private static void RestoreDescendantTransforms(IEnumerable<(Bone bone, Vector3 worldPos, Quaternion worldRot)> descendantTransforms)
        {
            foreach (var (bone, worldPos, worldRot) in descendantTransforms)
            {
                bone.WorldPosition = worldPos;
                bone.WorldRotation = worldRot;
            }
        }

        // Recursively collect the world transforms of a bone and all its descendants, optionally ignoring any bones with names in the ignore list
        private static void CollectDescendantTransforms(Bone bone, List<(Bone bone, Vector3 worldPos, Quaternion worldRot)> transforms, List<string>? ignore = null)
        {
            if (ignore == null)
            {
                ignore = new List<string>();
            }

            transforms.Add((bone, bone.WorldPosition, bone.WorldRotation));
            foreach (var child in bone.Children)
            {
                if (child is Bone childBone)
                {
                    if (ignore.Contains(childBone.Name))
                    {
                        Console.WriteLine($"Ignoring descendant bone \"{childBone.Name}\" when collecting transforms for rotation adjustment");
                        continue;
                    }
                    CollectDescendantTransforms(childBone, transforms, ignore);
                }
            }
        }

        // Try to resolve the rig bone name for a rig slot by checking the assigned bone and logical bone, and return null if neither of them are valid rig bones in the rig bone map
        private static string? ResolveRigBoneName(RigSlot slot, IReadOnlyDictionary<string, Bone> rigBoneMap)
        {
            ArgumentNullException.ThrowIfNull(slot);
            ArgumentNullException.ThrowIfNull(rigBoneMap);

            if (!string.IsNullOrWhiteSpace(slot.AssignedBone) && rigBoneMap.ContainsKey(slot.AssignedBone))
            {
                return slot.AssignedBone;
            }

            if (!string.IsNullOrWhiteSpace(slot.LogicalBone) && rigBoneMap.ContainsKey(slot.LogicalBone))
            {
                return slot.LogicalBone;
            }

            return null;
        }
    }
}
