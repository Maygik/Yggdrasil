using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Yggdrasil.Renderer.Runtime;

internal static class RendererDiagnostics
{
    private static readonly object Sync = new();

    public static string LogPath { get; } = Path.Combine(
        Path.GetTempPath(),
        "Yggdrasil",
        "renderer-diagnostics.log");

    // Reset the diagnostics log. This is called at the start of each rendering session to ensure we have a clean log for each session.
    public static void Reset()
    {
        lock (Sync)
        {
            var directory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(
                LogPath,
                $"Renderer diagnostics session started {DateTimeOffset.Now:O}{Environment.NewLine}",
                Encoding.UTF8);

            Debug.WriteLine($"Renderer diagnostics reset: {LogPath}");
        }
    }

    // Writes a message to the diagnostics log with a timestamp. This is thread-safe and can be called from any part of the renderer to log important information or errors.
    public static void Write(string message)
    {
        lock (Sync)
        {
            var directory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var line = $"[{DateTimeOffset.Now:O}] {message}";
            File.AppendAllText(
                LogPath,
                $"{line}{Environment.NewLine}",
                Encoding.UTF8);
            Debug.WriteLine(line);
        }
    }

    public static void WriteException(string stage, Exception exception)
    {
        Write($"{stage}: {exception}");
    }
}
