using Yggdrasil.Application.Abstractions;
using Yggdrasil.Domain.Project;
using Yggdrasil.Domain.Scene;

namespace Yggdrasil.Application.UseCases
{
    /// <summary>
    /// Handles exporting materials from the project to VMT and VTF files.
    /// This includes generating VMT content based on material settings, baking textures using the specified formats, and writing the output files to the appropriate directory structure for use in Source engine addons.
    /// </summary>
    public sealed class ExportMaterialsUseCase
    {
        private readonly IMaterialExporter _materialExporter;
        private readonly IMaterialBaker _materialBaker;

        public ExportMaterialsUseCase(IMaterialExporter materialExporter, IMaterialBaker materialBaker)
        {
            _materialExporter = materialExporter;
            _materialBaker = materialBaker;
        }

        public ExportMaterialsResult Execute(ExportMaterialsRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Project is null)
            {
                return new ExportMaterialsResult(false, "Project is required for material export.");
            }

            var scene = request.Project.Scene;
            if (scene.MaterialSettings.Count == 0)
            {
                return new ExportMaterialsResult(false, "No materials are available to export.");
            }

            if (string.IsNullOrWhiteSpace(request.Project.Build.AddonDirectory))
            {
                return new ExportMaterialsResult(false, "Set an addon root on the Project page before exporting materials.");
            }

            // We need VTFCmd, make sure we have it
            var bundledVtfCmdPath = PackagedToolPaths.GetVtfCmdPath();
            if (!File.Exists(bundledVtfCmdPath))
            {
                return new ExportMaterialsResult(
                    false,
                    $"This build does not include the bundled VTFCmd tool. Expected it at '{bundledVtfCmdPath}'.");
            }

            // Find the output directory for the materials
            var addonDirectory = ResolveAddonDirectory(request.Project);
            var relativeMaterialDirectory = NormalizeRelativeMaterialDirectory(
                request.Project.Qc.CdMaterialsPaths.FirstOrDefault());
            var outputDirectory = BuildMaterialOutputDirectory(addonDirectory, relativeMaterialDirectory);

            Directory.CreateDirectory(outputDirectory);

            // Build lookup tables for material export
            var allMaterials = scene.MaterialSettings.Values
                .OrderBy(material => material.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var selectedMaterials = ResolveTargetMaterials(scene, request.MaterialNames);
            if (selectedMaterials.Count == 0)
            {
                return new ExportMaterialsResult(false, "No matching materials were found for export.");
            }

            var textureNames = BuildUniqueTextureNames(allMaterials);
            var materialFileNames = BuildUniqueMaterialFileNames(allMaterials);
            var usedInternalTextures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Foreach material
            foreach (var material in selectedMaterials)
            {
                // Make the content, getting a list of used textures
                var vmtContents = _materialExporter.GenerateVMT(
                    material,
                    relativeMaterialDirectory,
                    textureNames,
                    out var internalTextures);

                // Add any internal textures to the used list for baking
                if (internalTextures is not null)
                {
                    foreach (var internalTexture in internalTextures)
                    {
                        usedInternalTextures.Add(internalTexture);
                    }
                }

                // Write the VMT to the output directory with a unique name
                var fileName = materialFileNames[material.Name];
                var outputPath = Path.Combine(outputDirectory, $"{fileName}.vmt");
                File.WriteAllText(outputPath, vmtContents);
            }

            // Convert and write all used textures to the output directory with unique names
            var bakeResult = _materialBaker.BakeTextures(new MaterialBakeRequest
            {
                Materials = selectedMaterials,
                UniqueTextureNames = textureNames,
                OutputDirectory = outputDirectory,
                VtfCmdPath = bundledVtfCmdPath,
                DefaultTextureFormat = request.Project.Build.MaterialExport.DefaultTextureFormat,
                NormalMapFormat = request.Project.Build.MaterialExport.NormalMapFormat,
                UsedInternalTextures = usedInternalTextures.Count == 0
                    ? null
                    : usedInternalTextures.ToList()
            });

            if (!bakeResult.Success)
            {
                return new ExportMaterialsResult(false, bakeResult.ErrorMessage ?? "Material texture export failed.");
            }

            // Return success with a message about how many materials were exported, and any messages/warnings from the bake step
            var result = new ExportMaterialsResult(true);
            result.Messages.Add(
                $"Wrote {selectedMaterials.Count} VMT file{(selectedMaterials.Count == 1 ? string.Empty : "s")} to '{outputDirectory}'.");

            if (bakeResult.HasMessages)
            {
                result.Messages.AddRange(bakeResult.Messages);
            }

            if (bakeResult.HasWarnings)
            {
                result.Warnings.AddRange(bakeResult.Warnings);
            }

            return result;
        }

        // Resolves the addon directory, making it absolute if it's relative to the project directory
        private static string ResolveAddonDirectory(Project project)
        {
            var addonDirectory = project.Build.AddonDirectory;
            if (!Path.IsPathRooted(addonDirectory) && !string.IsNullOrWhiteSpace(project.Directory))
            {
                return Path.GetFullPath(Path.Combine(project.Directory, addonDirectory));
            }

            return addonDirectory;
        }

