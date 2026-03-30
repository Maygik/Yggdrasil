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
        public VtfImageFormat DefaultTextureFormat { get; set; } = VtfImageFormat.Dxt5;
        public VtfImageFormat NormalMapFormat { get; set; } = VtfImageFormat.Bgra8888;

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
