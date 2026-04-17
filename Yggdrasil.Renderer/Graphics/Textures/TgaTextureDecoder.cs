using System;
using System.IO;

namespace Yggdrasil.Renderer.Graphics.Textures;

internal static class TgaTextureDecoder
{
    // Loads a TGA image from the specified absolute file path and returns its pixel data in RGBA format.
    public static TexturePixelData Load(string absolutePath)
    {
        using var stream = File.OpenRead(absolutePath);
        using var reader = new BinaryReader(stream);

        var header = TgaHeader.Read(reader);
        ValidateHeader(header);

        if (header.IdLength > 0)
        {
            stream.Seek(header.IdLength, SeekOrigin.Current);
        }

        var colorMap = header.ColorMapType == 1
            ? ReadColorMap(reader, header)
            : null;
        var pixelBytes = DecodePixels(reader, header, colorMap);

        return new TexturePixelData(header.Width, header.Height, pixelBytes);
    }

    // Performs validation checks on the TGA header to ensure it represents a supported and well-formed image.
    private static void ValidateHeader(TgaHeader header)
    {
        if (header.Width == 0 || header.Height == 0)
        {
            throw new InvalidDataException("TGA image has invalid dimensions.");
        }

        if (header.ColorMapType is not 0 and not 1)
        {
            throw new NotSupportedException($"Unsupported TGA color map type '{header.ColorMapType}'.");
        }

        if (header.ImageType is not 1 and not 2 and not 3 and not 9 and not 10 and not 11)
        {
            throw new NotSupportedException($"Unsupported TGA image type '{header.ImageType}'.");
        }

        if (header.IsColorMapped && header.ColorMapType == 0)
        {
            throw new InvalidDataException("TGA image uses a color map but does not include one.");
        }

        if (header.ColorMapType == 1 && header.ColorMapLength == 0)
        {
            throw new InvalidDataException("TGA image declares an empty color map.");
        }

        if (header.IsTrueColor && header.PixelDepth is not 15 and not 16 and not 24 and not 32)
        {
            throw new NotSupportedException($"Unsupported TGA true-color depth '{header.PixelDepth}'.");
        }

        if (header.IsGrayscale && header.PixelDepth is not 8 and not 16)
        {
            throw new NotSupportedException($"Unsupported TGA grayscale depth '{header.PixelDepth}'.");
        }

        if (header.IsColorMapped)
        {
            if (header.PixelDepth is not 8 and not 16)
            {
                throw new NotSupportedException($"Unsupported TGA color-map index depth '{header.PixelDepth}'.");
            }

            if (header.ColorMapEntrySize is not 15 and not 16 and not 24 and not 32)
            {
                throw new NotSupportedException($"Unsupported TGA palette entry depth '{header.ColorMapEntrySize}'.");
            }
        }
    }

    // Reads the color map entries from the TGA file and returns them as an array of TgaColor structures.
    // Each color map entry is read according to the specified entry size and may include an alpha bit if applicable.
    private static TgaColor[] ReadColorMap(BinaryReader reader, TgaHeader header)
    {
        var colorMap = new TgaColor[header.ColorMapLength];

        for (var i = 0; i < colorMap.Length; i++)
        {
            colorMap[i] = ReadDirectColor(reader, header.ColorMapEntrySize, useAlphaBit: header.ColorMapEntrySize == 16);
        }

        return colorMap;
    }

