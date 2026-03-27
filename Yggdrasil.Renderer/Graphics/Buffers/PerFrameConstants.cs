using System.Runtime.InteropServices;

namespace Yggdrasil.Renderer.Graphics.Buffers;

[StructLayout(LayoutKind.Sequential)]
internal struct PerFrameConstants
{
    public PackedMatrix4x4 ViewProjection;
    public float CameraPositionX;
    public float CameraPositionY;
    public float CameraPositionZ;
    public float Padding0;
    public float LightDirectionX;
    public float LightDirectionY;
    public float LightDirectionZ;
    public float AmbientStrength;
}
