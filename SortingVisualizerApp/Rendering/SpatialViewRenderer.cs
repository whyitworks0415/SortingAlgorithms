using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SortingVisualizerApp.Core;
using NumericsVector2 = System.Numerics.Vector2;

namespace SortingVisualizerApp.Rendering;

public sealed class SpatialViewRenderer : IViewRenderer
{
    [StructLayout(LayoutKind.Sequential)]
    private struct PointVertex
    {
        public float X;
        public float Y;
        public float Highlight;
        public float Heat;
    }

    private readonly ShaderProgram _shader;
    private readonly int _vao;
    private readonly int _vbo;
    private PointVertex[] _vertices = new PointVertex[2048];
    private int[] _lodSourceIndices = new int[2048];

    private readonly SpatialLodWorker _lodWorker = new();
    private long _lastSubmitTick;
    private long _lastSignature;

    private int _lastLodBins;
    private bool _usedAsyncLod;

    public int LastLodBins => _lastLodBins;
    public bool UsedAsyncLod => _usedAsyncLod;
    public int LodQueueDepth => _lodWorker.QueueDepth;
    public VisualizationMode Mode => VisualizationMode.Spatial;

    public SpatialViewRenderer()
    {
        _shader = new ShaderProgram(VertexSource, FragmentSource);
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * Unsafe.SizeOf<PointVertex>(), IntPtr.Zero, BufferUsageHint.DynamicDraw);

