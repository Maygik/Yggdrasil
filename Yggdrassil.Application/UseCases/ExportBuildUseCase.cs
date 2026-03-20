using Yggdrassil.Application.Abstractions;
using Yggdrassil.Domain.Project;
using Yggdrassil.Domain.QC;
using Yggdrassil.Domain.Scene;

namespace Yggdrassil.Application.UseCases
{
    public class ExportBuildUseCase
    {
        private readonly IMeshExporter _exporter;
        private readonly IQcAssembler _assembler;
        private readonly IProportionTrickService _proportionTrickService;

        public ExportBuildUseCase(IMeshExporter exporter, IQcAssembler assembler, IProportionTrickService proportionTrickService)
        {
            _exporter = exporter;
            _assembler = assembler;
            _proportionTrickService = proportionTrickService;
        }

        public ExportBuildResult Execute(ExportBuildRequest request)
        {
            if (request.Project == null)
            {
                return new ExportBuildResult(false, "Project is required for export.");
            }

            var exporter = request.ExporterOverride ?? _exporter;

            string outputDirectory = request.outputDirectoryOverride ?? request.Project.Build.OutputDirectory;

            if (string.IsNullOrEmpty(outputDirectory))
            {
                return new ExportBuildResult(false, "Output directory is not set in the project settings or the request.");
            }

            var result = new ExportBuildResult(true);
            result.Messages.Add($"Export output directory: '{outputDirectory}'.");

            if (request.exportMeshes)
            {
                // Export the animations (Do here just to get proportions)

                ProportionTrickResult? proportions = null;

                if (request.Project.Scene.RootBone != null)
                {
                    //Console.WriteLine($"Exporting animations...");

                    // Check if we need proportion trick
                    List<AnimationProfile> profilesRequiringProportionTrick = new List<AnimationProfile>()
                    {
                        Domain.QC.AnimationProfile.MalePlayer,
                        Domain.QC.AnimationProfile.FemalePlayer,
                        Domain.QC.AnimationProfile.MaleNPC,
                        Domain.QC.AnimationProfile.FemaleNPC,
                        Domain.QC.AnimationProfile.CombineNPC,
                        Domain.QC.AnimationProfile.MetrocopNPC
                    };

                    var animOutputDir = System.IO.Path.Combine(outputDirectory, "anims");
                    if (profilesRequiringProportionTrick.Contains(request.Project.Qc.AnimationProfile))
                    {
                        //Console.WriteLine($"Applying proportion trick for animation export due to selected animation profile: {request.Project.Qc.AnimationProfile}");

                        // Create a temporary project copy with a cloned scene to avoid modifying the original
                        var tempProject = new Project
                        {
                            Scene = request.Project.Scene.DeepClone(),
                            RigMapping = request.Project.RigMapping,
                            Qc = request.Project.Qc
                        };

                        proportions = _proportionTrickService.Build(tempProject);
                        result.Messages.Add($"Built proportion-trick animations for profile '{request.Project.Qc.AnimationProfile}'.");

                        // Export animations to /anims
                        exporter.ExportAnimationAsync(animOutputDir, "proportions", proportions.Proportions);
                        exporter.ExportAnimationAsync(animOutputDir, "reference_male", proportions.ReferenceMale);
                        exporter.ExportAnimationAsync(animOutputDir, "reference_female", proportions.ReferenceFemale);
                        result.Messages.Add($"Wrote animation set to '{animOutputDir}'.");
                    }
                    else
                    {
                        // Just export a normal skeleton with the bind pose as the only frame
                        exporter.ExportAnimationAsync(animOutputDir, "ragdoll", request.Project.Scene);
                        result.Messages.Add($"Wrote ragdoll animation to '{animOutputDir}'.");
                    }

                }
                else
                {
                    result.Warnings.Add("Scene has no root bone. Exporting meshes without skeleton animations.");
                }


                // Copy over the proportions armature
                if (proportions != null)
                    request.Project.Scene.RootBone = proportions.Proportions.RootBone;


                if (request.Project.Scene.MeshGroups.Count == 0)
                {
                    result.Warnings.Add("Scene has no meshes to export.");
                }
                else
                {
                    exporter.ExportSceneAsync(outputDirectory, request.Project.Scene);
                    result.Messages.Add($"Exported meshes to '{outputDirectory}'.");
                }

            }

            if (request.exportQc)
            {
                var qc = _assembler.AssembleQc(request.Project.Qc);
                var qcPath = System.IO.Path.Combine(outputDirectory, $"{request.Project.Name}.qc");
                System.IO.File.WriteAllText(qcPath, qc);
                result.Messages.Add($"Wrote QC file to '{qcPath}'.");
            }

            if (!request.exportMeshes && !request.exportQc)
            {
                result.Warnings.Add("Nothing was selected for export.");
            }

            return result;
        }
    }

    public class ExportBuildResult : ServiceResult
    {
        public ExportBuildResult(bool success, string? error = null) : base(success, error)
        {

        }
    }

    public class ExportBuildRequest
    {
        public required Project Project;
        public IMeshExporter? ExporterOverride { get; init; }
        public bool exportMeshes;
        public bool exportQc;
        public string? outputDirectoryOverride;
    }
}
