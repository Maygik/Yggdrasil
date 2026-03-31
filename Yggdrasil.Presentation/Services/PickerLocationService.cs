using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Yggdrasil.Presentation.Services
{
    /// <summary>
    /// Used to make pickers (folder and file) remember the last picked location for different contexts (e.g. create project, open project, export qc, etc.)
    /// This was really annoying
    /// </summary>
    internal sealed class PickerLocationService
    {
        private readonly string _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Yggdrasil",
            "pickerlocations.json");

        public string? GetDirectory(string key)
        {
            var entries = Load();
            return entries.TryGetValue(key, out var directory) ? directory : null;
        }

        public void SaveDirectory(string key, string? directory)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            var entries = Load();
            entries[key] = directory;
            Save(entries);
        }

        // Load the entries from a file
        private Dictionary<string, string> Load()
        {
            if (!File.Exists(_filePath))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var line in File.ReadAllLines(_filePath))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var separatorIndex = line.IndexOf('\t');
                    if (separatorIndex <= 0 || separatorIndex >= line.Length - 1)
                    {
                        continue;
                    }

                    var encodedKey = line[..separatorIndex];
                    var encodedDirectory = line[(separatorIndex + 1)..];

                    var key = Encoding.UTF8.GetString(Convert.FromBase64String(encodedKey));
                    var directory = Encoding.UTF8.GetString(Convert.FromBase64String(encodedDirectory));

                    if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(directory))
                    {
                        entries[key] = directory;
                    }
                }

                return entries;
            }
            catch
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        // Save the entries to a file
        // We encode the keys and values in Base64 to safely handle any special characters and ensure the file format is simple (key and value separated by a tab).
        private void Save(Dictionary<string, string> entries)
        {
            var folderPath = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            using var writer = new StreamWriter(_filePath, false, Encoding.UTF8);
            foreach (var entry in entries)
            {
                var encodedKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(entry.Key));
                var encodedDirectory = Convert.ToBase64String(Encoding.UTF8.GetBytes(entry.Value));
                writer.WriteLine($"{encodedKey}\t{encodedDirectory}");
            }
        }
    }
}
