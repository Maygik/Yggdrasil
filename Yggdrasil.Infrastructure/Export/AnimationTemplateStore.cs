using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Infrastructure.QC;

namespace Yggdrasil.Infrastructure.Export
{
    public class AnimationTemplateStore
    {

        // Mapping of template keys to file names
        private readonly Dictionary<string, string> _keyToFile = new(StringComparer.OrdinalIgnoreCase)
        {
            ["reference_male"] = "reference_male.smd",
            ["reference_female"] = "reference_female.smd",
            ["proportions"] = "proportions.smd",
        };

        // Cache for loaded templates
        private readonly Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
        private string _root = "";

        // Initializes the template store, setting up the root directory and installing defaults if needed.
        public void  Init(string appName = "Yggdrasil")
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _root = Path.Combine(appData, appName, "Templates", "Animations");

            Directory.CreateDirectory(_root);
            InstallMissingDefaults(); // copies embedded -> disk if file missing
        }

        // Retrieves the template content for the given key, loading it from disk if not cached.
        public string Get(string key)
        {
            // Check cache first
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            // Load from disk if not cached

            // Check key validity
            if (!_keyToFile.TryGetValue(key, out var fileName))
                throw new KeyNotFoundException($"Unknown template key '{key}'.");

            // Check file existence
            var path = Path.Combine(_root, fileName);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Missing QC template: {path}");

            // Read and normalize content
            var text = Normalize(File.ReadAllText(path, Encoding.UTF8));
            _cache[key] = text;
            return text;
        }

        // Clears the cache, forcing templates to be reloaded from disk on next access.
        public void Reload() => _cache.Clear();

        // Resets all templates to their default embedded versions, overwriting any existing files.
        public void ResetToDefaults()
        {
            Directory.CreateDirectory(_root);
            foreach (var kvp in _keyToFile)
                WriteDefault(kvp.Value, overwrite: true);

            Reload();
        }

        // Installs missing default templates by copying embedded resources to disk if they don't already exist.
        private void InstallMissingDefaults()
        {
            foreach (var kvp in _keyToFile)
            {
                var fileName = kvp.Value;
                var path = Path.Combine(_root, fileName);
                if (!File.Exists(path))
                    WriteDefault(fileName, overwrite: false);
            }
        }

        // Writes the default embedded template to disk, optionally overwriting existing files.
        private void WriteDefault(string fileName, bool overwrite = false)
        {
            var path = Path.Combine(_root, fileName);
            if (!overwrite && File.Exists(path))
                return;

            var asm = typeof(QcTemplateStore).Assembly;
            var resName = $"Yggdrasil.Infrastructure.Resources.Armatures.{fileName}"; // adjust

            var dumpPath = Path.Combine(Path.GetTempPath(), "ygg_resources.txt");
            File.WriteAllLines(dumpPath, asm.GetManifestResourceNames());

            using var stream = asm.GetManifestResourceStream(resName)
                ?? throw new InvalidOperationException($"Embedded animation not found: {resName}");

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = Normalize(reader.ReadToEnd());

            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        // Normalizes the content by stripping BOM and standardizing line endings to LF.
        private static string Normalize(string content)
        {
            // Strip BOM if it snuck in
            if (content.Length > 0 && content[0] == '\uFEFF')
                content = content[1..];

            // Remove other BOM variants if present
            content = content.Replace("\uFFFE", "").Replace("\uFEFF", "");
            // Standardize line endings to LF
            content = content.Replace("\r\n", "\n").Replace("\r", "\n");

            // Final normalization to ensure consistent line endings
            return content.Replace("\r\n", "\n");
        }
    }
}


