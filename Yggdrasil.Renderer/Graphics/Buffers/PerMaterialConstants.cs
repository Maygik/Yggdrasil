using System.Runtime.InteropServices;

namespace Yggdrasil.Renderer.Graphics.Buffers;

[StructLayout(LayoutKind.Sequential)]
internal struct PerMaterialConstants
{
    public float TintR;
    public float TintG;
    public float TintB;
    public float HasBaseTexture;
    public float HasNormalMap;
    public float Padding0;
    public float Padding1;
    public float Padding2;
}
