using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.Abstractions;
using Yggdrassil.Domain.Project;
using Yggdrassil.Domain.Scene;
using Yggdrassil.Infrastructure.Import;

using Vector3 = Yggdrassil.Domain.Scene.Vector3<float>;

namespace Yggdrassil.Infrastructure.Export
{
    public class ProportionTrickService : IProportionTrickService
    {
        private AnimationTemplateStore? _animationStore;

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

            // Root bone just gets moved to the rig equivalent

            var rigSlot = project.RigMapping.TryGetRigSlotFromName(result.Proportions.RootBone.Name);
            if (rigSlot != null)
            {
                var rigBone = rigSlot.AssignedBone;
                if (rigBone == null)
                {
                    throw new Exception($"Root bone \"{result.Proportions.RootBone.Name}\" of proportions model must be mapped to a rig slot in the project for proportion trick to work");
                }
                result.Proportions.RootBone.WorldPosition = rigBoneMap[rigSlot.AssignedBone!].WorldPosition;
                Console.WriteLine($"Set world position of proportions root bone \"{result.Proportions.RootBone.Name}\" to rig bone \"{rigBone}\"");
                Console.WriteLine($"Proportions root bone \"{result.Proportions.RootBone.Name}\" world position: {result.Proportions.RootBone.WorldPosition}, rig bone \"{rigBone}\" world position: {rigBoneMap[rigSlot.AssignedBone!].WorldPosition}");
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
                    ProcessBone(proportionsBone, project, rigBoneMap, collapsedBones);
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

            // Additional adjustments for specific bones that need slight tweaks
            // Wrist should be rotated to point exactly in between the middle and ring fingers
            {
                var leftHandName = "ValveBiped.Bip01_L_Hand";
                var rightHandName = "ValveBiped.Bip01_R_Hand";
                var leftFinger2Name = "ValveBiped.Bip01_L_Finger2";
                var leftFinger3Name = "ValveBiped.Bip01_L_Finger3";
                var rightFinger2Name = "ValveBiped.Bip01_R_Finger2";
                var rightFinger3Name = "ValveBiped.Bip01_R_Finger3";

                if (proportionsBoneMap.ContainsKey(leftHandName) && proportionsBoneMap.ContainsKey(leftFinger2Name) && proportionsBoneMap.ContainsKey(leftFinger3Name))
                {
                    var leftHand = proportionsBoneMap[leftHandName];
                    var leftFinger2 = proportionsBoneMap[leftFinger2Name];
                    var leftFinger3 = proportionsBoneMap[leftFinger3Name];

                    Vector3 averageFingerPosition = (leftFinger2.WorldPosition + leftFinger3.WorldPosition) * 0.5f;
                    Vector3 directionToTarget = (averageFingerPosition - leftHand.WorldPosition).Normalised();
                    Vector3 forwardDirection = leftHand.WorldRotation.Rotate(new Vector3(1, 0, 0));
                    float angleToTarget = Vector3<float>.Angle(forwardDirection, directionToTarget);

                    if (angleToTarget > 0.1f)
                    {
                        Quaternion rotationDelta = Quaternion.FromToRotation(forwardDirection, directionToTarget);

                        // Store original world transforms for all descendants
                        var descendantTransforms = new List<(Bone bone, Vector3 worldPos, Quaternion worldRot)>();
                        foreach (var child in leftHand.Children)
                        {
                            if (child is Bone childBone)
                            {
                                CollectDescendantTransforms(childBone, descendantTransforms);
                            }
                        }

                        // Apply rotation to hand bone
                        leftHand.WorldRotation = rotationDelta * leftHand.WorldRotation;

                        // Restore all descendants to their original world transforms
                        foreach (var (descendant, worldPos, worldRot) in descendantTransforms)
                        {
                            descendant.WorldPosition = worldPos;
                            descendant.WorldRotation = worldRot;
                        }

                        Console.WriteLine($"Adjusted rotation of left hand to point between fingers (angle was {angleToTarget} degrees)");
                    }
                }
                else
                {
                    Console.WriteLine($"Could not find left hand or finger bones for wrist adjustment");
                }

                if (proportionsBoneMap.ContainsKey(rightHandName) && proportionsBoneMap.ContainsKey(rightFinger2Name) && proportionsBoneMap.ContainsKey(rightFinger3Name))
                {
                    var rightHand = proportionsBoneMap[rightHandName];
                    var rightFinger2 = proportionsBoneMap[rightFinger2Name];
                    var rightFinger3 = proportionsBoneMap[rightFinger3Name];

                    Vector3 averageFingerPosition = (rightFinger2.WorldPosition + rightFinger3.WorldPosition) * 0.5f;
                    Vector3 directionToTarget = (averageFingerPosition - rightHand.WorldPosition).Normalised();
                    Vector3 forwardDirection = rightHand.WorldRotation.Rotate(new Vector3(1, 0, 0));
                    float angleToTarget = Vector3<float>.Angle(forwardDirection, directionToTarget);

                    if (angleToTarget > 0.1f)
                    {
                        Quaternion rotationDelta = Quaternion.FromToRotation(forwardDirection, directionToTarget);

                        // Store original world transforms for all descendants
                        var descendantTransforms = new List<(Bone bone, Vector3 worldPos, Quaternion worldRot)>();
                        foreach (var child in rightHand.Children)
                        {
                            if (child is Bone childBone)
                            {
                                CollectDescendantTransforms(childBone, descendantTransforms);
                            }
                        }

                        // Apply rotation to hand bone
                        rightHand.WorldRotation = rotationDelta * rightHand.WorldRotation;

                        // Restore all descendants to their original world transforms
                        foreach (var (descendant, worldPos, worldRot) in descendantTransforms)
                        {
                            descendant.WorldPosition = worldPos;
                            descendant.WorldRotation = worldRot;
                        }

                        Console.WriteLine($"Adjusted rotation of right hand to point between fingers (angle was {angleToTarget} degrees)");
                    }
                }
                else
                {
                    Console.WriteLine($"Could not find right hand or finger bones for wrist adjustment");
                }
            }

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

            // Do another once over of the spine rotations to make sure they're all good, and adjust any that are slightly off by hand if needed
            {
                // Bip01_Spine, Bip01_Spine1, Bip01_Spine2 and Bip01_Spine4
                // should all point at the next one in the chain
                // Spine4 should be poinint at Bip01_Neck1
                // If any of them are off by more than 10 degrees, rotate them to point exactly at the next one in the chain

                // Spine
                var spineBones = new[] { "ValveBiped.Bip01_Spine", "ValveBiped.Bip01_Spine1", "ValveBiped.Bip01_Spine2", "ValveBiped.Bip01_Spine4" };
                var neckBoneName = "ValveBiped.Bip01_Neck1";
                for (int i = 0; i < spineBones.Length; i++)
                {
                    var boneName = spineBones[i];
                    if (!proportionsBoneMap.ContainsKey(boneName))
                    {
                        Console.WriteLine($"Proportions bone \"{boneName}\" not found for spine adjustment");
                        continue;
                    }
                    var bone = proportionsBoneMap[boneName];
                    Bone? targetBone = null;
                    if (i < spineBones.Length - 1)
                    {
                        var nextBoneName = spineBones[i + 1];
                        if (proportionsBoneMap.ContainsKey(nextBoneName))
                        {
                            targetBone = proportionsBoneMap[nextBoneName];
                        }
                        else
                        {
                            Console.WriteLine($"Proportions bone \"{nextBoneName}\" not found for spine adjustment");
                        }
                    }
                    else
                    {
                        if (proportionsBoneMap.ContainsKey(neckBoneName))
                        {
                            targetBone = proportionsBoneMap[neckBoneName];
                        }
                        else
                        {
                            Console.WriteLine($"Proportions bone \"{neckBoneName}\" not found for spine adjustment");
                        }
                    }
                    if (targetBone == null)
                    {
                        Console.WriteLine($"No target bone found for spine bone \"{boneName}\", skipping adjustment");
                        continue;
                    }
                    Vector3 directionToTarget = (targetBone.WorldPosition - bone.WorldPosition).Normalised();
                    Vector3 forwardDirection = bone.WorldRotation.Rotate(new Vector3(1, 0, 0));
                    float angleToTarget = Vector3<float>.Angle(forwardDirection, directionToTarget);
                    if (angleToTarget > 10f)
                    {
                        Quaternion rotationDelta = Quaternion.FromToRotation(forwardDirection, directionToTarget);
                        
                        // Store original world transforms for all descendants
                        var descendantTransforms = new List<(Bone bone, Vector3 worldPos, Quaternion worldRot)>();
                        foreach (var child in bone.Children)
                        {
                            if (child is Bone childBone)
                            {
                                CollectDescendantTransforms(childBone, descendantTransforms);
                            }
                        }
                        
                        // Apply rotation to parent bone
                        bone.WorldRotation = rotationDelta * bone.WorldRotation;
                        
                        // Restore all descendants to their original world transforms
                        foreach (var (descendant, worldPos, worldRot) in descendantTransforms)
                        {
                            descendant.WorldPosition = worldPos;
                            descendant.WorldRotation = worldRot;
                        }

                        Console.WriteLine($"Adjusted rotation of spine bone \"{boneName}\" to point at \"{targetBone.Name}\" (angle to target was {angleToTarget} degrees)");
                    }
                }
            }


            // Do a once over of the clavicles
            {
                var leftClavicleName = "ValveBiped.Bip01_L_Clavicle";
                var rightClavicleName = "ValveBiped.Bip01_R_Clavicle";

                var leftUpperArmName = "ValveBiped.Bip01_L_UpperArm";
                var rightUpperArmName = "ValveBiped.Bip01_R_UpperArm";

                // Same logic as spines
                if (proportionsBoneMap.ContainsKey(leftClavicleName) && proportionsBoneMap.ContainsKey(leftUpperArmName))
                {
                    var leftClavicle = proportionsBoneMap[leftClavicleName];
                    var leftUpperArm = proportionsBoneMap[leftUpperArmName];
                    Vector3 leftDirectionToTarget = (leftUpperArm.WorldPosition - leftClavicle.WorldPosition).Normalised();
                    Vector3 leftForwardDirection = leftClavicle.WorldRotation.Rotate(new Vector3(1, 0, 0));
                    float leftAngleToTarget = Vector3<float>.Angle(leftForwardDirection, leftDirectionToTarget);
                    if (leftAngleToTarget > 10f)
                    {
                        Quaternion leftRotationDelta = Quaternion.FromToRotation(leftForwardDirection, leftDirectionToTarget);
                        
                        // Store original world transforms for all descendants
                        var descendantTransforms = new List<(Bone bone, Vector3 worldPos, Quaternion worldRot)>();
                        foreach (var child in leftClavicle.Children)
                        {
                            if (child is Bone childBone)
                            {
                                CollectDescendantTransforms(childBone, descendantTransforms);
                            }
                        }
                        
                        // Apply rotation to parent bone
                        leftClavicle.WorldRotation = leftRotationDelta * leftClavicle.WorldRotation;
                        
                        // Restore all descendants to their original world transforms
                        foreach (var (descendant, worldPos, worldRot) in descendantTransforms)
                        {
                            descendant.WorldPosition = worldPos;
                            descendant.WorldRotation = worldRot;
                        }
                        Console.WriteLine($"Adjusted rotation of clavicle bone \"{leftClavicleName}\" to point at upper arm \"{leftUpperArmName}\" (angle to target was {leftAngleToTarget} degrees)");
                    }
                }
                else
                {
                    Console.WriteLine($"Could not find bones for left clavicle adjustment");
                }

                if (proportionsBoneMap.ContainsKey(rightClavicleName) && proportionsBoneMap.ContainsKey(rightUpperArmName))
                {
                    var rightClavicle = proportionsBoneMap[rightClavicleName];
                    var rightUpperArm = proportionsBoneMap[rightUpperArmName];
                    Vector3 rightDirectionToTarget = (rightUpperArm.WorldPosition - rightClavicle.WorldPosition).Normalised();
                    Vector3 rightForwardDirection = rightClavicle.WorldRotation.Rotate(new Vector3(1, 0, 0));
                    float rightAngleToTarget = Vector3<float>.Angle(rightForwardDirection, rightDirectionToTarget);
                    if (rightAngleToTarget > 10f)
                    {
                        Quaternion rightRotationDelta = Quaternion.FromToRotation(rightForwardDirection, rightDirectionToTarget);

                        // Store original world transforms for all descendants
                        var descendantTransforms = new List<(Bone bone, Vector3 worldPos, Quaternion worldRot)>();
                        foreach (var child in rightClavicle.Children)
                        {
                            if (child is Bone childBone)
                            {
                                CollectDescendantTransforms(childBone, descendantTransforms);
                            }
                        }

                        // Apply rotation to parent bone
                        rightClavicle.WorldRotation = rightRotationDelta * rightClavicle.WorldRotation;

                        // Restore all descendants to their original world transforms
                        foreach (var (descendant, worldPos, worldRot) in descendantTransforms)
                        {
                            descendant.WorldPosition = worldPos;
                            descendant.WorldRotation = worldRot;
                        }
                        Console.WriteLine($"Adjusted rotation of clavicle bone \"{rightClavicleName}\" to point at upper arm \"{rightUpperArmName}\" (angle to target was {rightAngleToTarget} degrees)");
                    }
                }
                else
                {
                    Console.WriteLine($"Could not find bones for right clavicle adjustment");
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

            // Copy the rotations of the proportions bones to the reference bones
            var proportionsBoneDict = result.Proportions.RootBone.GetAllDescendantsAndSelf().ToDictionary(b => b.Name);
            var referenceMaleBoneDict = result.ReferenceMale.RootBone.GetAllDescendantsAndSelf().ToDictionary(b => b.Name);
            var referenceFemaleBoneDict = result.ReferenceFemale.RootBone.GetAllDescendantsAndSelf().ToDictionary(b => b.Name);

            Console.WriteLine("Copying rotations from proportions bones to reference bones");

            foreach (var boneName in proportionsBoneDict.Keys)
            {
                // We pray that LocalRotation setter works correctly until we can test this

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


        private static void ProcessBone(Bone proportionsBone, Project project, Dictionary<string, Bone> rigBoneMap, HashSet<Bone> collapsedBones)
        {
            Console.WriteLine($"\n=== Processing bone: {proportionsBone.Name} ===");

            // Find the corresponding rig bone
            var rigSlot = project.RigMapping.TryGetRigSlotFromName(proportionsBone.Name);
            if (rigSlot == null )
            {
                Console.WriteLine($"Proportions bone {proportionsBone.Name} does not have a corresponding rig slot, skipping");
                return;
            }
            var rigBoneName = rigSlot.AssignedBone;
            if (rigBoneName == null)
            {
                // Proportions bone is mapped to a rig slot, but that rig slot doesn't have an assigned bone
                // Collapse this proportions bone into its first mapped descendant bone, if it has one, otherwise just skip it
                Bone? firstMappedDescendant = FindFirstMappedDescendant(proportionsBone, project);
                if (firstMappedDescendant != null)
                {
                    ProcessBone(firstMappedDescendant, project, rigBoneMap, collapsedBones);
                    proportionsBone.WorldMatrix = firstMappedDescendant.WorldMatrix;
                    firstMappedDescendant.LocalPosition = Vector3<float>.Zero;
                    firstMappedDescendant.LocalRotation = Quaternion.Identity;
                    firstMappedDescendant.LocalScale = Vector3<float>.One;
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


        private static bool HasMappedChild(Bone bone, Project project)
        {
            if (bone.Children.Count == 0)
                return false;
            foreach (var child in bone.Children)
            {
                if (child is Bone childBone)
                {
                    if (childBone.Name != null && project.RigMapping.TryGetRigSlotFromName(childBone.Name) != null)
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

        private static Bone? FindFirstMappedDescendant(Bone bone, Project project)
        {
            foreach (var child in bone.Children)
            {
                if (child is Bone childBone)
                {
                    if (childBone.Name != null && project.RigMapping.TryGetRigSlotFromName(childBone.Name) != null)
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

        private static void CollectDescendantTransforms(Bone bone, List<(Bone bone, Vector3 worldPos, Quaternion worldRot)> transforms, List<string> ignore = null)
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
    }
}