        var stride = Unsafe.SizeOf<PointVertex>();
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.BindVertexArray(0);
    }

    public void Draw(SimulationFrameState state)
    {
        if (!state.VisualEnabled)
        {
            return;
        }

        var points = state.Spatial.Points;
        if (points.Length == 0)
        {
            return;
        }

        var drawCount = BuildVertices(
            points,
            state.Spatial.HighlightedIndices,
            state.Spatial.MemoryAccess,
            state.Spatial.MemoryAccessMax,
            state.Overlay.ShowMemoryHeatmap,
            state.Overlay.NormalizeHeatmapByMax,
            state.ViewportWidth);
        if (drawCount <= 0)
        {
            return;
        }

        EnsureCapacity(drawCount);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        UploadBuffer(_vertices, drawCount, BufferTarget.ArrayBuffer);
        CheckGlError("spatial-upload");

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.DepthTest);

        _shader.Use();
        _shader.SetMatrix4("uProjection", Matrix4.Identity);
        _shader.SetInt("uShowHeatmap", state.Overlay.ShowMemoryHeatmap ? 1 : 0);
        GL.BindVertexArray(_vao);
        GL.PointSize(drawCount > 100000 ? 1.0f : 2.0f);
        GL.DrawArrays(PrimitiveType.Points, 0, drawCount);
        GL.BindVertexArray(0);
        CheckGlError("spatial-draw");

        DrawOverlay(state);
    }

    private void DrawOverlay(SimulationFrameState state)
    {
        var drawList = ImGui.GetBackgroundDrawList();
        var left = 24.0f;
        var top = 52.0f;
        var right = Math.Max(left + 220.0f, state.ViewportWidth - 24.0f);
        var bottom = Math.Max(top + 220.0f, state.ViewportHeight - 24.0f);
        drawList.AddRect(new NumericsVector2(left, top), new NumericsVector2(right, bottom), PackColor(200, 200, 200, 120), 0.0f, ImDrawFlags.None, 1.0f);

        if (state.Spatial.ShowGrid)
        {
            for (var i = 1; i < 8; i++)
            {
                var x = left + (right - left) * (i / 8.0f);
                var y = top + (bottom - top) * (i / 8.0f);
                drawList.AddLine(new NumericsVector2(x, top), new NumericsVector2(x, bottom), PackColor(130, 130, 130, 50), 1.0f);
                drawList.AddLine(new NumericsVector2(left, y), new NumericsVector2(right, y), PackColor(130, 130, 130, 50), 1.0f);
            }
        }

        if (state.Spatial.ShowOrder)
        {
            var points = state.Spatial.Points;
            var maxLines = Math.Min(points.Length - 1, 3000);
            for (var i = 0; i < maxLines; i++)
            {
                var p0 = ToScreen(points[i], left, top, right, bottom);
                var p1 = ToScreen(points[i + 1], left, top, right, bottom);
                drawList.AddLine(p0, p1, PackColor(42, 173, 255, 55), 1.0f);
            }
        }

        if (state.Spatial.RegionHighlight.HasValue)
        {
            var region = state.Spatial.RegionHighlight.Value;
            var rx0 = left + region.X0 * (right - left);
            var ry0 = top + region.Y0 * (bottom - top);
            var rx1 = left + region.X1 * (right - left);
            var ry1 = top + region.Y1 * (bottom - top);
            drawList.AddRectFilled(
                new NumericsVector2(rx0, ry0),
                new NumericsVector2(rx1, ry1),
                PackColor(42, 173, 255, 32),
                0.0f,
                ImDrawFlags.None);
            drawList.AddRect(
                new NumericsVector2(rx0, ry0),
                new NumericsVector2(rx1, ry1),
                PackColor(42, 173, 255, 170),
                0.0f,
                ImDrawFlags.None,
                1.2f);
        }
    }

    private static NumericsVector2 ToScreen(SpatialPoint point, float left, float top, float right, float bottom)
    {
        var x = left + point.X * (right - left);
        var y = top + point.Y * (bottom - top);
        return new NumericsVector2(x, y);
    }

    private int BuildVertices(
        SpatialPoint[] points,
        IReadOnlyList<int> highlights,
        int[] memoryAccess,
        int memoryAccessMax,
        bool showHeatmap,
        bool normalizeHeatmapByMax,
        int viewportWidth)
    {
        if (points.Length == 0)
        {
            _lastLodBins = 0;
            _usedAsyncLod = false;
            return 0;
        }

        var lodThreshold = Math.Max(20000, viewportWidth * 4);
        if (points.Length < lodThreshold || viewportWidth <= 0)
        {
            _lastLodBins = points.Length;
            _usedAsyncLod = false;
            EnsureCapacity(points.Length);
            var heatDenom = normalizeHeatmapByMax ? Math.Max(1, memoryAccessMax) : 16;

            for (var i = 0; i < points.Length; i++)
            {
                var p = points[i];
                _vertices[i] = new PointVertex
                {
                    X = p.X * 2.0f - 1.0f,
                    Y = 1.0f - p.Y * 2.0f,
                    Highlight = IsHighlighted(i, highlights) ? 1.0f : 0.0f,
                    Heat = ResolveHeat(i, memoryAccess, heatDenom, showHeatmap)
                };
            }

            return points.Length;
        }

        var bins = Math.Clamp(viewportWidth, 64, 4096);
        _lastLodBins = bins;
        var signature = ComputeSignature(points);
        var signatureChanged = signature != _lastSignature;
        _lastSignature = signature;

        var hasLatest = _lodWorker.TryGetLatest(points.Length, bins, signature, out var result);
        var now = Environment.TickCount64;
        var submitIntervalMs = signatureChanged ? 35 : 120;
        if ((signatureChanged || !hasLatest) && now - _lastSubmitTick >= submitIntervalMs && _lodWorker.QueueDepth == 0)
        {
            var immutable = new SpatialPoint[points.Length];
            Array.Copy(points, immutable, points.Length);
            _lodWorker.Submit(immutable, points.Length, bins, signature);
            _lastSubmitTick = now;
        }

        int[] sampled;
        if (hasLatest)
        {
            sampled = result.SourceIndices;
            _usedAsyncLod = true;
        }
        else
        {
            sampled = ComputeLodIndicesSynchronously(points, bins);
            _usedAsyncLod = false;
        }

        var drawCount = Math.Max(1, sampled.Length);
        EnsureCapacity(drawCount);
        EnsureLodSourceCapacity(drawCount);
        var lodHeatDenom = normalizeHeatmapByMax ? Math.Max(1, memoryAccessMax) : 16;

        for (var i = 0; i < drawCount; i++)
        {
            var sourceIndex = sampled[i];
            if ((uint)sourceIndex >= (uint)points.Length)
            {
                sourceIndex = Math.Clamp(sourceIndex, 0, points.Length - 1);
            }

            _lodSourceIndices[i] = sourceIndex;
            var p = points[sourceIndex];
            _vertices[i] = new PointVertex
            {
                X = p.X * 2.0f - 1.0f,
                Y = 1.0f - p.Y * 2.0f,
                Highlight = IsHighlighted(sourceIndex, highlights) ? 1.0f : 0.0f,
                Heat = ResolveHeat(sourceIndex, memoryAccess, lodHeatDenom, showHeatmap)
            };
        }

        return drawCount;
    }

    private static int[] ComputeLodIndicesSynchronously(SpatialPoint[] points, int bins)
    {
        var minY = new float[bins];
        var maxY = new float[bins];
        var minIdx = new int[bins];
        var maxIdx = new int[bins];

        Array.Fill(minY, float.MaxValue);
        Array.Fill(maxY, float.MinValue);
        Array.Fill(minIdx, -1);
        Array.Fill(maxIdx, -1);

        for (var i = 0; i < points.Length; i++)
        {
            var p = points[i];
            var bin = Math.Clamp((int)(p.X * bins), 0, bins - 1);
            if (p.Y < minY[bin])
            {
                minY[bin] = p.Y;
                minIdx[bin] = i;
            }

            if (p.Y > maxY[bin])
            {
                maxY[bin] = p.Y;
                maxIdx[bin] = i;
            }
        }

        var indices = new List<int>(bins * 2);
        for (var b = 0; b < bins; b++)
        {
            if (minIdx[b] >= 0)
            {
                indices.Add(minIdx[b]);
            }

            if (maxIdx[b] >= 0 && maxIdx[b] != minIdx[b])
            {
                indices.Add(maxIdx[b]);
            }
        }

        if (indices.Count == 0)
        {
            indices.Add(0);
        }

        indices.Sort();
        return indices.ToArray();
    }

    private static bool IsHighlighted(int index, IReadOnlyList<int> highlights)
    {
        for (var i = 0; i < highlights.Count; i++)
        {
            if (highlights[i] == index)
            {
                return true;
            }
        }

        return false;
    }

    private static float ResolveHeat(int index, int[] memoryAccess, int heatDenom, bool showHeatmap)
    {
        if (!showHeatmap || index < 0 || index >= memoryAccess.Length)
        {
            return 0.0f;
        }

        return Math.Clamp(memoryAccess[index] / (float)Math.Max(1, heatDenom), 0.0f, 1.0f);
    }

    private void EnsureCapacity(int required)
    {
        if (required <= _vertices.Length)
        {
            return;
        }

        var newSize = _vertices.Length;
        while (newSize < required)
        {
            newSize *= 2;
        }

        Array.Resize(ref _vertices, newSize);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * Unsafe.SizeOf<PointVertex>(), IntPtr.Zero, BufferUsageHint.DynamicDraw);
    }

    private void EnsureLodSourceCapacity(int required)
    {
        if (required <= _lodSourceIndices.Length)
        {
            return;
        }

        var newSize = _lodSourceIndices.Length;
        while (newSize < required)
        {
            newSize *= 2;
        }

        Array.Resize(ref _lodSourceIndices, newSize);
    }

    private static unsafe void UploadBuffer<T>(T[] source, int count, BufferTarget target) where T : unmanaged
    {
        var sizeInBytes = count * Unsafe.SizeOf<T>();
        var ptr = GL.MapBufferRange(
            target,
            IntPtr.Zero,
            sizeInBytes,
            BufferAccessMask.MapWriteBit | BufferAccessMask.MapUnsynchronizedBit | BufferAccessMask.MapInvalidateRangeBit);

        if (ptr == IntPtr.Zero)
        {
            GL.BufferSubData(target, IntPtr.Zero, sizeInBytes, source);
            return;
        }

        fixed (T* src = source)
        {
            System.Buffer.MemoryCopy(src, ptr.ToPointer(), sizeInBytes, sizeInBytes);
        }

        GL.UnmapBuffer(target);
    }

    private static long ComputeSignature(SpatialPoint[] points)
    {
        unchecked
        {
            var hash = 1469598103934665603L;
            var step = Math.Max(1, points.Length / 64);
            for (var i = 0; i < points.Length; i += step)
            {
                hash ^= points[i].Id;
                hash *= 1099511628211L;
                hash ^= (int)(points[i].X * 100000.0f);
                hash *= 1099511628211L;
                hash ^= (int)(points[i].Y * 100000.0f);
                hash *= 1099511628211L;
            }

            hash ^= points.Length;
            return hash;
        }
    }

    [Conditional("DEBUG")]
    private static void CheckGlError(string phase)
    {
        var error = GL.GetError();
        if (error != ErrorCode.NoError)
        {
            Debug.WriteLine($"[GL:{phase}] {error}");
        }
    }

    public void Dispose()
    {
        _lodWorker.Dispose();
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        _shader.Dispose();
    }

    private static uint PackColor(byte r, byte g, byte b, byte a)
    {
        return (uint)(r | (g << 8) | (b << 16) | (a << 24));
    }

    private sealed class SpatialLodWorker : IDisposable
    {
        private sealed record LodRequest(SpatialPoint[] Snapshot, int Count, int Bins, long Signature);
        public sealed record LodResult(long Signature, int Count, int Bins, int[] SourceIndices);

        private readonly object _lock = new();
        private readonly AutoResetEvent _signal = new(false);
        private readonly Thread _thread;
        private LodRequest? _pending;
        private LodResult? _latest;
        private bool _running = true;
        private int _queueDepth;

        public int QueueDepth => Volatile.Read(ref _queueDepth);

        public SpatialLodWorker()
        {
            _thread = new Thread(ThreadLoop)
            {
                IsBackground = true,
                Name = "Spatial-LOD-Worker"
            };
            _thread.Start();
        }

        public void Submit(SpatialPoint[] immutableSnapshot, int count, int bins, long signature)
        {
            lock (_lock)
            {
                _pending = new LodRequest(immutableSnapshot, count, bins, signature);
                Volatile.Write(ref _queueDepth, 1);
            }

            _signal.Set();
        }

        public bool TryGetLatest(int count, int bins, long signature, out LodResult result)
        {
            lock (_lock)
            {
                if (_latest is null || _latest.Count != count || _latest.Bins != bins || _latest.Signature != signature)
                {
                    result = null!;
                    return false;
                }

                result = _latest;
                return true;
            }
        }

        private void ThreadLoop()
        {
            while (_running)
            {
                _signal.WaitOne();
                if (!_running)
                {
                    return;
                }

                LodRequest? request;
                lock (_lock)
                {
                    request = _pending;
                    _pending = null;
                }

                if (request is null)
                {
                    continue;
                }

                var indices = ComputeLodIndicesSynchronously(request.Snapshot, request.Bins);
                lock (_lock)
                {
                    _latest = new LodResult(request.Signature, request.Count, request.Bins, indices);
                    Volatile.Write(ref _queueDepth, _pending is null ? 0 : 1);
                }
            }
        }

        public void Dispose()
        {
            _running = false;
            _signal.Set();
            if (!_thread.Join(200))
            {
                _thread.Interrupt();
            }

            _signal.Dispose();
        }
    }

    private const string VertexSource = """
#version 330 core
layout(location = 0) in vec2 aPos;
layout(location = 1) in float aHighlight;
layout(location = 2) in float aHeat;

uniform mat4 uProjection;
out float vHighlight;
out float vHeat;

void main()
{
    gl_Position = uProjection * vec4(aPos, 0.0, 1.0);
    vHighlight = aHighlight;
    vHeat = aHeat;
}
""";

    private const string FragmentSource = """
#version 330 core
in float vHighlight;
in float vHeat;
out vec4 FragColor;
uniform int uShowHeatmap;

void main()
{
    vec4 color;
    if (vHighlight > 0.5)
    {
        color = vec4(0.16, 0.68, 0.98, 0.95);
    }
    else
    {
        color = vec4(0.92, 0.92, 0.92, 0.85);
    }

    if (uShowHeatmap != 0 && vHighlight <= 0.5)
    {
        float h = clamp(vHeat, 0.0, 1.0);
        color = mix(color, vec4(0.95, 0.45, 0.22, 0.92), h * 0.85);
    }

    FragColor = color;
}
""";
}
