using Yggdrasil.Application.Abstractions;
using Yggdrasil.Domain.Project;
using Yggdrasil.Domain.Scene;

namespace Yggdrasil.Application.UseCases
{
    /// <summary>
    /// Handles importing a model file into the project. This includes reading the model file, extracting mesh groups and bones, and updating the project state accordingly.
    /// </summary>
    public class ImportModelUseCase
    {
        private readonly IModelImporter _modelImporter;

        public ImportModelUseCase(IModelImporter modelImporter)
        {
            _modelImporter = modelImporter;
        }

        public ImportModelResult Execute(ImportModelRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ModelPath))
            {
                return new ImportModelResult(false, "Model path cannot be empty.");
            }

            try
            {
                var scene = _modelImporter.ImportModelAsync(request.ModelPath).Result;
                request.Project.Scene = scene;
                if (scene.RootBone != null)
                {
                    request.Project.Qc.IllumPosition = scene.RootBone.WorldPosition;
                }

                var result = new ImportModelResult(true, null);
                result.Messages.Add($"Imported model '{request.ModelPath}'.");
                result.Messages.Add($"Loaded {scene.MeshGroups.Count} mesh groups.");
                if (scene.RootBone != null)
                {
                    result.Messages.Add($"Set QC illum bone to root bone '{scene.RootBone.Name}'.");
                }

                // Auto-add every mesh group as a bodygroup with a single submodel
                int addedBodygroups = 0;
                foreach (var meshGroup in scene.MeshGroups)
                {
                    if (!request.Project.Qc.Bodygroups.Any(bg => bg.Name == meshGroup.Name))
                    {
                        request.Project.Qc.Bodygroups.Add(new Domain.QC.Bodygroup(meshGroup.Name, new List<string?>() { meshGroup.Name }));
                        addedBodygroups++;
                    }
                }

                if (addedBodygroups > 0)
                {
                    result.Messages.Add($"Added {addedBodygroups} bodygroups from imported mesh groups.");
                }

                // Attemp to automatically map bones to slots
                if (request.AutoMap)
                {
                    if (scene.RootBone == null)
                    {
                        result.Warnings.Add("Scene has no root bone. Skipping automatic rig mapping.");
                    }
                    else
                    {
                        int mappedSlots = 0;
                        void CheckBoneMap(Bone bone)
                        {
                            var slot = request.Project.RigMapping.TryGetRigSlotFromName(bone.Name);

                            if (slot != null)
                            {
                                if (slot.AssignedBone == null)
                                {
                                    mappedSlots++;
                                }
                                slot.AssignedBone = bone.Name;
                            }

                            foreach (var child in bone.Children)
                            {
                                var childBone = child as Bone;
                                if (childBone != null)
                                    CheckBoneMap(childBone);
                            }
                        }
                        CheckBoneMap(scene.RootBone);
                        result.Messages.Add($"Automatically mapped {mappedSlots} rig slots.");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                return new ImportModelResult(false, ex.Message);
            }
        }
    }

    public class ImportModelResult : ServiceResult
    {
        public ImportModelResult(bool success, string? error = null) : base(success, error)
        {

        }
    }

    public class ImportModelRequest
    {
        public string ModelPath { get; set; }
        public Project Project { get; set; }
        public bool AutoMap { get; set; }

        public ImportModelRequest(string path, Project project, bool autoMap = false)
        {
            ModelPath = path;
            Project = project;
            AutoMap = autoMap;
        }
    }
}
