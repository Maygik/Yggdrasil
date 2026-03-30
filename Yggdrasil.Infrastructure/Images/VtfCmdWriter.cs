using System.Diagnostics;
using Yggdrasil.Application.Abstractions;
using Yggdrasil.Application.UseCases;
using Yggdrasil.Domain.Project;

namespace Yggdrasil.Infrastructure.Images
{
    internal sealed class VtfCmdWriter : IVTFWriter
    {
        public ServiceResult WriteVtf(VtfWriteRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.ToolPath))
            {
                return new ServiceResult(false, "VTFCmd path is required for VTF export.");
            }

            if (string.IsNullOrWhiteSpace(request.SourceImagePath))
            {
                return new ServiceResult(false, "A source image path is required for VTF export.");
            }

            if (!File.Exists(request.SourceImagePath))
            {
                return new ServiceResult(false, $"Source image not found: {request.SourceImagePath}");
            }

            if (string.IsNullOrWhiteSpace(request.OutputDirectory))
            {
                return new ServiceResult(false, "An output directory is required for VTF export.");
            }

            Directory.CreateDirectory(request.OutputDirectory);

            var outputFileName = $"{Path.GetFileNameWithoutExtension(request.SourceImagePath)}.vtf";
            var outputFilePath = Path.Combine(request.OutputDirectory, outputFileName);

            var startInfo = new ProcessStartInfo
            {
                FileName = request.ToolPath,
                WorkingDirectory = request.OutputDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("-silent");
            startInfo.ArgumentList.Add("-file");
            startInfo.ArgumentList.Add(request.SourceImagePath);
            startInfo.ArgumentList.Add("-output");
            startInfo.ArgumentList.Add(request.OutputDirectory);
            startInfo.ArgumentList.Add("-format");
            startInfo.ArgumentList.Add(ToCommandArgument(request.Format));
            startInfo.ArgumentList.Add("-alphaformat");
            startInfo.ArgumentList.Add(ToCommandArgument(request.Format));

            if (request.MarkAsNormalMap)
            {
                startInfo.ArgumentList.Add("-flag");
                startInfo.ArgumentList.Add("NORMAL");
            }

            try
            {
                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var standardOutput = process.StandardOutput.ReadToEnd();
                var standardError = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    return new ServiceResult(false, BuildFailureMessage(outputFileName, process.ExitCode, standardOutput, standardError));
                }

                if (!File.Exists(outputFilePath))
                {
                    return new ServiceResult(false, $"VTFCmd finished without creating '{outputFilePath}'.");
                }

                return new ServiceResult(true);
            }
            catch (Exception ex)
            {
                return new ServiceResult(false, $"Failed to run VTFCmd for '{outputFileName}': {ex.Message}");
            }
        }

        private static string ToCommandArgument(VtfImageFormat format)
        {
            return format switch
            {
                VtfImageFormat.Dxt5 => "DXT5",
                VtfImageFormat.Bgra8888 => "BGRA8888",
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported VTF image format.")
            };
        }

        private static string BuildFailureMessage(string outputFileName, int exitCode, string standardOutput, string standardError)
        {
            var details = string.IsNullOrWhiteSpace(standardError)
                ? standardOutput?.Trim()
                : standardError.Trim();

            return string.IsNullOrWhiteSpace(details)
                ? $"VTFCmd exited with code {exitCode} while writing '{outputFileName}'."
                : $"VTFCmd exited with code {exitCode} while writing '{outputFileName}': {details}";
        }
    }
}
