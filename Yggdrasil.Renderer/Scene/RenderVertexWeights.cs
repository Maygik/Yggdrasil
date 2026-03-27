namespace Yggdrasil.Renderer.Scene;

public readonly record struct RenderVertexWeights(
    int BoneIndex0,
    int BoneIndex1,
    int BoneIndex2,
    int BoneIndex3,
    float Weight0,
    float Weight1,
    float Weight2,
    float Weight3);
