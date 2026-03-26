namespace Yggdrasil.Types;

public struct Color3 : IEquatable<Color3>
{
    public float R { get; set; }

    public float G { get; set; }

    public float B { get; set; }

    public Color3(float r, float g, float b)
    {
        R = r;
        G = g;
        B = b;
    }

    public static Color3 White => new(1f, 1f, 1f);

    public static Color3 Black => new(0f, 0f, 0f);

    public static Color3 FromInt(int red, int green, int blue)
        => new(red / 255f, green / 255f, blue / 255f);

    public bool Equals(Color3 other) => R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B);

    public override bool Equals(object? obj) => obj is Color3 other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(R, G, B);

    public override string ToString() => $"({R}, {G}, {B})";
}

public struct Color4 : IEquatable<Color4>
{
    public float R { get; set; }

    public float G { get; set; }

    public float B { get; set; }

    public float A { get; set; }

    public Color4(float r, float g, float b, float a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public static Color4 Transparent => new(0f, 0f, 0f, 0f);

    public static Color4 White => new(1f, 1f, 1f, 1f);

    public static Color4 FromInt(int red, int green, int blue, int alpha)
        => new(red / 255f, green / 255f, blue / 255f, alpha / 255f);

    public bool Equals(Color4 other)
        => R.Equals(other.R)
        && G.Equals(other.G)
        && B.Equals(other.B)
        && A.Equals(other.A);

    public override bool Equals(object? obj) => obj is Color4 other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(R, G, B, A);

    public override string ToString() => $"({R}, {G}, {B}, {A})";
}