    // Decodes the pixel data from the TGA file, handling both uncompressed and RLE-compressed formats.
    private static byte[] DecodePixels(BinaryReader reader, TgaHeader header, TgaColor[]? colorMap)
    {
        var totalPixels = checked((int)header.Width * (int)header.Height);
        var pixelBytes = new byte[checked(totalPixels * 4)];
        var pixelIndex = 0;

        // For each pixel in the image
        while (pixelIndex < totalPixels)
        {
            // If the image is not run-length encoded, read a single pixel and write it to the output array.
            // This means that each pixel value is stored directly in the file without compression
            if (!header.IsRunLengthEncoded)
            {
                var color = ReadPixel(reader, header, colorMap);
                WritePixel(pixelBytes, header, pixelIndex, color);
                pixelIndex++;
                continue;
            }

            // If the image is run-length encoded, read a packet header to determine the packet type and length.
            var packetHeader = reader.ReadByte();
            var packetLength = (packetHeader & 0x7F) + 1;
            if (pixelIndex + packetLength > totalPixels)
            {
                throw new InvalidDataException("TGA RLE packet exceeds image bounds.");
            }

            // If the high bit of the packet header is set, this is a run-length packet where a single pixel value is repeated for the length of the packet.
            // This means that the next pixel value read from the file should be duplicated for the number of pixels specified by the packet length.
            // E.g. pixels are R R R R R R R R B B B so we repeat R for the first 8 pixels in one pass, and then write B for the last three pixels in the next pass.
            if ((packetHeader & 0x80) != 0)
            {
                var color = ReadPixel(reader, header, colorMap);
                for (var i = 0; i < packetLength; i++)
                {
                    WritePixel(pixelBytes, header, pixelIndex, color);
                    pixelIndex++;
                }

                continue;
            }

            // If the high bit of the packet header is not set, this is a raw packet where each pixel value is read directly from the file for the length of the packet.
            // This means that the next set of pixel values read from the file should be written directly to the output array for the number of pixels specified by the packet length.
            // E.g. pixels are R G B R G B R G B so we read each pixel value directly for all 9 pixels in one pass.
            for (var i = 0; i < packetLength; i++)
            {
                var color = ReadPixel(reader, header, colorMap);
                WritePixel(pixelBytes, header, pixelIndex, color);
                pixelIndex++;
            }
        }

        return pixelBytes;
    }

    // Reads a single pixel from the TGA file according to the image type and color map information.
    private static TgaColor ReadPixel(BinaryReader reader, TgaHeader header, TgaColor[]? colorMap)
    {
        if (header.IsTrueColor)
        {
            return ReadDirectColor(reader, header.PixelDepth, useAlphaBit: header.PixelDepth == 16 && header.AlphaBits > 0);
        }

        if (header.IsGrayscale)
        {
            return ReadGrayscaleColor(reader, header.PixelDepth);
        }

        if (colorMap is null)
        {
            throw new InvalidDataException("TGA palette data is missing.");
        }

        var colorMapIndex = header.PixelDepth switch
        {
            8 => reader.ReadByte(),
            16 => reader.ReadUInt16(),
            _ => throw new NotSupportedException($"Unsupported TGA color-map index depth '{header.PixelDepth}'.")
        };
        var normalizedIndex = colorMapIndex - header.ColorMapFirstEntryIndex;
        if (normalizedIndex < 0 || normalizedIndex >= colorMap.Length)
        {
            throw new InvalidDataException("TGA palette index is out of bounds.");
        }

        return colorMap[normalizedIndex];
    }

    // Reads a direct color pixel from the TGA file based on the specified pixel depth and whether to use an alpha bit for 16-bit formats.
    private static TgaColor ReadDirectColor(BinaryReader reader, byte pixelDepth, bool useAlphaBit)
    {
        return pixelDepth switch
        {
            15 => ReadPackedColor(reader.ReadUInt16(), useAlphaBit: false),
            16 => ReadPackedColor(reader.ReadUInt16(), useAlphaBit),
            24 => ReadRgb24Color(reader),
            32 => ReadRgba32Color(reader),
            _ => throw new NotSupportedException($"Unsupported TGA true-color depth '{pixelDepth}'.")
        };
    }

    // Reads a grayscale pixel from the TGA file based on the specified pixel depth, creating a TgaColor with equal RGB components and an alpha value.
    private static TgaColor ReadGrayscaleColor(BinaryReader reader, byte pixelDepth)
    {
        return pixelDepth switch
        {
            8 => CreateGrayscale(reader.ReadByte(), 255),
            16 => CreateGrayscale(reader.ReadByte(), reader.ReadByte()),
            _ => throw new NotSupportedException($"Unsupported TGA grayscale depth '{pixelDepth}'.")
        };
    }

