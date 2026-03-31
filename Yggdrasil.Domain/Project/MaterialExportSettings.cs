using System.Text;

namespace Yggdrasil.Domain.Project
{
    public enum VtfImageFormat
    {
        Dxt5,
        Bgra8888
    }

    public sealed class MaterialExportSettings
    {
        public VtfImageFormat DefaultTextureFormat { get; set; } = VtfImageFormat.Dxt5; // DXT5 works for almost everything. Technically less optimised for some than DXT1, but DXT1 has some compatability issues.
        public VtfImageFormat NormalMapFormat { get; set; } = VtfImageFormat.Bgra8888; // BGRA8888 is better for normal maps, DXT5 can cause artifacts. Stores uncompressed.

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Material Export:");
            sb.AppendLine($"\tDefault Texture Format: {DefaultTextureFormat}");
            sb.AppendLine($"\tNormal Map Format: {NormalMapFormat}");
            return sb.ToString();
        }
    }
}
