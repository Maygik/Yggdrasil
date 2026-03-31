using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Application.UseCases;
using Yggdrasil.Domain.Project;
using Yggdrasil.Domain.QC;
using Yggdrasil.Domain.Scene;
using Matrix4x4 = Yggdrasil.Types.Matrix4x4;
using Vector3 = Yggdrasil.Types.Vector3;

namespace Yggdrasil.Application.Services
{
    /// <summary>
    /// Handles editing of project properties and settings such as project name, output directory, model scale/rotation/translation, QC config settings, rig bindings, etc.r
    /// </summary>
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
            project.Build.OutputDirectory = NormalizeProjectRelativePath(project, directory);

            var result = new ServiceResult(true);
            result.Messages.Add($"Project output directory set to '{project.Build.OutputDirectory}' successfully.");
            result.Messages.Add($"Output directory is now stored relative to the project root.");
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

            if (!float.IsFinite(scaleFactor) || scaleFactor <= 0.0f)
            {
                result.ErrorMessage = "Scale factor must be a finite value greater than zero.";
                return result;
            }

            if (project.Scene.RootBone != null)
            {
                // Scale all bone positions
                void ScaleBone(Bone bone)
                {
                    bone.LocalPosition = new Vector3(
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
                result.Warnings.Add("No root bone. Only scaling mesh transforms.");
            }

            foreach (var meshGroup in project.Scene.MeshGroups)
            {
                meshGroup.LocalMatrix = Yggdrasil.Types.SceneAlignmentMath.CreateAlignmentMatrix(
                    scaleFactor,
                    Vector3.Zero,
                    Vector3.Zero) * meshGroup.LocalMatrix;
            }

            result.Messages.Add($"Finished scaling model by a factor of {scaleFactor}.");
            result.Messages.Add("Scale is now stored on the model itself and will persist when saving.");

            result.Success = true;
            return result;
        }

        public ServiceResult Rotate(Project project, Vector3 rotationDegrees)
        {
            var result = new ServiceResult(false);

            if (project.Scene == null)
            {
                result.ErrorMessage = "No scene to rotate. Please import a model first.";
                return result;
            }

            if (!IsFiniteVector(rotationDegrees))
            {
                result.ErrorMessage = "Rotation must use finite numeric values.";
                return result;
            }

            if (rotationDegrees == Vector3.Zero)
            {
                result.ErrorMessage = "Rotation values are all zero.";
                return result;
            }

            var rotationMatrix = Yggdrasil.Types.SceneAlignmentMath.CreateRotationMatrix(rotationDegrees);
            ApplySceneTransform(project.Scene, rotationMatrix);

            result.Success = true;
            result.Messages.Add(
                $"Rotated model by ({rotationDegrees.X}, {rotationDegrees.Y}, {rotationDegrees.Z}) degrees.");
            result.Messages.Add("Rotation is saved with the project and applied to exports.");
            return result;
        }

        public ServiceResult Translate(Project project, Vector3 translation)
        {
            var result = new ServiceResult(false);

            if (project.Scene == null)
            {
                result.ErrorMessage = "No scene to move. Please import a model first.";
                return result;
            }

            if (!IsFiniteVector(translation))
            {
                result.ErrorMessage = "Position offset must use finite numeric values.";
                return result;
            }

            if (translation == Vector3.Zero)
            {
                result.ErrorMessage = "Position values are all zero.";
                return result;
            }

            var translationMatrix = Yggdrasil.Types.SceneAlignmentMath.CreateTranslationMatrix(translation);
            ApplySceneTransform(project.Scene, translationMatrix);

            result.Success = true;
            result.Messages.Add($"Moved model by ({translation.X}, {translation.Y}, {translation.Z}) units.");
            result.Messages.Add("Position changes are saved with the project and applied to exports.");
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

        /// <summary>
        /// Normalizes a path so it is always stored relative to the project root.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public string NormalizeProjectRelativePath(Project project, string path)
        {
            var trimmedPath = (path ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedPath))
            {
                return string.Empty;
            }

            if (Path.IsPathRooted(trimmedPath) && !string.IsNullOrWhiteSpace(project.Directory))
            {
                trimmedPath = Path.GetRelativePath(project.Directory, trimmedPath);
            }

            trimmedPath = trimmedPath.Replace('\\', '/').TrimStart('/');
            return trimmedPath;
        }

        public ServiceResult UpdateMaterial(Project project, string materialName, Action<SourceMaterialSettings> applyChanges)
        {
            var result = new ServiceResult(false);

            if (project.Scene == null)
            {
                result.ErrorMessage = "No scene is loaded for this project.";
                return result;
            }

            if (!project.Scene.MaterialSettings.TryGetValue(materialName, out var material))
            {
                result.ErrorMessage = $"Material '{materialName}' was not found in the current scene.";
                return result;
            }

            applyChanges(material);
            material.Adjusted = true;

            result.Success = true;
            result.Messages.Add($"Updated material '{materialName}'.");
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

        public ServiceResult RemoveMaterialPath(Project project, int materialPathIndex)
        {
            var result = new ServiceResult(false);

            if (materialPathIndex < 0 || materialPathIndex >= project.Qc.CdMaterialsPaths.Count)
            {
                result.ErrorMessage = $"Material path index '{materialPathIndex}' is out of range";
                return result;
            }

            var removedPath = project.Qc.CdMaterialsPaths[materialPathIndex];
            project.Qc.CdMaterialsPaths.RemoveAt(materialPathIndex);
            result.Success = true;
            result.Messages.Add($"Removed material path at index {materialPathIndex}: {removedPath}");
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
            var result = new ServiceResult(true);
            project.Qc.Bodygroups.Add(new Domain.QC.Bodygroup(bodygroupName, submodelNames));
            result.Messages.Add($"Created new bodygroup '{bodygroupName}' with {submodelNames.Count} submodels.");
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

        public ServiceResult RemoveBodygroup(Project project, int bodygroupIndex)
        {
            if (bodygroupIndex < 0 || bodygroupIndex >= project.Qc.Bodygroups.Count)
            {
                return new ServiceResult(false, $"Bodygroup index '{bodygroupIndex}' is out of range");
            }

            var bodygroupName = project.Qc.Bodygroups[bodygroupIndex].Name;
            project.Qc.Bodygroups.RemoveAt(bodygroupIndex);
            var result = new ServiceResult(true);
            result.Messages.Add($"Removed bodygroup '{bodygroupName}' at index {bodygroupIndex}.");
            return result;
        }

        public ServiceResult AddBodygroupOption(Project project, string bodygroupName, string? submodelName)
        {
            var bodyGroup = project.Qc.Bodygroups.FirstOrDefault(bg => bg.Name == bodygroupName);
            if (bodyGroup == null)
            {
                return new ServiceResult(false, $"Bodygroup \"{bodygroupName}\" does not exist");
            }
            bodyGroup.Submeshes.Add(submodelName);
            var result = new ServiceResult(true);
            result.Messages.Add($"Added submodel \"{submodelName}\" to bodygroup \"{bodygroupName}\"");
            return result;
        }

        public ServiceResult AddBodygroupOption(Project project, int bodygroupIndex, string? submodelName)
        {
            if (bodygroupIndex < 0 || bodygroupIndex >= project.Qc.Bodygroups.Count)
            {
                return new ServiceResult(false, $"Bodygroup index '{bodygroupIndex}' is out of range");
            }

            var bodyGroup = project.Qc.Bodygroups[bodygroupIndex];
            bodyGroup.Submeshes.Add(submodelName);
            var result = new ServiceResult(true);
            result.Messages.Add($"Added submodel \"{submodelName}\" to bodygroup '{bodyGroup.Name}' at index {bodygroupIndex}.");
            return result;
        }

        public ServiceResult RemoveBodygroupOption(Project project, string bodygroupName, string? submodelName)
        {
            var bodyGroup = project.Qc.Bodygroups.FirstOrDefault(bg => bg.Name == bodygroupName);
            if (bodyGroup == null)
            {
                return new ServiceResult(false, $"Bodygroup \"{bodygroupName}\" does not exist");
            }
            if (!bodyGroup.Submeshes.Contains(submodelName))
            {
                return new ServiceResult(false, $"Submodel \"{submodelName}\" does not exist in bodygroup \"{bodygroupName}\"");
            }
            bodyGroup.Submeshes.Remove(submodelName);
            var result = new ServiceResult(true);
            result.Messages.Add($"Removed submodel \"{submodelName}\" from bodygroup \"{bodygroupName}\"");
            return result;
        }

        public ServiceResult RemoveBodygroupOption(Project project, int bodygroupIndex, int optionIndex)
        {
            if (bodygroupIndex < 0 || bodygroupIndex >= project.Qc.Bodygroups.Count)
            {
                return new ServiceResult(false, $"Bodygroup index '{bodygroupIndex}' is out of range");
            }

            var bodyGroup = project.Qc.Bodygroups[bodygroupIndex];
            if (optionIndex < 0 || optionIndex >= bodyGroup.Submeshes.Count)
            {
                return new ServiceResult(false, $"Option index '{optionIndex}' is out of range for bodygroup '{bodyGroup.Name}'");
            }

            var removedValue = bodyGroup.Submeshes[optionIndex];
            bodyGroup.Submeshes.RemoveAt(optionIndex);
            var result = new ServiceResult(true);
            result.Messages.Add($"Removed submodel \"{removedValue}\" from bodygroup '{bodyGroup.Name}' at option index {optionIndex}.");
            return result;
        }

        private static void ApplySceneTransform(SceneModel scene, Matrix4x4 transformMatrix)
        {
            foreach (var meshGroup in scene.MeshGroups)
            {
                meshGroup.LocalMatrix = transformMatrix * meshGroup.LocalMatrix;
            }

            if (scene.RootBone != null)
            {
                scene.RootBone.WorldMatrix = transformMatrix * scene.RootBone.WorldMatrix;
            }
        }

        private static bool IsFiniteVector(Vector3 value)
        {
            return float.IsFinite(value.X)
                && float.IsFinite(value.Y)
                && float.IsFinite(value.Z);
        }
    }
}
