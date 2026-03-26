using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Presentation.Models;

namespace Yggdrasil.Presentation.Services
{
    public sealed class RecentProjectsService
    {
        private const int MaxEntries = 12;

        private static List<RecentProjectEntry> GetRecentProjectsFromDisk()
        {
            // Load the recent projects list from persistent storage
            // Use JSON because easy
            var folderPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Yggdrasil");
            var filePath = System.IO.Path.Combine(folderPath, "recentprojects.json");

            // If the file doesn't exist, return an empty list
            if (!System.IO.File.Exists(filePath))
                return new List<RecentProjectEntry>();

            try
            {
                // Read the JSON from the file and deserialize it into a list of RecentProjectEntry
                string json = System.IO.File.ReadAllText(filePath);
                var entries = System.Text.Json.JsonSerializer.Deserialize<List<RecentProjectEntry>>(json);
                return entries ?? new List<RecentProjectEntry>();
            }
            catch
            {
                // If there's an error reading or deserializing, return an empty list
                return new List<RecentProjectEntry>();
            }
        }

        public async Task<IReadOnlyList<RecentProjectEntry>> LoadAsync()
        {
            // Load the recent projects list from persistent storage
            var entries = GetRecentProjectsFromDisk();
            return await Task.FromResult((IReadOnlyList<RecentProjectEntry>)(entries ?? new List<RecentProjectEntry>()));
        }

        public async Task SaveAsync(IReadOnlyList<RecentProjectEntry> entries)
        {
            // Ensure we only save up to the maximum number of entries
            var entriesToSave = entries.Take(MaxEntries).ToList();

            // Save the entriesToSave list to persistent storage
            // Use JSON because easy

            // Serialize entriesToSave to JSON and save to file or user settings
            string? json = System.Text.Json.JsonSerializer.Serialize(entriesToSave);

            // Save to local app data
            var folderPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Yggdrasil");
            System.IO.Directory.CreateDirectory(folderPath);
            
            var filePath = System.IO.Path.Combine(folderPath, "recentprojects.json");
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        public async Task<IReadOnlyList<RecentProjectEntry>> AddOrUpdateAsync(RecentProjectEntry entry)
        {
            // Load the current list
            var entries = GetRecentProjectsFromDisk() ?? new List<RecentProjectEntry>();

            // Remove any existing entry with the same file path
            entries = entries.Where(e => !string.Equals(e.FilePath, entry.FilePath, StringComparison.OrdinalIgnoreCase)).ToList();

            // Add the new entry to the top of the list
            entries.Insert(0, entry);

            // Save the updated list back to disk
            await SaveAsync(entries);

            return await Task.FromResult((IReadOnlyList<RecentProjectEntry>)entries);
        }

        public async Task<IReadOnlyList<RecentProjectEntry>> RemoveMissingAsync()
        {
            // Load the current list
            var entries = GetRecentProjectsFromDisk() ?? new List<RecentProjectEntry>();

            // Remove any entries where the file path does not exist
            entries = entries.Where(e => System.IO.File.Exists(e.FilePath)).ToList();

            // Save the updated list back to disk
            await SaveAsync(entries);

            return await Task.FromResult((IReadOnlyList<RecentProjectEntry>)entries);
        }
    }
}
