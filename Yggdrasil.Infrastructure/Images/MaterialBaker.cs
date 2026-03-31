using System.Reflection;
using Yggdrasil.Application.Abstractions;
using Yggdrasil.Application.UseCases;
using Yggdrasil.Domain.Project;

namespace Yggdrasil.Infrastructure.Images
{
    /// <summary>
    /// Implements source-specific texture baking logic rules
    /// (e.g. normal alpha packing, phong exponent)
    /// </summary>
    internal sealed class MaterialBaker : IMaterialBaker
    {
        private static readonly IReadOnlyDictionary<string, string> InternalTextureResources =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["white"] = "Yggdrasil.Infrastructure.Resources.Textures.white.png",
                ["black"] = "Yggdrasil.Infrastructure.Resources.Textures.black.png",
                ["neutral"] = "Yggdrasil.Infrastructure.Resources.Textures.neutral.png",
                ["normal"] = "Yggdrasil.Infrastructure.Resources.Textures.normal.png"
            };

        private readonly IVTFWriter _vtfWriter;

        public MaterialBaker(IVTFWriter vtfWriter)
        {
            _vtfWriter = vtfWriter;
        }

        public ServiceResult BakeTextures(MaterialBakeRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);


            // Get the list of textures to export
            var result = new ServiceResult(true);
            var exportJobs = BuildExportJobs(request, result);
            if (exportJobs.Count == 0)
            {
                result.Messages.Add("No VTF files were required for the selected materials.");
                return result;
            }

            Directory.CreateDirectory(request.OutputDirectory);

