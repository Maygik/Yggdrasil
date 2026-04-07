using System.Diagnostics;
using Yggdrasil.Application.Abstractions;
using Yggdrasil.Application.UseCases;
using Yggdrasil.Domain.Project;

namespace Yggdrasil.Infrastructure.Images
{
    /// <summary>
    /// Handles writing VTF files to disk by invoking the VTFCmd command-line tool.
    /// This class is responsible for constructing the appropriate command-line arguments based on the VtfWriteRequest, running the process, and handling any errors that may occur during execution.
    /// It ensures that the output file is created successfully and provides detailed error messages if something goes wrong.
    /// </summary>
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

            // Set the input and output paths
            startInfo.ArgumentList.Add("-file");
            startInfo.ArgumentList.Add(request.SourceImagePath);
            startInfo.ArgumentList.Add("-output");
            startInfo.ArgumentList.Add(request.OutputDirectory);

            // Set the max image dimensions and resize mode
            // (Stops 8k textures turning into 300MB monstrosities that don't work)
            startInfo.ArgumentList.Add("-resize");
            startInfo.ArgumentList.Add("-rclampwidth");
            startInfo.ArgumentList.Add("4096");
            startInfo.ArgumentList.Add("-rclampheight");
            startInfo.ArgumentList.Add("4096");
            startInfo.ArgumentList.Add("-rmethod");
            startInfo.ArgumentList.Add("BIGGEST");

            // Set the image format, e.g. rgba, dxt5, etc.
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