    // Reads a packed color value from a 15 or 16-bit TGA pixel, extracting the red, green, blue, and optional alpha components based on the specified format.
    private static TgaColor ReadPackedColor(ushort packedValue, bool useAlphaBit)
    {
        var blue = Expand5To8(packedValue & 0x1F);
        var green = Expand5To8((packedValue >> 5) & 0x1F);
        var red = Expand5To8((packedValue >> 10) & 0x1F);
        var alpha = useAlphaBit
            ? (byte)((packedValue & 0x8000) != 0 ? 255 : 0)
            : (byte)255;

        return new TgaColor(red, green, blue, alpha);
    }

    // Reads a 24-bit RGB color from the TGA file,
    // where each color component is stored as a separate byte in the order of blue, green, and red,
    // and returns a TgaColor with an alpha value of 255.
    private static TgaColor ReadRgb24Color(BinaryReader reader)
    {
        var blue = reader.ReadByte();
        var green = reader.ReadByte();
        var red = reader.ReadByte();
        return new TgaColor(red, green, blue, 255);
    }

    // Reads a 32-bit RGBA color from the TGA file,
    // where each color component is stored as a separate byte in the order of blue, green, red, and alpha,
    // and returns a TgaColor with the corresponding RGBA values.
    private static TgaColor ReadRgba32Color(BinaryReader reader)
    {
        var blue = reader.ReadByte();
        var green = reader.ReadByte();
        var red = reader.ReadByte();
        var alpha = reader.ReadByte();
        return new TgaColor(red, green, blue, alpha);
    }

    private static TgaColor CreateGrayscale(byte value, byte alpha) => new(value, value, value, alpha);

    private static byte Expand5To8(int component) => (byte)((component << 3) | (component >> 2));

    private static void WritePixel(byte[] pixelBytes, TgaHeader header, int pixelIndex, TgaColor color)
    {
        var width = (int)header.Width;
        var height = (int)header.Height;
        var sourceX = pixelIndex % width;
        var sourceY = pixelIndex / width;
        var destinationX = header.IsRightOrigin ? width - 1 - sourceX : sourceX;
        var destinationY = header.IsTopOrigin ? sourceY : height - 1 - sourceY;
        var destinationOffset = checked(((destinationY * width) + destinationX) * 4);

        pixelBytes[destinationOffset] = color.R;
        pixelBytes[destinationOffset + 1] = color.G;
        pixelBytes[destinationOffset + 2] = color.B;
        pixelBytes[destinationOffset + 3] = color.A;
    }

    private readonly record struct TgaHeader(
        byte IdLength,
        byte ColorMapType,
        byte ImageType,
        ushort ColorMapFirstEntryIndex,
        ushort ColorMapLength,
        byte ColorMapEntrySize,
        ushort Width,
        ushort Height,
        byte PixelDepth,
        byte ImageDescriptor)
    {
        public int AlphaBits => ImageDescriptor & 0x0F;

        public bool IsColorMapped => ImageType is 1 or 9;

        public bool IsGrayscale => ImageType is 3 or 11;

        public bool IsRunLengthEncoded => ImageType is 9 or 10 or 11;

        public bool IsRightOrigin => (ImageDescriptor & 0x10) != 0;

        public bool IsTopOrigin => (ImageDescriptor & 0x20) != 0;

        public bool IsTrueColor => ImageType is 2 or 10;

        public static TgaHeader Read(BinaryReader reader)
        {
            var idLength = reader.ReadByte();
            var colorMapType = reader.ReadByte();
            var imageType = reader.ReadByte();
            var colorMapFirstEntryIndex = reader.ReadUInt16();
            var colorMapLength = reader.ReadUInt16();
            var colorMapEntrySize = reader.ReadByte();
            _ = reader.ReadUInt16();
            _ = reader.ReadUInt16();
            var width = reader.ReadUInt16();
            var height = reader.ReadUInt16();
            var pixelDepth = reader.ReadByte();
            var imageDescriptor = reader.ReadByte();

            return new TgaHeader(
                idLength,
                colorMapType,
                imageType,
                colorMapFirstEntryIndex,
                colorMapLength,
                colorMapEntrySize,
                width,
                height,
                pixelDepth,
                imageDescriptor);
        }
    }

    private readonly record struct TgaColor(byte R, byte G, byte B, byte A);
}