            var stagingDirectory = Path.Combine(
                Path.GetTempPath(),
                "Yggdrasil",
                "vtfcmd",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(stagingDirectory);

            try
            {
                var exportedCount = 0;

                foreach (var exportJob in exportJobs.Values.OrderBy(job => job.OutputName, StringComparer.OrdinalIgnoreCase))
                {
                    string? stagedSourcePath;
                    try
                    {
                        stagedSourcePath = StageSourceTexture(exportJob, stagingDirectory);
                    }
                    catch (Exception ex)
                    {
                        result.Warnings.Add($"Skipped texture '{exportJob.OutputName}': {ex.Message}");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(stagedSourcePath))
                    {
                        continue;
                    }

                    // Export the staged source texture to VTF
                    var writeResult = _vtfWriter.WriteVtf(new VtfWriteRequest
                    {
                        ToolPath = request.VtfCmdPath,
                        SourceImagePath = stagedSourcePath,
                        OutputDirectory = request.OutputDirectory,
                        Format = exportJob.Format,
                        MarkAsNormalMap = exportJob.MarkAsNormalMap
                    });

                    if (!writeResult.Success)
                    {
                        result.Warnings.Add(writeResult.ErrorMessage ?? $"Failed to write '{exportJob.OutputName}.vtf'.");
                        continue;
                    }

                    if (writeResult.HasWarnings)
                    {
                        result.Warnings.AddRange(writeResult.Warnings);
                    }

                    exportedCount++;
                }

                if (exportedCount > 0)
                {
                    result.Messages.Add(
                        $"Wrote {exportedCount} VTF file{(exportedCount == 1 ? string.Empty : "s")} to '{request.OutputDirectory}'.");
                }
                else
                {
                    result.Warnings.Add("No VTF files were written for the selected materials.");
                }

                return result;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(stagingDirectory))
                    {
                        Directory.Delete(stagingDirectory, recursive: true);
                    }
                }
                catch
                {
                    // Staging cleanup is best-effort only.
                }
            }
        }

        /// <summary>
        /// Builds the list of texture export jobs based on the material bake request, applying source-specific rules
        /// </summary>
        /// <param name="request"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private static Dictionary<string, TextureExportJob> BuildExportJobs(
            MaterialBakeRequest request,
            ServiceResult result)
        {
            var exportJobs = new Dictionary<string, TextureExportJob>(StringComparer.OrdinalIgnoreCase);

            foreach (var material in request.Materials)
            {
                TryAddExternalTexture(exportJobs, result, request.UniqueTextureNames, material.BaseTexture, request.DefaultTextureFormat, markAsNormalMap: false);
                TryAddExternalTexture(exportJobs, result, request.UniqueTextureNames, material.BumpMap, request.NormalMapFormat, markAsNormalMap: true);
                TryAddExternalTexture(exportJobs, result, request.UniqueTextureNames, material.LightWarpTexture, request.DefaultTextureFormat, markAsNormalMap: false);
                TryAddExternalTexture(exportJobs, result, request.UniqueTextureNames, material.EmissiveTexture, request.DefaultTextureFormat, markAsNormalMap: false);
                TryAddExternalTexture(exportJobs, result, request.UniqueTextureNames, material.EnvMap, request.DefaultTextureFormat, markAsNormalMap: false);
                TryAddExternalTexture(exportJobs, result, request.UniqueTextureNames, material.EnvMapMask, request.DefaultTextureFormat, markAsNormalMap: false);
                TryAddExternalTexture(exportJobs, result, request.UniqueTextureNames, material.PhongExponentTexture, request.DefaultTextureFormat, markAsNormalMap: false);
            }

            if (request.UsedInternalTextures is not null)
            {
                foreach (var internalTexture in request.UsedInternalTextures)
                {
                    var isNormalTexture = string.Equals(internalTexture, "normal", StringComparison.OrdinalIgnoreCase);
                    TryAddInternalTexture(
                        exportJobs,
                        result,
                        internalTexture,
                        isNormalTexture ? request.NormalMapFormat : request.DefaultTextureFormat,
                        isNormalTexture);
                }
            }

            return exportJobs;
        }

        private static void TryAddExternalTexture(
            IDictionary<string, TextureExportJob> exportJobs,
            ServiceResult result,
            IReadOnlyDictionary<string, string> uniqueTextureNames,
            string? sourcePath,
            VtfImageFormat format,
            bool markAsNormalMap)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return;
            }

            if (!uniqueTextureNames.TryGetValue(sourcePath, out var outputName))
            {
                result.Warnings.Add($"Skipped texture '{sourcePath}' because no export name was assigned.");
                return;
            }

            TryAddJob(
                exportJobs,
                result,
                new TextureExportJob
                {
                    OutputName = outputName,
                    SourcePath = sourcePath,
                    Format = format,
                    MarkAsNormalMap = markAsNormalMap
                });
        }

        private static void TryAddInternalTexture(
            IDictionary<string, TextureExportJob> exportJobs,
            ServiceResult result,
            string internalTextureKey,
            VtfImageFormat format,
            bool markAsNormalMap)
        {
            if (string.IsNullOrWhiteSpace(internalTextureKey))
            {
                return;
            }

            if (!InternalTextureResources.ContainsKey(internalTextureKey))
            {
                result.Warnings.Add($"Skipped unknown internal texture '{internalTextureKey}'.");
                return;
            }

            TryAddJob(
                exportJobs,
                result,
                new TextureExportJob
                {
                    OutputName = internalTextureKey,
                    InternalTextureKey = internalTextureKey,
                    Format = format,
                    MarkAsNormalMap = markAsNormalMap
                });
        }

        private static void TryAddJob(
            IDictionary<string, TextureExportJob> exportJobs,
            ServiceResult result,
            TextureExportJob exportJob)
        {
            if (!exportJobs.TryGetValue(exportJob.OutputName, out var existingJob))
            {
                exportJobs[exportJob.OutputName] = exportJob;
                return;
            }

            if (string.Equals(existingJob.SourcePath, exportJob.SourcePath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(existingJob.InternalTextureKey, exportJob.InternalTextureKey, StringComparison.OrdinalIgnoreCase)
                && existingJob.Format == exportJob.Format
                && existingJob.MarkAsNormalMap == exportJob.MarkAsNormalMap)
            {
                return;
            }

            result.Warnings.Add(
                $"Texture export name conflict for '{exportJob.OutputName}'. Keeping the first texture assigned to that output name.");
        }

        private static string StageSourceTexture(TextureExportJob exportJob, string stagingDirectory)
        {
            if (!string.IsNullOrWhiteSpace(exportJob.SourcePath))
            {
                if (!File.Exists(exportJob.SourcePath))
                {
                    throw new FileNotFoundException($"Source image not found: {exportJob.SourcePath}");
                }

                var extension = Path.GetExtension(exportJob.SourcePath);
                if (string.IsNullOrWhiteSpace(extension))
                {
                    throw new InvalidOperationException($"Source image '{exportJob.SourcePath}' does not have a file extension.");
                }

                var stagedPath = Path.Combine(stagingDirectory, $"{exportJob.OutputName}{extension}");
                File.Copy(exportJob.SourcePath, stagedPath, overwrite: true);
                return stagedPath;
            }

            if (!string.IsNullOrWhiteSpace(exportJob.InternalTextureKey))
            {
                if (!InternalTextureResources.TryGetValue(exportJob.InternalTextureKey, out var resourceName))
                {
                    throw new InvalidOperationException($"Unknown internal texture '{exportJob.InternalTextureKey}'.");
                }

                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream(resourceName)
                    ?? throw new InvalidOperationException($"Embedded texture not found: {resourceName}");

                var stagedPath = Path.Combine(stagingDirectory, $"{exportJob.OutputName}.png");
                using var outputStream = File.Create(stagedPath);
                stream.CopyTo(outputStream);
                return stagedPath;
            }

            throw new InvalidOperationException($"Texture export job '{exportJob.OutputName}' does not have a source image.");
        }

        private sealed class TextureExportJob
        {
            public required string OutputName { get; init; }
            public string? SourcePath { get; init; }
            public string? InternalTextureKey { get; init; }
            public required VtfImageFormat Format { get; init; }
            public bool MarkAsNormalMap { get; init; }
        }
    }
}
