using System.Runtime.InteropServices;
using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Graphics.Buffers;

[StructLayout(LayoutKind.Sequential)]
internal struct PackedMatrix4x4
{
    public float M00;
    public float M01;
    public float M02;
    public float M03;
    public float M10;
    public float M11;
    public float M12;
    public float M13;
    public float M20;
    public float M21;
    public float M22;
    public float M23;
    public float M30;
    public float M31;
    public float M32;
    public float M33;

    public static PackedMatrix4x4 FromMatrix(Matrix4x4 matrix)
    {
        return new PackedMatrix4x4
        {
            M00 = matrix.M00,
            M01 = matrix.M01,
            M02 = matrix.M02,
            M03 = matrix.M03,
            M10 = matrix.M10,
            M11 = matrix.M11,
            M12 = matrix.M12,
            M13 = matrix.M13,
            M20 = matrix.M20,
            M21 = matrix.M21,
            M22 = matrix.M22,
            M23 = matrix.M23,
            M30 = matrix.M30,
            M31 = matrix.M31,
            M32 = matrix.M32,
            M33 = matrix.M33
        };
    }
}
