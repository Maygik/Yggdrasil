using System.Runtime.InteropServices;

namespace Yggdrasil.Renderer.Graphics.Buffers;

[StructLayout(LayoutKind.Sequential)]
internal struct SkinningConstants
{
    public PackedMatrix4x4 Bone0;

    public static SkinningConstants CreatePlaceholder()
    {
        throw new System.NotImplementedException();
    }
}
