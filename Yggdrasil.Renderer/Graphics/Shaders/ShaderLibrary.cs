namespace Yggdrasil.Renderer.Graphics.Shaders;

internal sealed class ShaderLibrary
{
    private readonly PreviewShaderFamily _previewShaderFamily = new();

    public PreviewShaderFamily GetPreviewShaderFamily()
    {
        return _previewShaderFamily;
    }
}
