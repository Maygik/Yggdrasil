using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Domain.Project;
using Yggdrassil.Domain.Scene;
using Yggdrassil.Infrastructure.Import;

namespace Yggdrassil.Infrastructure.Export
{
    public static class ProportionTrickFactory
    {
        // Builds the reference animations for proportion trick
        public static void BuildAnimations(Project project, out SceneModel reference_male, out SceneModel reference_female, out SceneModel proportions)
        {
            // Import the base proportions model from packaged resources
            string resourcesPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Armatures", "proportions.smd");
            proportions = SmdImporter.ImportSmd(resourcesPath);

            // Import the base proportions model, which is a T-pose model with the correct proportions

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

            if (proportions.RootBone == null)
                throw new Exception("Proportions model must have a root bone");
            if (project.Scene.RootBone == null)
                throw new Exception("Project model must have a root bone to run proportion trick");

            // Build a bone map for the original proportions model as well as the project model
            Dictionary<string, Bone> proportionsBoneMap = proportions.RootBone.GetAllDescendantsAndSelf().ToDictionary(b => b.Name);
            Dictionary<string, Bone> rigBoneMap = project.Scene.RootBone.GetAllDescendantsAndSelf().ToDictionary(b => b.Name);

            foreach (var proportionsBone in proportions.RootBone.GetAllDescendantsAndSelf())
            {
                var rigSlot = project.RigMapping.TryGetRigSlotFromName(proportionsBone.Name);
                if (rigSlot != null)
                {
                    string? rigBoneName = rigSlot.AssignedBone;
                    if (rigBoneName != null)
                    {
                        // Move the proportions bone to the rig bone position, and rotate it to match the rig bone rotation
                        // Move in world space to preserve the local axis of the proportions bones, which is important for proportion trick to work
                        Bone rigBone = rigBoneMap[rigBoneName];

                        proportionsBone.WorldPosition = rigBone.WorldPosition;

                        Vector3<float> facingDir = new();

                        // Rotate the proportions bone to point towards the rig bone's main child, or the center of influence if it has no children
                        if (rigBone.Children.Count > 0)
                        {
                            // TODO: Check that the child is actually the main one for every bone
                            // Might need to setup custom rules for certain bones if not
                            facingDir = rigBone.Children[0].WorldPosition - rigBone.WorldPosition;
                        }
                        else
                        {
                            // Leaf bone, use current bone axis that is closest to pointing towards the center of mass of weights

                            // 1) Find average position of the weights for this bone in world space
                            // 2) Calculate the direction from the bone to this average position
                            // 3) Find which bone axis is the closest (x,y,z) to pointing towards this direction

                            Vector3<float> averagePosition = new();
                            float sumWeight = 0;

                            foreach (var mesh in project.Scene.MeshGroups)
                            {
                                foreach (var meshData in mesh.Meshes)
                                {
                                    for (int i = 0; i < meshData.Vertices.Count; i++)
                                    {
                                        var vertex = meshData.Vertices[i];
                                        var weight = meshData.BoneWeights[i].FirstOrDefault(w => w.Item1 == rigBone.Name);
                                        if (weight?.Item2 > 0)
                                        {
                                            averagePosition += vertex * weight.Item2;
                                            sumWeight += weight.Item2;
                                        }
                                    }
                                }
                            }

                            if (sumWeight == 0)
                            {
                                // No weights for this bone, just use the rig bone's forward direction
                                var facing = rigBone.WorldMatrix.GetColumn(0);
                                facingDir = new Vector3<float>(facing[0], facing[1], facing[2]);
                            }
                            else
                            {
                                facingDir = (averagePosition / sumWeight) - rigBone.WorldPosition;
                            }
                            
                        }

                        // Now that we have the target facing direction, we need to rotate the proportions bone to match this direction
                        // Find the shortest rotation to face this direction, and apply it to the proportions bone

                        var worldX = proportionsBone.WorldMatrix.GetColumn(0); // Assuming the bone's local x-axis is the forward direction
                        Vector3<float> worldXAxis = new(worldX[0], worldX[1], worldX[2]);

                        Quaternion rotation = Quaternion.FromToRotation(worldXAxis.Normalised(), facingDir.Normalised());
                        proportionsBone.WorldRotation = rotation * proportionsBone.WorldRotation;


                        // Facing (X) is now correct
                        // But we need to make sure the roll is correct



                    }
                    else
                    {
                        // Bone will need moving to it's child's equivalent bone, then child setting to 0
                        // Annoying feed forward, but we have to do it in one pass to preserve the local axis of the proportions bones
                        // TODO: Add this
                        throw new NotImplementedException($"Bone {proportionsBone.Name} in proportions model has no assigned bone in the rig mapping, and collapsing bones isn't implemented yet");
                    }
                }
                else
                {
                    throw new Exception($"Bone {proportionsBone.Name} in proportions model has no equivalent slot in the rig mapping");
                }
            }








            // Import the reference animations from packaged resources
            string referenceMalePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Animations", "reference_male.smd");
            reference_male = SmdImporter.ImportSmd(referenceMalePath);
            string referenceFemalePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Animations", "reference_female.smd");
            reference_female = SmdImporter.ImportSmd(referenceFemalePath);

            // Then copy the rotations of the proportions bones to the reference bones

            // Make sure they all have root bones
            if (proportions.RootBone == null)
                throw new Exception("Proportions model must have a root bone");
            if (reference_male.RootBone == null)
                throw new Exception("Reference male model must have a root bone");
            if (reference_female.RootBone == null)
                throw new Exception("Reference female model must have a root bone");

            // Copy the rotations of the proportions bones to the reference bones
            var proportionsBoneDict = proportions.RootBone.GetAllDescendantsAndSelf().ToDictionary(b => b.Name);
            var referenceMaleBoneDict = reference_male.RootBone.GetAllDescendantsAndSelf().ToDictionary(b => b.Name);
            var referenceFemaleBoneDict = reference_female.RootBone.GetAllDescendantsAndSelf().ToDictionary(b => b.Name);

            foreach (var boneName in proportionsBoneDict.Keys)
            {
                // We pray that LocalRotation setter works correctly until we can test this

                if (referenceMaleBoneDict.ContainsKey(boneName))
                {
                    referenceMaleBoneDict[boneName].LocalRotation = proportionsBoneDict[boneName].LocalRotation;
                }
                if (referenceFemaleBoneDict.ContainsKey(boneName))
                {
                    referenceFemaleBoneDict[boneName].LocalRotation = proportionsBoneDict[boneName].LocalRotation;
                }
            }


        }
    }
}
