using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Rendering;

public enum BarsRenderMode
{
    BarsRaw = 0,
    BarsLOD = 1
}

public sealed class BarRenderer : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct InstanceData
    {
        public float X;
        public float Width;
        public float Y0;
        public float Y1;
        public float Type;
        public float ValueNorm;
    }

    private readonly ShaderProgram _shader;

    private readonly int _vao;
    private readonly int _quadVbo;
    private readonly int _instanceVbo;
    private InstanceData[] _instances = new InstanceData[1024];
    private byte[] _highlightByBucket = new byte[1024];

    private readonly LodWorker _lodWorker = new();
    private long _lastLodSubmitTick;
    private long _lastSignature;

    private int _lastLodBins;
    private bool _usedAsyncLod;
    private float _lastBarWidthPx;
    private float _lastNominalBarWidthPx;
    private BarsRenderMode _lastRenderMode;
    private int _lastVisibleCount;

    public int LastLodBins => _lastLodBins;
    public bool UsedAsyncLod => _usedAsyncLod;
    public int LodQueueDepth => _lodWorker.QueueDepth;
    public float LastBarWidthPx => _lastBarWidthPx;
    public float LastNominalBarWidthPx => _lastNominalBarWidthPx;
    public BarsRenderMode LastRenderMode => _lastRenderMode;
    public int LastVisibleCount => _lastVisibleCount;

    public BarRenderer()
    {
        _shader = new ShaderProgram(VertexSource, FragmentSource);

        _vao = GL.GenVertexArray();
        _quadVbo = GL.GenBuffer();
        _instanceVbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVbo);
        var quad = new float[]
        {
            0f, 0f,
            1f, 0f,
            1f, 1f,
            0f, 0f,
            1f, 1f,
            0f, 1f
        };
        GL.BufferData(BufferTarget.ArrayBuffer, quad.Length * sizeof(float), quad, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, _instances.Length * Unsafe.SizeOf<InstanceData>(), IntPtr.Zero, BufferUsageHint.DynamicDraw);

        var stride = Unsafe.SizeOf<InstanceData>();
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, stride, 0);
        GL.VertexAttribDivisor(1, 1);

        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, stride, 4 * sizeof(float));
        GL.VertexAttribDivisor(2, 1);

        GL.EnableVertexAttribArray(3);
        GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, stride, 5 * sizeof(float));
        GL.VertexAttribDivisor(3, 1);

        GL.BindVertexArray(0);
    }

    public void Render(
        int[] data,
        int dataLength,
        int maxValue,
        int[] memoryAccess,
        int memoryAccessMax,
        ReadOnlySpan<SortEvent> recentEvents,
        int viewportWidth,
        int viewportHeight,
        bool visualEnabled,
        float overlayIntensity,
        bool showRanges,
        bool showPivot,
        bool showBuckets,
        bool showMemoryHeatmap,
        bool normalizeHeatmapByMax)
    {
        if (!visualEnabled || dataLength == 0 || viewportWidth <= 0 || viewportHeight <= 0)
        {
            return;
        }

        var instanceCount = BuildInstances(
            data,
            dataLength,
            maxValue,
            memoryAccess,
            memoryAccessMax,
            recentEvents,
            viewportWidth,
            viewportHeight,
            showRanges,
            showPivot,
            showBuckets,
            showMemoryHeatmap,
            normalizeHeatmapByMax);
        if (instanceCount <= 0)
        {
            return;
        }

        EnsureInstanceCapacity(instanceCount);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);
        UploadBuffer(_instances, instanceCount, BufferTarget.ArrayBuffer);
        CheckGlError("bar-upload");

        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _shader.Use();
        var projection = Matrix4.CreateOrthographicOffCenter(0f, viewportWidth, 0f, 1f, -1f, 1f);
        var normalColor = new Vector4(0.93f, 0.93f, 0.93f, 1.0f);
        _shader.SetMatrix4("uProjection", projection);
        _shader.SetVector4("uColorNormal", normalColor);
        _shader.SetVector4("uColorCompare", BlendColor(normalColor, new Vector4(0.65f, 0.65f, 0.65f, 1.0f), overlayIntensity));
        _shader.SetVector4("uColorSwap", BlendColor(normalColor, new Vector4(0.18f, 0.68f, 0.98f, 1.0f), overlayIntensity));
        _shader.SetVector4("uColorPivot", BlendColor(normalColor, new Vector4(0.98f, 0.38f, 0.22f, 1.0f), overlayIntensity));
        _shader.SetVector4("uColorThreadA", BlendColor(normalColor, new Vector4(0.22f, 0.68f, 0.90f, 1.0f), overlayIntensity));
        _shader.SetVector4("uColorThreadB", BlendColor(normalColor, new Vector4(0.90f, 0.70f, 0.22f, 1.0f), overlayIntensity));
        _shader.SetVector4("uColorThreadC", BlendColor(normalColor, new Vector4(0.34f, 0.86f, 0.56f, 1.0f), overlayIntensity));
        _shader.SetVector4("uColorHeat", BlendColor(normalColor, new Vector4(0.95f, 0.45f, 0.22f, 1.0f), overlayIntensity));
        _shader.SetInt("uShowHeatmap", showMemoryHeatmap ? 1 : 0);

        GL.BindVertexArray(_vao);
        GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, instanceCount);
        GL.BindVertexArray(0);
        CheckGlError("bar-draw");
    }

    private int BuildInstances(
        int[] data,
        int dataLength,
        int maxValue,
        int[] memoryAccess,
        int memoryAccessMax,
        ReadOnlySpan<SortEvent> events,
        int viewportWidth,
        int viewportHeight,
        bool showRanges,
        bool showPivot,
        bool showBuckets,
        bool showMemoryHeatmap,
        bool normalizeHeatmapByMax)
    {
        const float minRawBarWidthPx = 1.5f;
        const float minLodBarWidthPx = 2.0f;
        const float minLodBandHeightPx = 2.0f;

        var rawWidth = viewportWidth / (float)Math.Max(1, dataLength);
        var forceLod = rawWidth < minRawBarWidthPx;
        var maxLodBars = Math.Max(8, (int)Math.Floor(viewportWidth / minLodBarWidthPx));

        var bucketSize = forceLod
            ? Math.Max(1, (int)Math.Ceiling(dataLength / (double)maxLodBars))
            : 1;
        var barCount = (int)Math.Ceiling(dataLength / (double)Math.Max(1, bucketSize));

        _lastLodBins = forceLod ? barCount : 0;
        _lastRenderMode = forceLod ? BarsRenderMode.BarsLOD : BarsRenderMode.BarsRaw;
        _lastVisibleCount = barCount;
        EnsureInstanceCapacity(barCount);
        EnsureHighlightCapacity(barCount);
        Array.Clear(_highlightByBucket, 0, barCount);
        for (var i = 0; i < events.Length; i++)
        {
            var ev = events[i];
            var h = EventToHighlightType(ev, showRanges, showPivot, showBuckets);
            if (h == 0)
            {
                continue;
            }

            if (IsRangeLikeEvent(ev.Type) && ev.I >= 0 && ev.J >= 0)
            {
                var left = Math.Min(ev.I, ev.J);
                var right = Math.Max(ev.I, ev.J);
                var startBucket = Math.Clamp(left / bucketSize, 0, barCount - 1);
                var endBucket = Math.Clamp(right / bucketSize, 0, barCount - 1);
                for (var b = startBucket; b <= endBucket; b++)
                {
                    _highlightByBucket[b] = Math.Max(_highlightByBucket[b], h);
                }

                continue;
            }

            if (ev.I >= 0 && ev.I < dataLength)
            {
                var bucket = ev.I / bucketSize;
                if ((uint)bucket < (uint)barCount)
                {
                    _highlightByBucket[bucket] = Math.Max(_highlightByBucket[bucket], h);
                }
            }

            if (ev.J >= 0 && ev.J < dataLength)
            {
                var bucket = ev.J / bucketSize;
                if ((uint)bucket < (uint)barCount)
                {
                    _highlightByBucket[bucket] = Math.Max(_highlightByBucket[bucket], h);
                }
            }
        }

        var useAsyncLod = forceLod && dataLength >= 20_000 && bucketSize > 1;
        int[] mins;
        int[] maxs;
        float[] avgs;

        var signature = ComputeDataSignature(data, dataLength);
        var signatureChanged = signature != _lastSignature;
        _lastSignature = signature;

        if (useAsyncLod)
        {
            var now = Environment.TickCount64;
            var hasLatest = _lodWorker.TryGetLatest(dataLength, bucketSize, barCount, signature, out var lodResult);
            var wantFreshSubmit = signatureChanged || !hasLatest;
            var submitIntervalMs = signatureChanged ? 30 : 120;

            if (wantFreshSubmit && now - _lastLodSubmitTick >= submitIntervalMs && _lodWorker.QueueDepth == 0)
            {
                var immutable = new int[dataLength];
                Array.Copy(data, immutable, dataLength);
                _lodWorker.Submit(immutable, dataLength, bucketSize, barCount, signature);
                _lastLodSubmitTick = now;
            }

            if (hasLatest)
            {
                mins = lodResult.Mins;
                maxs = lodResult.Maxs;
                avgs = lodResult.Averages;
                _usedAsyncLod = true;
            }
            else
            {
                ComputeLodSynchronously(data, dataLength, bucketSize, barCount, out mins, out maxs, out avgs);
                _usedAsyncLod = false;
            }
        }
        else
        {
            ComputeLodSynchronously(data, dataLength, bucketSize, barCount, out mins, out maxs, out avgs);
            _usedAsyncLod = false;
        }

        var denom = Math.Max(1, maxValue);
        var heatDenom = Math.Max(1.0f, normalizeHeatmapByMax ? memoryAccessMax : 16.0f);
        var width = viewportWidth / (float)Math.Max(1, barCount);
        _lastNominalBarWidthPx = width;
        _lastBarWidthPx = forceLod ? Math.Max(minLodBarWidthPx, width) : Math.Max(minRawBarWidthPx, width);
        var minBandNormalized = viewportHeight > 0
            ? (minLodBandHeightPx / viewportHeight)
            : 0.0f;

        var outCount = 0;
        for (var b = 0; b < barCount; b++)
        {
            var min = mins[b];
            var max = maxs[b];
            if (min == int.MaxValue)
            {
                continue;
            }

            var x = b * width;
            var y0 = bucketSize == 1 ? 0.0f : Math.Clamp(min / (float)denom, 0.0f, 1.0f);
            var y1 = Math.Clamp(max / (float)denom, 0.0f, 1.0f);
            if (y1 < y0)
            {
                (y0, y1) = (y1, y0);
            }

            if (forceLod && y1 - y0 < minBandNormalized)
            {
                var center = (y0 + y1) * 0.5f;
                y0 = Math.Clamp(center - (minBandNormalized * 0.5f), 0.0f, 1.0f);
                y1 = Math.Clamp(center + (minBandNormalized * 0.5f), 0.0f, 1.0f);
                if (y1 <= y0)
                {
                    y1 = Math.Min(1.0f, y0 + minBandNormalized);
                }
            }

            var drawWidth = forceLod
                ? Math.Max(minLodBarWidthPx, width)
                : Math.Max(minRawBarWidthPx, width - 0.5f);

            _instances[outCount++] = new InstanceData
            {
                X = x,
                Width = drawWidth,
                Y0 = y0,
                Y1 = Math.Max(y0 + 0.0005f, y1),
                Type = _highlightByBucket[b],
                ValueNorm = showMemoryHeatmap
                    ? ComputeBucketHeat(memoryAccess, dataLength, bucketSize, b, heatDenom)
                    : Math.Clamp(avgs[b] / denom, 0.0f, 1.0f)
            };
        }

        return outCount;
    }

    private static void ComputeLodSynchronously(
        int[] data,
        int dataLength,
        int bucketSize,
        int barCount,
        out int[] mins,
        out int[] maxs,
        out float[] avgs)
    {
        mins = new int[barCount];
        maxs = new int[barCount];
        avgs = new float[barCount];

        for (var b = 0; b < barCount; b++)
        {
            var start = b * bucketSize;
            var end = Math.Min(start + bucketSize, dataLength);

            var min = int.MaxValue;
            var max = int.MinValue;
            long sum = 0;

            for (var i = start; i < end; i++)
            {
                var v = data[i];
                if (v < min)
                {
                    min = v;
                }

                if (v > max)
                {
                    max = v;
                }

                sum += v;
            }

            mins[b] = min;
            maxs[b] = max;
            avgs[b] = end > start ? (float)(sum / (double)(end - start)) : 0.0f;
        }
    }

    private void EnsureInstanceCapacity(int required)
    {
        if (required <= _instances.Length)
        {
            return;
        }

        var newSize = _instances.Length;
        while (newSize < required)
        {
            newSize *= 2;
        }

        Array.Resize(ref _instances, newSize);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, _instances.Length * Unsafe.SizeOf<InstanceData>(), IntPtr.Zero, BufferUsageHint.DynamicDraw);
    }

    private void EnsureHighlightCapacity(int required)
    {
        if (required <= _highlightByBucket.Length)
        {
            return;
        }

        var newSize = _highlightByBucket.Length;
        while (newSize < required)
        {
            newSize *= 2;
        }

        Array.Resize(ref _highlightByBucket, newSize);
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

    private static float ComputeBucketHeat(int[] memoryAccess, int dataLength, int bucketSize, int bucket, float heatDenom)
    {
        if (memoryAccess.Length == 0 || dataLength == 0)
        {
            return 0.0f;
        }

        var start = bucket * bucketSize;
        if (start >= dataLength)
        {
            return 0.0f;
        }

        var end = Math.Min(start + bucketSize, dataLength);
        long sum = 0;
        var count = 0;
        for (var i = start; i < end; i++)
        {
            if ((uint)i >= (uint)memoryAccess.Length)
            {
                break;
            }

            sum += memoryAccess[i];
            count++;
        }

        if (count <= 0)
        {
            return 0.0f;
        }

        var avg = sum / (float)count;
        return Math.Clamp(avg / heatDenom, 0.0f, 1.0f);
    }

    private static byte EventToHighlightType(SortEvent ev, bool showRanges, bool showPivot, bool showBuckets)
    {
        return ev.Type switch
        {
            SortEventType.Compare => 1,
            SortEventType.Swap or SortEventType.Write => 2,
            SortEventType.MarkPivot when showPivot => 3,
            SortEventType.MarkRange when showRanges => 3,
            SortEventType.MarkBucket when showBuckets => 3,
            SortEventType.ParallelTaskStart => (byte)(4 + (Math.Abs(ev.Aux) % 3)),
            _ => 0
        };
    }

    private static bool IsRangeLikeEvent(SortEventType type)
    {
        return type is SortEventType.MarkRange
            or SortEventType.ParallelTaskStart
            or SortEventType.MergeStart
            or SortEventType.MergeComplete;
    }

    private static long ComputeDataSignature(int[] data, int dataLength)
    {
        if (dataLength <= 0)
        {
            return 0;
        }

        unchecked
        {
            var hash = 1469598103934665603L;
            var step = Math.Max(1, dataLength / 64);
            for (var i = 0; i < dataLength; i += step)
            {
                hash ^= data[i];
                hash *= 1099511628211L;
            }

            hash ^= data[dataLength - 1];
            hash *= 1099511628211L;
            hash ^= dataLength;
            return hash;
        }
    }

    private static Vector4 BlendColor(Vector4 baseColor, Vector4 accent, float intensity)
    {
        var t = Math.Clamp(intensity, 0.0f, 1.0f);
        return new Vector4(
            baseColor.X + (accent.X - baseColor.X) * t,
            baseColor.Y + (accent.Y - baseColor.Y) * t,
            baseColor.Z + (accent.Z - baseColor.Z) * t,
            1.0f);
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
        GL.DeleteBuffer(_instanceVbo);
        GL.DeleteBuffer(_quadVbo);
        GL.DeleteVertexArray(_vao);
        _shader.Dispose();
    }

    private sealed class LodWorker : IDisposable
    {
        public sealed record LodResult(long Signature, int DataLength, int BucketSize, int BarCount, int[] Mins, int[] Maxs, float[] Averages);
        private sealed record LodRequest(int[] Snapshot, int DataLength, int BucketSize, int BarCount, long Signature);

        private readonly object _lock = new();
        private readonly AutoResetEvent _signal = new(false);
        private readonly Thread _thread;

        private LodRequest? _pending;
        private LodResult? _latest;
        private bool _running = true;
        private int _queueDepth;

        public int QueueDepth => Volatile.Read(ref _queueDepth);

        public LodWorker()
        {
            _thread = new Thread(ThreadLoop)
            {
                IsBackground = true,
                Name = "LOD-Worker"
            };
            _thread.Start();
        }

        public void Submit(int[] immutableSnapshot, int dataLength, int bucketSize, int barCount, long signature)
        {
            lock (_lock)
            {
                _pending = new LodRequest(immutableSnapshot, dataLength, bucketSize, barCount, signature);
                Volatile.Write(ref _queueDepth, 1);
            }

            _signal.Set();
        }

        public bool TryGetLatest(int dataLength, int bucketSize, int barCount, long signature, out LodResult result)
        {
            lock (_lock)
            {
                if (_latest is null
                    || _latest.Signature != signature
                    || _latest.DataLength != dataLength
                    || _latest.BucketSize != bucketSize
                    || _latest.BarCount != barCount)
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

                var mins = new int[request.BarCount];
                var maxs = new int[request.BarCount];
                var avgs = new float[request.BarCount];

                for (var b = 0; b < request.BarCount; b++)
                {
                    var start = b * request.BucketSize;
                    var end = Math.Min(start + request.BucketSize, request.DataLength);

                    var min = int.MaxValue;
                    var max = int.MinValue;
                    long sum = 0;

                    for (var i = start; i < end; i++)
                    {
                        var v = request.Snapshot[i];
                        if (v < min)
                        {
                            min = v;
                        }

                        if (v > max)
                        {
                            max = v;
                        }

                        sum += v;
                    }

                    mins[b] = min;
                    maxs[b] = max;
                    avgs[b] = end > start ? (float)(sum / (double)(end - start)) : 0.0f;
                }

                lock (_lock)
                {
                    _latest = new LodResult(request.Signature, request.DataLength, request.BucketSize, request.BarCount, mins, maxs, avgs);
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
layout(location = 1) in vec4 iRect;
layout(location = 2) in float iType;
layout(location = 3) in float iValue;

uniform mat4 uProjection;
out float vType;
out float vValue;

void main()
{
    float x = iRect.x + aPos.x * iRect.y;
    float y = mix(iRect.z, iRect.w, aPos.y);
    gl_Position = uProjection * vec4(x, y, 0.0, 1.0);
    vType = iType;
    vValue = iValue;
}
""";

    private const string FragmentSource = """
#version 330 core
in float vType;
in float vValue;
out vec4 FragColor;

uniform vec4 uColorNormal;
uniform vec4 uColorCompare;
uniform vec4 uColorSwap;
uniform vec4 uColorPivot;
uniform vec4 uColorThreadA;
uniform vec4 uColorThreadB;
uniform vec4 uColorThreadC;
uniform vec4 uColorHeat;
uniform int uShowHeatmap;

void main()
{
    int t = int(vType + 0.5);
    vec4 color;
    if (t == 1)
    {
        color = uColorCompare;
    }
    else if (t == 2)
    {
        color = uColorSwap;
    }
    else if (t == 3)
    {
        color = uColorPivot;
    }
    else if (t == 4)
    {
        color = uColorThreadA;
    }
    else if (t == 5)
    {
        color = uColorThreadB;
    }
    else if (t == 6)
    {
        color = uColorThreadC;
    }
    else
    {
        color = uColorNormal;
    }

    if (uShowHeatmap != 0 && t == 0)
    {
        float h = clamp(vValue, 0.0, 1.0);
        color = mix(color, uColorHeat, h * 0.85);
    }

    FragColor = color;
}
""";
}