        // Resolves the target materials based on the requested material names. If no names are provided, all materials are returned.
        private static List<SourceMaterialSettings> ResolveTargetMaterials(
            SceneModel scene,
            IReadOnlyCollection<string>? requestedMaterialNames)
        {
            if (requestedMaterialNames is null || requestedMaterialNames.Count == 0)
            {
                return scene.MaterialSettings.Values
                    .OrderBy(material => material.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var requested = new HashSet<string>(requestedMaterialNames, StringComparer.OrdinalIgnoreCase);

            return scene.MaterialSettings
                .Where(kvp => requested.Contains(kvp.Key))
                .Select(kvp => kvp.Value)
                .OrderBy(material => material.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Make sure all textures properly convert, even if materials use textures that share a name, but are in different places
        private static Dictionary<string, string> BuildUniqueTextureNames(
            IEnumerable<SourceMaterialSettings> materials)
        {
            var texturePaths = materials
                .SelectMany(GetReferencedTexturePaths)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return BuildUniqueNameMap(texturePaths);
        }

        // Make sure materials get unique file names, even if they share the same name in the source data
        private static Dictionary<string, string> BuildUniqueMaterialFileNames(
            IEnumerable<SourceMaterialSettings> materials)
        {
            var materialNames = materials
                .Select(material => material.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return BuildUniqueNameMap(materialNames);
        }

        // Builds a map from source values to unique, sanitized file stem candidates. If there are duplicates after sanitization, suffixes are added to ensure uniqueness.
        private static Dictionary<string, string> BuildUniqueNameMap(IEnumerable<string> sourceValues)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var usedNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var sourceValue in sourceValues)
            {
                var baseName = SanitizeFileStem(sourceValue);
                var candidate = baseName;

                if (usedNames.TryGetValue(candidate, out var suffix))
                {
                    do
                    {
                        suffix++;
                        candidate = $"{baseName}_{suffix}";
                    }
                    while (usedNames.ContainsKey(candidate));

                    usedNames[baseName] = suffix;
                }
                else
                {
                    usedNames[baseName] = 0;
                }

                usedNames[candidate] = 0;
                result[sourceValue] = candidate;
            }

            return result;
        }

        // Gets all texture paths referenced by a material, ignoring null or whitespace entries
        private static IEnumerable<string> GetReferencedTexturePaths(SourceMaterialSettings material)
        {
            if (!string.IsNullOrWhiteSpace(material.BaseTexture))
            {
                yield return material.BaseTexture;
            }

            if (!string.IsNullOrWhiteSpace(material.BumpMap))
            {
                yield return material.BumpMap;
            }

            if (!string.IsNullOrWhiteSpace(material.LightWarpTexture))
            {
                yield return material.LightWarpTexture;
            }

            if (!string.IsNullOrWhiteSpace(material.EmissiveTexture))
            {
                yield return material.EmissiveTexture;
            }

            if (!string.IsNullOrWhiteSpace(material.EnvMap))
            {
                yield return material.EnvMap;
            }

            if (!string.IsNullOrWhiteSpace(material.EnvMapMask))
            {
                yield return material.EnvMapMask;
            }

            if (!string.IsNullOrWhiteSpace(material.PhongExponentTexture))
            {
                yield return material.PhongExponentTexture;
            }
        }

        // Sanitizes a string to be used as a file stem by removing invalid characters and replacing them with underscores. If the result is empty, "material" is returned as a default.
        private static string SanitizeFileStem(string input)
        {
            var fileStem = Path.GetFileNameWithoutExtension(input?.Trim());
            if (string.IsNullOrWhiteSpace(fileStem))
            {
                fileStem = "material";
            }

            foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            {
                fileStem = fileStem.Replace(invalidCharacter, '_');
            }

            fileStem = fileStem.Replace('/', '_').Replace('\\', '_');
            return string.IsNullOrWhiteSpace(fileStem) ? "material" : fileStem;
        }

        // Normalizes relative material paths by trimming whitespace, replacing backslashes with forward slashes, and removing leading/trailing slashes. If the input is null, it is treated as an empty string.
        private static string NormalizeRelativeMaterialDirectory(string? relativeMaterialDirectory)
        {
            return (relativeMaterialDirectory ?? string.Empty)
                .Trim()
                .Replace('\\', '/')
                .Trim('/');
        }

        // Builds the output directory by combining addon (C:/program files/steam/... .../addons/myaddon) and relative material directory (e.g. "materials/models/jeff")
        private static string BuildMaterialOutputDirectory(string addonDirectory, string relativeMaterialDirectory)
        {
            var outputDirectory = Path.Combine(addonDirectory, "materials");

            if (string.IsNullOrWhiteSpace(relativeMaterialDirectory))
            {
                return outputDirectory;
            }

            var segments = relativeMaterialDirectory
                .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var segment in segments)
            {
                outputDirectory = Path.Combine(outputDirectory, segment);
            }

            return outputDirectory;
        }
    }

    public sealed class ExportMaterialsResult : ServiceResult
    {
        public ExportMaterialsResult(bool success, string? error = null) : base(success, error)
        {
        }
    }

    public sealed class ExportMaterialsRequest
    {
        public required Project Project { get; init; }
        public IReadOnlyCollection<string>? MaterialNames { get; init; }
    }
}
