using System;

namespace Yggdrasil.Renderer.Graphics.Shaders;

[Flags]
internal enum VertexLitGenericFeatures
{
    None = 0,
    BaseTexture = 1 << 0,
    NormalMap = 1 << 1,
    Phong = 1 << 2,
    RimLight = 1 << 3,
    SelfIllum = 1 << 4,
    EnvMap = 1 << 5,
    LightWarp = 1 << 6,
    DoubleSided = 1 << 7
}
