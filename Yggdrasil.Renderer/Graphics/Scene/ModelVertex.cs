using System.Runtime.InteropServices;

namespace Yggdrasil.Renderer.Graphics.Scene;

[StructLayout(LayoutKind.Sequential)]
internal struct ModelVertex
{
    public float PositionX;
    public float PositionY;
    public float PositionZ;
    public float NormalX;
    public float NormalY;
    public float NormalZ;
    public float TangentX;
    public float TangentY;
    public float TangentZ;
    public float BitangentX;
    public float BitangentY;
    public float BitangentZ;
    public float TexCoordX;
    public float TexCoordY;
    public int BoneIndex0;
    public int BoneIndex1;
    public int BoneIndex2;
    public int BoneIndex3;
    public float BoneWeight0;
    public float BoneWeight1;
    public float BoneWeight2;
    public float BoneWeight3;
}
