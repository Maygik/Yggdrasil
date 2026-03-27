using System.Runtime.InteropServices;

namespace Yggdrasil.Renderer.Graphics.Buffers;

[StructLayout(LayoutKind.Sequential)]
internal struct PerObjectConstants
{
    public PackedMatrix4x4 World;
    public float HighlightMix;
    public float IsHovered;
    public float Padding1;
    public float Padding2;
}
