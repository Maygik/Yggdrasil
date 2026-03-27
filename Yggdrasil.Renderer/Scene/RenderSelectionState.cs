namespace Yggdrasil.Renderer.Scene;

public sealed class RenderSelectionState
{
    public static RenderSelectionState Empty { get; } = new();

    public string? SelectedMaterialName { get; init; }

    public string? HoveredMaterialName { get; init; }
}
