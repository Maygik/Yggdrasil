using System.Runtime.InteropServices;

namespace Yggdrasil.Renderer.Graphics.Scene;

[StructLayout(LayoutKind.Sequential)]
internal struct LineVertex
{
    public float PositionX;
    public float PositionY;
    public float PositionZ;
    public float ColorR;
    public float ColorG;
    public float ColorB;
    public float ColorA;
}
