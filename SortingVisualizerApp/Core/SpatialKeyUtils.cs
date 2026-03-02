namespace SortingVisualizerApp.Core;

public static class SpatialKeyUtils
{
    public static uint MortonKey16(float x, float y)
    {
        var xi = (uint)Math.Clamp((int)MathF.Round(x * 65535.0f), 0, 65535);
        var yi = (uint)Math.Clamp((int)MathF.Round(y * 65535.0f), 0, 65535);
        return Interleave16(xi, yi);
    }

    public static uint ZOrderKey16(float x, float y)
    {
        // Z-order and Morton are equivalent here; kept as distinct API for clarity and metadata separation.
        return MortonKey16(x, y);
    }

    public static uint HilbertKey16(float x, float y)
    {
        var xi = (uint)Math.Clamp((int)MathF.Round(x * 65535.0f), 0, 65535);
        var yi = (uint)Math.Clamp((int)MathF.Round(y * 65535.0f), 0, 65535);
        return HilbertIndex16(xi, yi);
    }

    private static uint Interleave16(uint x, uint y)
    {
        x = (x | (x << 8)) & 0x00FF00FFu;
        x = (x | (x << 4)) & 0x0F0F0F0Fu;
        x = (x | (x << 2)) & 0x33333333u;
        x = (x | (x << 1)) & 0x55555555u;

        y = (y | (y << 8)) & 0x00FF00FFu;
        y = (y | (y << 4)) & 0x0F0F0F0Fu;
        y = (y | (y << 2)) & 0x33333333u;
        y = (y | (y << 1)) & 0x55555555u;

        return x | (y << 1);
    }

    private static uint HilbertIndex16(uint x, uint y)
    {
        uint d = 0;
        for (uint s = 1u << 15; s > 0; s >>= 1)
        {
            var rx = (x & s) > 0 ? 1u : 0u;
            var ry = (y & s) > 0 ? 1u : 0u;
            d += s * s * ((3u * rx) ^ ry);
            Rotate(s, ref x, ref y, rx, ry);
        }

        return d;
    }

    private static void Rotate(uint n, ref uint x, ref uint y, uint rx, uint ry)
    {
        if (ry != 0)
        {
            return;
        }

        if (rx == 1)
        {
            x = (n - 1) - x;
            y = (n - 1) - y;
        }

        (x, y) = (y, x);
    }
}
