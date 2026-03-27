using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Scene;

internal static class ViewportAlignmentMath
{
    public static Matrix4x4 CreateModelTransform(ViewportRenderOptions viewportOptions)
    {
        ArgumentNullException.ThrowIfNull(viewportOptions);

        return SceneAlignmentMath.CreateAlignmentMatrix(
            viewportOptions.ModelScale,
            viewportOptions.ModelRotationDegrees,
            viewportOptions.ModelPosition).Transpose();
    }

    public static Matrix4x4 CreateHeightPlaneTransform(ViewportRenderOptions viewportOptions)
    {
        ArgumentNullException.ThrowIfNull(viewportOptions);

        return SceneAlignmentMath.CreateTranslationMatrix(
            new Vector3(0.0f, 0.0f, viewportOptions.HeightPlaneHeight)).Transpose();
    }
}
