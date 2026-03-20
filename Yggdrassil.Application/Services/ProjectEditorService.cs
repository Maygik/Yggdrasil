using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application.UseCases;
using Yggdrassil.Domain.Project;
using Yggdrassil.Domain.QC;
using Yggdrassil.Domain.Scene;

namespace Yggdrassil.Application.Services
{
    public class ProjectEditorService
    {
        public ProjectEditorService() { }

        /// <summary>
        /// Renames the project
        /// </summary>
        /// <param name="project"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public ServiceResult Rename(Project project, string newName)
        {
            project.Name = newName;

            var result = new ServiceResult(true);
            result.Messages.Add($"Project renamed to '{newName}' successfully.");
            return result;
        }

        /// <summary>
        /// Sets the output directory relative to the project root
        /// </summary>
        /// <param name="project"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        public ServiceResult SetOutputDirectory(Project project, string directory)
        {
            project.Build.OutputDirectory = directory;

            var result = new ServiceResult(true);
            result.Messages.Add($"Project output directory set to '{project.Directory + project.Build.OutputDirectory}' successfully.");
            return result;
        }

        /// <summary>
        /// Scales the model and bone positions by scaleFactor
        /// </summary>
        /// <param name="project"></param>
        /// <param name="scaleFactor"></param>
        /// <returns></returns>
        public ServiceResult Scale(Project project, float scaleFactor)
        {
            var result = new ServiceResult(false);

            if (project.Scene == null)
            {
                result.ErrorMessage = "No scene to scale. Please import a model first.";
                return result;
            }

            if (project.Scene.RootBone != null)
            {
                // Scale all bone positions
                void ScaleBone(Bone bone)
                {
                    bone.LocalPosition = new Vector3<float>(
                        bone.LocalPosition.X * scaleFactor,
                        bone.LocalPosition.Y * scaleFactor,
                        bone.LocalPosition.Z * scaleFactor
                    );
                    foreach (var child in bone.Children)
                    {
                        var childBone = child as Bone;
                        if (childBone != null)
                            ScaleBone(childBone);
                    }
                }
                ScaleBone(project.Scene.RootBone);
            }
            else
            {
                result.Warnings.Add("No root bone. Only scaling vertices");
            }

            // Scale vertices
            result.Messages.Add("Scaling all vertex positions");
            foreach (var meshGroup in project.Scene.MeshGroups)
            {
                foreach (var mesh in meshGroup.Meshes)
                {
                    for (int i = 0; i < mesh.Vertices.Count; i++)
                    {
                        var vertex = mesh.Vertices[i];
                        vertex.X *= scaleFactor;
                        vertex.Y *= scaleFactor;
                        vertex.Z *= scaleFactor;
                        mesh.Vertices[i] = vertex;
                    }
                }
            }

            result.Messages.Add($"Finished scaling model by a factor of {scaleFactor}");

            result.Success = true;
            return result;
        }

        /// <summary>
        /// Sets the $modelname line
        /// </summary>
        /// <param name="project"></param>
        /// <param name="modelName"></param>
        /// <returns></returns>
        public ServiceResult SetModelPath(Project project, string modelName)
        {
            project.Qc.ModelPath = modelName;
            var result = new ServiceResult(true);
            result.Messages.Add($"Set model path to {modelName}");
            result.Messages.Add($"Model will now compile to \"ADDON_NAME/models/{modelName}.mdl\"");
            return result;
        }


        public ServiceResult BindBone(Project project, string modelBone, string slotName)
        {
            var result = new ServiceResult(false);

            var slot = project.RigMapping.TryGetRigSlotFromName(slotName);
            if (slot == null)
            {
                result.ErrorMessage = $"Could not find a bone slot matching: \"{slotName}\". Please specify a valid slot index or name.";
                return result;
            }

            slot.AssignedBone = modelBone;
            result.Success = true;
            result.Messages.Add($"Bound model bone '{modelBone}' to slot '{slot.DisplayName}'");
            return result;
        }

        public ServiceResult BindBone(Project project, string modelBone, int slotIndex)
        {
            var result = new ServiceResult(false);

            if (slotIndex < 0 || slotIndex >= project.RigMapping.Count)
            {
                result.ErrorMessage = $"Invalid slot index: {slotIndex}";
                return result;
            }

            var slot = project.RigMapping[slotIndex];
            slot.AssignedBone = modelBone;
            result.Success = true;
            result.Messages.Add($"Bound model bone '{modelBone}' to slot '{slot.DisplayName}'");
            return result;
        }

