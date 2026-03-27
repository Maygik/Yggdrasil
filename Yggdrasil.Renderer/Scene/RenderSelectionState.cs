namespace Yggdrasil.Renderer.Scene;

public sealed class RenderSelectionState
{
    public static RenderSelectionState Empty { get; } = new();

    public string? HoveredMaterialName { get; init; }

    public string? SelectedMaterialName { get; init; }
}
