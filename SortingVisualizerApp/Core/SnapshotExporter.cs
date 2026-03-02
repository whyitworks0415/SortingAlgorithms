using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SortingVisualizerApp.Core;

public static class SnapshotExporter
{
    public static void SavePng(string path, int width, int height, byte[] rgbaPixelsBottomLeft)
    {
        if (width <= 0 || height <= 0 || rgbaPixelsBottomLeft.Length < width * height * 4)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

        using var image = new Image<Rgba32>(width, height);
        for (var y = 0; y < height; y++)
        {
            var srcY = height - 1 - y;
            var srcRow = srcY * width * 4;
            for (var x = 0; x < width; x++)
            {
                var idx = srcRow + x * 4;
                image[x, y] = new Rgba32(
                    rgbaPixelsBottomLeft[idx + 0],
                    rgbaPixelsBottomLeft[idx + 1],
                    rgbaPixelsBottomLeft[idx + 2],
                    rgbaPixelsBottomLeft[idx + 3]);
            }
        }

        image.SaveAsPng(path);
    }
}
