namespace Yggdrasil.Renderer.Graphics.Shaders;

internal readonly record struct MaterialShaderKey(
    string ShaderName,
    MaterialRenderMode RenderMode,
    VertexLitGenericFeatures Features);
