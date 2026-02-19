using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Domain.Scene;

namespace Yggdrassil.Infrastructure.Export
{
    public class MaterialExporter
    {
        public MaterialExporter() { }

        public void ExportMaterial(SourceMaterialSettings materialSettings, string outputPath)
        {
            var sb = new StringBuilder();
            // Start with the shader
            sb.AppendLine($"\"{materialSettings.Shader}\"");

        }
    }
}