        public ServiceResult UnbindBone(Project project, string slotName)
        {
            var result = new ServiceResult(false);

            var slot = project.RigMapping.TryGetRigSlotFromName(slotName);
            if (slot == null)
            {
                result.ErrorMessage = $"Could not find a bone slot matching: \"{slotName}\". Please specify a valid slot index or name.";
                return result;
            }

            slot.AssignedBone = null;
            result.Success = true;
            result.Messages.Add($"Cleared binding for slot: \"{slot.DisplayName}\"");
            return result;
        }

        public ServiceResult UnbindBone(Project project, int slotIndex)
        {
            var result = new ServiceResult(false);

            if (slotIndex < 0 || slotIndex >= project.RigMapping.Count)
            {
                result.ErrorMessage = $"Invalid slot index: {slotIndex}";
                return result;
            }

            var slot = project.RigMapping[slotIndex];
            slot.AssignedBone = null;
            result.Success = true;
            result.Messages.Add($"Cleared binding for slot: \"{slot.DisplayName}\"");
            return result;
        }


        public ServiceResult SetAnimationProfile(Project project, AnimationProfile profile)
        {
            project.Qc.AnimationProfile = profile;
            var result = new ServiceResult(true);
            result.Messages.Add($"Set animation profile to {profile.ToString()}");
            return result;
        }

        public ServiceResult AddMaterialPath(Project project, string materialPath)
        {
            if (!project.Qc.CdMaterialsPaths.Contains(materialPath))
            {
                project.Qc.CdMaterialsPaths.Add(materialPath);
                var result = new ServiceResult(true);
                result.Messages.Add($"Added material path: {materialPath}");
                return result;
            }
            else
            {
                var result = new ServiceResult(false);
                result.ErrorMessage = $"Material path already exists: {materialPath}";
                return result;
            }
        }

        public ServiceResult RemoveMaterialPath(Project project, string materialPath)
        {
            var result = new ServiceResult(false);
            if (!project.Qc.CdMaterialsPaths.Contains(materialPath))
            {
                result.ErrorMessage = $"Material path not found: {materialPath}";
                return result;
            }
            project.Qc.CdMaterialsPaths.Remove(materialPath);
            result.Success = true;
            result.Messages.Add($"Removed material path: {materialPath}");
            return result;
        }


        public ServiceResult SetSurfaceProp(Project project, string surfaceProp)
        {
            project.Qc.SurfaceProp = surfaceProp;
            var result = new ServiceResult(true);
            result.Messages.Add($"Set surface prop to {surfaceProp}");
            return result;
        }


        public ServiceResult AddBodygroup(Project project, string bodygroupName, List<string?> submodelNames)
        {
            var result = new ServiceResult(false);

            // If the bodygroup already exists, append to it
            // Otherwise, create a new one
            var bodyGroup = project.Qc.Bodygroups.FirstOrDefault(bg => bg.Name == bodygroupName);
            if (bodyGroup != null)
            {
                bodyGroup.Submeshes.AddRange(submodelNames);
                result.Success = true;
                result.Messages.Add($"Appended {submodelNames.Count} submodels to bodygroup '{bodygroupName}'.");
            }
            else
            {
                project.Qc.Bodygroups.Add(new Domain.QC.Bodygroup(bodygroupName, submodelNames));
                result.Success = true;
                result.Messages.Add($"Created new bodygroup '{bodygroupName}' with {submodelNames.Count} submodels.");
            }

            return result;
        }

        public ServiceResult RemoveBodygroup(Project project, string bodygroupName)
        {
            var bodyGroup = project.Qc.Bodygroups.FirstOrDefault(bg => bg.Name == bodygroupName);
            if (bodyGroup != null)
            {
                project.Qc.Bodygroups.Remove(bodyGroup);
                var result = new ServiceResult(true);
                result.Messages.Add($"Removed bodygroup '{bodygroupName}'.");
                return result;
            }
            else
            {
                return new ServiceResult(false, $"Bodygroup \"{bodygroupName}\" does not exist");
            }
        }

    }
}
