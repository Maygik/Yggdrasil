using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Domain.Project;
using Yggdrassil.Domain.Scene;

// Based on CaptainBigButt's stuff
// https://steamcommunity.com/sharedfiles/filedetails/?id=2225904754


namespace Yggdrassil.Infrastructure.Export
{
    public static class Deprecated_ProportionTrickFactory
    {
        public static Dictionary<string, SceneModel> BuildReferences(Project project)
        {
            // Pain and suffering

            // Steps (1x male, 1x female, easier to just always export both and makes ux nicer):
            // 1. Build a "proportions" model which will copy the bone transforms of the original model, but with the correct bone rotations.
            // 2. Load the reference model (the one with the correct proportions)
            // 3. For each bone in the reference model, find the corresponding bone in the proportions model and copy the rotation from the proportions model
            // 4. Export the proportions model to "proportions"
            // 5. Export the reference model to "x_reference"

            if (project.Scene.RootBone == null)
            {
                throw new InvalidOperationException("Scene must have a root bone to build proportions reference.");
            }

            Dictionary<string, Bone> originalBones = new Dictionary<string, Bone>();
            Bone originalRoot = project.Scene.RootBone;
            void BuildOriginalBoneMap(Bone bone)
            {
                originalBones[bone.Name] = bone;
                foreach (Bone child in bone.Children)
                {
                    BuildOriginalBoneMap(child);
                }
            }
            BuildOriginalBoneMap(originalRoot);


            var proportionsModel = BuildBaseProportions();
            Dictionary<string, Bone> proportionsBones = new Dictionary<string, Bone>();
            void BuildProportionsBoneMap(Bone bone)
            {
                proportionsBones[bone.Name] = bone;
                foreach (Bone child in bone.Children)
                {
                    BuildProportionsBoneMap(child);
                }
            }
            if (proportionsModel.RootBone == null)
            {
                throw new InvalidOperationException("Proportions model failed to create a root bone.");
            }
            BuildProportionsBoneMap(proportionsModel.RootBone);


            // Copy the positions from the original model to the proportions model
            
            foreach (var kvp in proportionsBones)
            {
                // Check if bone exists in original mapping
                var slot = project.RigMapping.TryGetRigSlotFromName(kvp.Key);
                if (slot == null || slot.AssignedBone == null)
                {
                    Console.WriteLine($"Bone {kvp.Key} has not been set in rig mapping, skipping.");
                    continue;
                }
                var propBone = kvp.Value;
                if (!originalBones.TryGetValue(slot.AssignedBone, out var originalBone))
                {
                    Console.WriteLine($"Target bone \"{kvp.Key}\" is mapped to source bone \"{slot.AssignedBone}\" which does not exist in the original model, skipping.");
                    continue;
                }

                // This line is so much more complex than it looks
                propBone.WorldPosition = originalBone.WorldPosition;

            }

            // Make sure the rotations are correct for source
            // TODO: Make sure they're correct
            // Need to verify the correct rotations for the proportions model
            // Will need to correct child positions after rotating the bones, so repeat the position copying for child bones after rotating the parent bones
            void ApplyRotations(Bone bone)
            {
                // Calculate the rotation for this bone
                // Default to left
                Vector3<float> facing = new Vector3<float>(1, 0, 0);

                if (bone.Children.Count == 0)
                {
                    // Leaf bone, use current bone axis that is closest to pointing towards the center of mass of weights

                    // 1) Find average position of the weights for this bone in world space
                    // 2) Calculate the direction from the bone to this average position
                    // 3) Find which bone axis is the closest (x,y,z) to pointing towards this direction

                    Vector3<float> averagePosition = new();
                    int count = 0;

                    foreach (var mesh in project.Scene.MeshGroups)
                    {
                        foreach (var meshData in mesh.Meshes)
                        {
                            for (int i = 0; i < meshData.Vertices.Count; i++)
                            {
                                var vertex = meshData.Vertices[i];
                                var weight = meshData.BoneWeights[i].FirstOrDefault(w => w.Item1 == bone.Name);
                                if (weight != null)
                                {
                                    averagePosition += vertex * weight.Item2;
                                    count++;
                                }
                            }
                        }
                    }

                    // Weighted average position of the weights for this bone in world space

                    // Backup default facing direction
                    // Straight from reference armature
                    facing = new Vector3<float>(0, 2.06235f, 0.3599f);


                    if (count > 0)
                    {
                        float countF = (float)count;
                        averagePosition.X /= countF;
                        averagePosition.Y /= countF;
                        averagePosition.Z /= countF;

                        var directionToCoM = averagePosition - bone.WorldPosition;

                        var right = new Vector4<float>(bone.WorldMatrix.GetRow(0)).xyz;
                        var up = new Vector4<float>(bone.WorldMatrix.GetRow(1)).xyz;
                        var forward = new Vector4<float>(bone.WorldMatrix.GetRow(2)).xyz;

                        float Dot(Vector3<float> a, Vector3<float> b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

                        float rightDot = Dot(right, directionToCoM);
                        float upDot = Dot(up, directionToCoM);
                        float forwardDot = Dot(forward, directionToCoM);

                        // Find which axis is most closely aligned with the direction to the center of mass
                        if (Math.Abs(rightDot) > Math.Abs(upDot) && Math.Abs(rightDot) > Math.Abs(forwardDot))
                        {
                            // Right axis is most aligned
                            if (rightDot > 0)
                            {
                                // Pointing towards the center of mass, so we leave it as is
                                facing = right;
                            }
                            else
                            {
                                // Pointing away from the center of mass, so just flip the vector
                                facing = right * -1;
                            }
                        }
                        else if (Math.Abs(upDot) > Math.Abs(rightDot) && Math.Abs(upDot) > Math.Abs(forwardDot))
                        {
                            // Up axis is most aligned
                            if (upDot > 0)
                            {
                                // Pointing towards the center of mass, so we leave it as is
                                facing = up;
                            }
                            else
                            {
                                // Pointing away from the center of mass, so just flip the vector
                                facing = up * -1;
                            }
                        }
                        else
                        {
                            // Forward axis is most aligned
                            if (forwardDot > 0)
                            {
                                // Pointing towards the center of mass, so we leave it as is
                                facing = forward;
                            }
                            else
                            {
                                // Pointing away from the center of mass, so just flip the vector
                                facing = forward * -1;
                            }
                        }
                    }

                }

                else
                {
                    // Non-leaf bone, can calculate based on position of children
                    // All bones apart from ValveBiped.Bip01_L_Hand and ValveBiped.Bip01_R_Hand
                    // just point directly to their first child
                    // For the hand bones, they point directly in between the middle and ring fingers (so between the second and third child)
                    if (bone.Children.Count == 0)
                    {
                        throw new InvalidOperationException($"Bone {bone.Name} has no children, but was expected to have at least one.");
                    }

                    if (bone.Name == "ValveBiped.Bip01_L_Hand" || bone.Name == "ValveBiped.Bip01_R_Hand")
                    {
                        if (bone.Children.Count < 3)
                        {
                            throw new InvalidOperationException($"Bone {bone.Name} was expected to have at least 3 children, but only has {bone.Children.Count}.");
                        }
                        var middleFingerPos = bone.Children[1].WorldPosition;
                        var ringFingerPos = bone.Children[2].WorldPosition;
                        var directionToMiddle = middleFingerPos - bone.WorldPosition;
                        var directionToRing = ringFingerPos - bone.WorldPosition;
                        facing = directionToMiddle + directionToRing;
                    }
                    else
                    {

                        // Get the position of the first child bone in world space
                        var childPos = bone.Children[0].WorldPosition;
                        var directionToChild = childPos - bone.WorldPosition;

                        facing = directionToChild;
                    }
                }

                // Normalise the facing vector
                float length = (float)Math.Sqrt(facing.X * facing.X + facing.Y * facing.Y + facing.Z * facing.Z);
                if (length > 0)
                {
                    facing.X /= length;
                    facing.Y /= length;
                    facing.Z /= length;
                }
                else
                {
                    // If the length is zero, something went wrong
                    Console.Error.WriteLine($"Bone {bone.Name} has a facing vector of length zero, defaulting to (0,1,0).");
                    facing = new Vector3<float>(0, 1, 0);
                }

                // Got the facing vector
                // Now the problem is figuring out the roll around this vector

                // Things noted from reference:
                // 1) Facing vector is local x
                // 2) Local y is perpendicular to the facing vector. In blender it is the head of the bone.
                //      Pelvis points straight up
                //      Legs points backwards, feet/toes more downwards.
                //      Spines all point forwards
                //      Neck/Head point more backwards
                //      Clavicles point more upwards, uppearm/forearm points backwards
                //      Hands points outwards (Right point right, Left points left)
                //      Fingers all point outwards
                // 3) Local z is perpendicular to both of these, follows the right hand rule
                // 4) The roll of the bone is basically just how much the local y axis is rotated around the local x axis
                // How can we make sure we're calculating roll without breaking it?



                // Fix child positions
                foreach (Bone child in bone.Children)
                {
                    // Set the world position again like the above code
                    child.WorldPosition = originalBones[child.Name].WorldPosition;

                    // Recurse
                    ApplyRotations(child);
                }
            }



            // Load the male and female references
            // TODO: Load it (Should be relatively trivial to load)



            // Copy the rotations from the proportions model to the reference models
            // TODO: Copy the rotations





            Dictionary<string, SceneModel> references = new Dictionary<string, SceneModel>
            {
                { "proportions", proportionsModel },
                { "male", null! }, // TODO: Replace with loaded
                { "female", null! }, // TODO: Replace with loaded
            };

            return references;


            throw new NotImplementedException();
        }

        private static SceneModel BuildBaseProportions()
        {
            SceneModel sceneModel = new SceneModel();
            List<string> boneNames = new List<string>
            {
                // core
                "ValveBiped.Bip01_Pelvis",
                "ValveBiped.Bip01_Spine",
                "ValveBiped.Bip01_Spine1",
                "ValveBiped.Bip01_Spine2",
                "ValveBiped.Bip01_Spine4",
                "ValveBiped.Bip01_Neck1",
                "ValveBiped.Bip01_Head1",

                // arms
                "ValveBiped.Bip01_L_Clavicle",
                "ValveBiped.Bip01_L_UpperArm",
                "ValveBiped.Bip01_L_Forearm",
                "ValveBiped.Bip01_L_Hand",
                "ValveBiped.Bip01_R_Clavicle",
                "ValveBiped.Bip01_R_UpperArm",
                "ValveBiped.Bip01_R_Forearm",
                "ValveBiped.Bip01_R_Hand",

                // legs
                "ValveBiped.Bip01_L_Thigh",
                "ValveBiped.Bip01_L_Calf",
                "ValveBiped.Bip01_L_Foot",
                "ValveBiped.Bip01_L_Toe0",
                "ValveBiped.Bip01_R_Thigh",
                "ValveBiped.Bip01_R_Calf",
                "ValveBiped.Bip01_R_Foot",
                "ValveBiped.Bip01_R_Toe0",

                // left fingers
                "ValveBiped.Bip01_L_Finger0",
                "ValveBiped.Bip01_L_Finger01",
                "ValveBiped.Bip01_L_Finger02",
                "ValveBiped.Bip01_L_Finger1",
                "ValveBiped.Bip01_L_Finger11",
                "ValveBiped.Bip01_L_Finger12",
                "ValveBiped.Bip01_L_Finger2",
                "ValveBiped.Bip01_L_Finger21",
                "ValveBiped.Bip01_L_Finger22",
                "ValveBiped.Bip01_L_Finger3",
                "ValveBiped.Bip01_L_Finger31",
                "ValveBiped.Bip01_L_Finger32",
                "ValveBiped.Bip01_L_Finger4",
                "ValveBiped.Bip01_L_Finger41",
                "ValveBiped.Bip01_L_Finger42",

                // right fingers
                "ValveBiped.Bip01_R_Finger0",
                "ValveBiped.Bip01_R_Finger01",
                "ValveBiped.Bip01_R_Finger02",
                "ValveBiped.Bip01_R_Finger1",
                "ValveBiped.Bip01_R_Finger11",
                "ValveBiped.Bip01_R_Finger12",
                "ValveBiped.Bip01_R_Finger2",
                "ValveBiped.Bip01_R_Finger21",
                "ValveBiped.Bip01_R_Finger22",
                "ValveBiped.Bip01_R_Finger3",
                "ValveBiped.Bip01_R_Finger31",
                "ValveBiped.Bip01_R_Finger32",
                "ValveBiped.Bip01_R_Finger4",
                "ValveBiped.Bip01_R_Finger41",
                "ValveBiped.Bip01_R_Finger42",
            };


            Dictionary<string, Bone> bones = new Dictionary<string, Bone>();
            foreach (string boneName in boneNames)
            {
                bones[boneName] = new Bone(boneName);
            }

            // Core
            bones["ValveBiped.Bip01_Pelvis"].AddChild(bones["ValveBiped.Bip01_Spine"]);
            bones["ValveBiped.Bip01_Spine"].AddChild(bones["ValveBiped.Bip01_Spine1"]);
            bones["ValveBiped.Bip01_Spine1"].AddChild(bones["ValveBiped.Bip01_Spine2"]);
            bones["ValveBiped.Bip01_Spine2"].AddChild(bones["ValveBiped.Bip01_Spine4"]);
            bones["ValveBiped.Bip01_Spine4"].AddChild(bones["ValveBiped.Bip01_Neck1"]);
            bones["ValveBiped.Bip01_Neck1"].AddChild(bones["ValveBiped.Bip01_Head1"]);

            // Left arm
            bones["ValveBiped.Bip01_Spine4"].AddChild(bones["ValveBiped.Bip01_L_Clavicle"]);
            bones["ValveBiped.Bip01_L_Clavicle"].AddChild(bones["ValveBiped.Bip01_L_UpperArm"]);
            bones["ValveBiped.Bip01_L_UpperArm"].AddChild(bones["ValveBiped.Bip01_L_Forearm"]);
            bones["ValveBiped.Bip01_L_Forearm"].AddChild(bones["ValveBiped.Bip01_L_Hand"]);

            // Right arm
            bones["ValveBiped.Bip01_Spine4"].AddChild(bones["ValveBiped.Bip01_R_Clavicle"]);
            bones["ValveBiped.Bip01_R_Clavicle"].AddChild(bones["ValveBiped.Bip01_R_UpperArm"]);
            bones["ValveBiped.Bip01_R_UpperArm"].AddChild(bones["ValveBiped.Bip01_R_Forearm"]);
            bones["ValveBiped.Bip01_R_Forearm"].AddChild(bones["ValveBiped.Bip01_R_Hand"]);

            // Left leg
            bones["ValveBiped.Bip01_Pelvis"].AddChild(bones["ValveBiped.Bip01_L_Thigh"]);
            bones["ValveBiped.Bip01_L_Thigh"].AddChild(bones["ValveBiped.Bip01_L_Calf"]);
            bones["ValveBiped.Bip01_L_Calf"].AddChild(bones["ValveBiped.Bip01_L_Foot"]);
            bones["ValveBiped.Bip01_L_Foot"].AddChild(bones["ValveBiped.Bip01_L_Toe0"]);

            // Right leg
            bones["ValveBiped.Bip01_Pelvis"].AddChild(bones["ValveBiped.Bip01_R_Thigh"]);
            bones["ValveBiped.Bip01_R_Thigh"].AddChild(bones["ValveBiped.Bip01_R_Calf"]);
            bones["ValveBiped.Bip01_R_Calf"].AddChild(bones["ValveBiped.Bip01_R_Foot"]);
            bones["ValveBiped.Bip01_R_Foot"].AddChild(bones["ValveBiped.Bip01_R_Toe0"]);


            // Left fingers
            bones["ValveBiped.Bip01_L_Hand"].AddChild(bones["ValveBiped.Bip01_L_Finger0"]);
            bones["ValveBiped.Bip01_L_Finger0"].AddChild(bones["ValveBiped.Bip01_L_Finger01"]);
            bones["ValveBiped.Bip01_L_Finger01"].AddChild(bones["ValveBiped.Bip01_L_Finger02"]);

            bones["ValveBiped.Bip01_L_Hand"].AddChild(bones["ValveBiped.Bip01_L_Finger1"]);
            bones["ValveBiped.Bip01_L_Finger1"].AddChild(bones["ValveBiped.Bip01_L_Finger11"]);
            bones["ValveBiped.Bip01_L_Finger11"].AddChild(bones["ValveBiped.Bip01_L_Finger12"]);

            bones["ValveBiped.Bip01_L_Hand"].AddChild(bones["ValveBiped.Bip01_L_Finger2"]);
            bones["ValveBiped.Bip01_L_Finger2"].AddChild(bones["ValveBiped.Bip01_L_Finger21"]);
            bones["ValveBiped.Bip01_L_Finger21"].AddChild(bones["ValveBiped.Bip01_L_Finger22"]);

            bones["ValveBiped.Bip01_L_Hand"].AddChild(bones["ValveBiped.Bip01_L_Finger3"]);
            bones["ValveBiped.Bip01_L_Finger3"].AddChild(bones["ValveBiped.Bip01_L_Finger31"]);
            bones["ValveBiped.Bip01_L_Finger31"].AddChild(bones["ValveBiped.Bip01_L_Finger32"]);

            bones["ValveBiped.Bip01_L_Hand"].AddChild(bones["ValveBiped.Bip01_L_Finger4"]);
            bones["ValveBiped.Bip01_L_Finger4"].AddChild(bones["ValveBiped.Bip01_L_Finger41"]);
            bones["ValveBiped.Bip01_L_Finger41"].AddChild(bones["ValveBiped.Bip01_L_Finger42"]);


            // Right fingers
            bones["ValveBiped.Bip01_R_Hand"].AddChild(bones["ValveBiped.Bip01_R_Finger0"]);
            bones["ValveBiped.Bip01_R_Finger0"].AddChild(bones["ValveBiped.Bip01_R_Finger01"]);
            bones["ValveBiped.Bip01_R_Finger01"].AddChild(bones["ValveBiped.Bip01_R_Finger02"]);

            bones["ValveBiped.Bip01_R_Hand"].AddChild(bones["ValveBiped.Bip01_R_Finger1"]);
            bones["ValveBiped.Bip01_R_Finger1"].AddChild(bones["ValveBiped.Bip01_R_Finger11"]);
            bones["ValveBiped.Bip01_R_Finger11"].AddChild(bones["ValveBiped.Bip01_R_Finger12"]);

            bones["ValveBiped.Bip01_R_Hand"].AddChild(bones["ValveBiped.Bip01_R_Finger2"]);
            bones["ValveBiped.Bip01_R_Finger2"].AddChild(bones["ValveBiped.Bip01_R_Finger21"]);
            bones["ValveBiped.Bip01_R_Finger21"].AddChild(bones["ValveBiped.Bip01_R_Finger22"]);

            bones["ValveBiped.Bip01_R_Hand"].AddChild(bones["ValveBiped.Bip01_R_Finger3"]);
            bones["ValveBiped.Bip01_R_Finger3"].AddChild(bones["ValveBiped.Bip01_R_Finger31"]);
            bones["ValveBiped.Bip01_R_Finger31"].AddChild(bones["ValveBiped.Bip01_R_Finger32"]);

            bones["ValveBiped.Bip01_R_Hand"].AddChild(bones["ValveBiped.Bip01_R_Finger4"]);
            bones["ValveBiped.Bip01_R_Finger4"].AddChild(bones["ValveBiped.Bip01_R_Finger41"]);
            bones["ValveBiped.Bip01_R_Finger41"].AddChild(bones["ValveBiped.Bip01_R_Finger42"]);


            sceneModel.RootBone = bones["ValveBiped.Bip01_Pelvis"];

            return sceneModel;
        }

    }
}
