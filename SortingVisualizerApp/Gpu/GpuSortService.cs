using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using SortingVisualizerApp.Core;

namespace SortingVisualizerApp.Gpu;

public sealed class GpuSortService : IDisposable
{
    private const int LocalSize = 256;

    private readonly string _shaderRoot;

    private int _bitonicProgram;
    private int _radixCountProgram;
    private int _radixScatterProgram;
    private int _radixCopyProgram;

    private bool _initialized;

    public bool IsAvailable { get; private set; }
    public string LastError { get; private set; } = string.Empty;

    public GpuSortService(string shaderRoot)
    {
        _shaderRoot = shaderRoot;
    }

    public bool Initialize()
    {
        return EnsureInitialized();
    }

    public bool TrySortBitonic(int[] input, out int[] sorted, out GpuExecutionMetrics metrics, Action<double>? progress = null)
    {
        sorted = Array.Empty<int>();
        metrics = GpuExecutionMetrics.Empty with { Kind = GpuSortKind.Bitonic };

        if (!EnsureInitialized())
        {
            metrics = metrics with { Message = LastError };
            return false;
        }

        if (input.Length == 0)
        {
            sorted = Array.Empty<int>();
            metrics = metrics with { UsedGpu = true, Progress01 = 1.0, Message = "empty" };
            progress?.Invoke(1.0);
            return true;
        }

        var paddedLength = NextPow2(Math.Max(1, input.Length));
        var padded = new int[paddedLength];
        Array.Fill(padded, int.MaxValue);
        Array.Copy(input, padded, input.Length);

        var uploadTimer = Stopwatch.StartNew();
        var ssbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, paddedLength * sizeof(int), padded, BufferUsageHint.DynamicCopy);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, ssbo);
        uploadTimer.Stop();

        var dispatchTimer = Stopwatch.StartNew();
        GL.UseProgram(_bitonicProgram);

        var locJ = GL.GetUniformLocation(_bitonicProgram, "uJ");
        var locK = GL.GetUniformLocation(_bitonicProgram, "uK");
        var locN = GL.GetUniformLocation(_bitonicProgram, "uN");
        GL.Uniform1(locN, paddedLength);

        var stageCount = 0;
        var dispatchCount = 0;
        for (var k = 2u; k <= (uint)paddedLength; k <<= 1)
        {
            for (var j = k >> 1; j > 0; j >>= 1)
            {
                GL.Uniform1(locJ, j);
                GL.Uniform1(locK, k);

                var groups = (paddedLength + LocalSize - 1) / LocalSize;
                GL.DispatchCompute(groups, 1, 1);
                GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
                dispatchCount++;
                stageCount++;
            }

            if (k > 1)
            {
                var phase = Math.Log2(k) / Math.Max(1.0, Math.Log2(paddedLength));
                progress?.Invoke(Math.Clamp(phase, 0.0, 0.98));
            }
        }

        GL.Finish();
        dispatchTimer.Stop();

        var readbackTimer = Stopwatch.StartNew();
        var output = new int[paddedLength];
        GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, paddedLength * sizeof(int), output);
        readbackTimer.Stop();

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        GL.DeleteBuffer(ssbo);

        sorted = new int[input.Length];
        Array.Copy(output, sorted, input.Length);

        metrics = new GpuExecutionMetrics(
            Kind: GpuSortKind.Bitonic,
            UsedGpu: true,
            CpuSortMs: 0,
            UploadMs: uploadTimer.Elapsed.TotalMilliseconds,
            DispatchMs: dispatchTimer.Elapsed.TotalMilliseconds,
            ReadbackMs: readbackTimer.Elapsed.TotalMilliseconds,
            DispatchCount: dispatchCount,
            WorkGroupCount: (paddedLength + LocalSize - 1) / LocalSize,
            StageCount: stageCount,
            GpuMemoryBytes: (long)paddedLength * sizeof(int),
            Progress01: 1.0,
            Message: "ok");

        progress?.Invoke(1.0);
        return true;
    }

    public bool TrySortRadixLsd(int[] input, out int[] sorted, out GpuExecutionMetrics metrics, Action<double>? progress = null)
    {
        sorted = Array.Empty<int>();
        metrics = GpuExecutionMetrics.Empty with { Kind = GpuSortKind.RadixLsd };

        if (!EnsureInitialized())
        {
            metrics = metrics with { Message = LastError };
            return false;
        }

        if (input.Length == 0)
        {
            sorted = Array.Empty<int>();
            metrics = metrics with { UsedGpu = true, Progress01 = 1.0, Message = "empty" };
            progress?.Invoke(1.0);
            return true;
        }

        var n = input.Length;
        var data = new uint[n];
        for (var i = 0; i < n; i++)
        {
            data[i] = unchecked((uint)Math.Max(0, input[i]));
        }

        var uploadTimer = Stopwatch.StartNew();
        var dataBuffer = GL.GenBuffer();
        var tempBuffer = GL.GenBuffer();
        var countBuffer = GL.GenBuffer();
        var offsetBuffer = GL.GenBuffer();

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, dataBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, n * sizeof(uint), data, BufferUsageHint.DynamicCopy);

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, tempBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, n * sizeof(uint), IntPtr.Zero, BufferUsageHint.DynamicCopy);

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, countBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, 16 * sizeof(uint), IntPtr.Zero, BufferUsageHint.DynamicCopy);

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, offsetBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, 16 * sizeof(uint), IntPtr.Zero, BufferUsageHint.DynamicCopy);

        uploadTimer.Stop();

        var groups = (n + LocalSize - 1) / LocalSize;
        var dispatchTimer = Stopwatch.StartNew();
        var dispatchCount = 0;
        var stageCount = 0;

        var locCountShift = GL.GetUniformLocation(_radixCountProgram, "uShift");
        var locCountN = GL.GetUniformLocation(_radixCountProgram, "uN");
        var locScatterShift = GL.GetUniformLocation(_radixScatterProgram, "uShift");
        var locScatterN = GL.GetUniformLocation(_radixScatterProgram, "uN");
        var locCopyN = GL.GetUniformLocation(_radixCopyProgram, "uN");

        var zeros = new uint[16];
        var counts = new uint[16];
        var offsets = new uint[16];

        const int passCount = 8;
        for (var pass = 0; pass < passCount; pass++)
        {
            var shift = pass * 4;

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, countBuffer);
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 16 * sizeof(uint), zeros);

            GL.UseProgram(_radixCountProgram);
            GL.Uniform1(locCountShift, shift);
            GL.Uniform1(locCountN, n);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, dataBuffer);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, countBuffer);
            GL.DispatchCompute(groups, 1, 1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
            dispatchCount++;

            GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 16 * sizeof(uint), counts);
            offsets[0] = 0;
            for (var i = 1; i < 16; i++)
            {
                offsets[i] = offsets[i - 1] + counts[i - 1];
            }

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, offsetBuffer);
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 16 * sizeof(uint), offsets);

            GL.UseProgram(_radixScatterProgram);
            GL.Uniform1(locScatterShift, shift);
            GL.Uniform1(locScatterN, n);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, dataBuffer);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, tempBuffer);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, offsetBuffer);
            GL.DispatchCompute(groups, 1, 1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
            dispatchCount++;

            GL.UseProgram(_radixCopyProgram);
            GL.Uniform1(locCopyN, n);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, tempBuffer);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, dataBuffer);
            GL.DispatchCompute(groups, 1, 1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
            dispatchCount++;

            stageCount++;
            progress?.Invoke((pass + 1) / (double)passCount * 0.98);
        }

        GL.Finish();
        dispatchTimer.Stop();

        var readbackTimer = Stopwatch.StartNew();
        var output = new uint[n];
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, dataBuffer);
        GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, n * sizeof(uint), output);
        readbackTimer.Stop();

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        GL.DeleteBuffer(dataBuffer);
        GL.DeleteBuffer(tempBuffer);
        GL.DeleteBuffer(countBuffer);
        GL.DeleteBuffer(offsetBuffer);

        sorted = new int[n];
        for (var i = 0; i < n; i++)
        {
            sorted[i] = unchecked((int)output[i]);
        }

        metrics = new GpuExecutionMetrics(
            Kind: GpuSortKind.RadixLsd,
            UsedGpu: true,
            CpuSortMs: 0,
            UploadMs: uploadTimer.Elapsed.TotalMilliseconds,
            DispatchMs: dispatchTimer.Elapsed.TotalMilliseconds,
            ReadbackMs: readbackTimer.Elapsed.TotalMilliseconds,
            DispatchCount: dispatchCount,
            WorkGroupCount: groups,
            StageCount: stageCount,
            GpuMemoryBytes: (long)n * sizeof(uint) * 2 + 16 * sizeof(uint) * 2,
            Progress01: 1.0,
            Message: "ok");

        progress?.Invoke(1.0);
        return true;
    }

    private bool EnsureInitialized()
    {
        if (_initialized)
        {
            return IsAvailable;
        }

        _initialized = true;

        try
        {
            _bitonicProgram = CompileComputeProgram(Path.Combine(_shaderRoot, "BitonicSort.comp"));
            _radixCountProgram = CompileComputeProgram(Path.Combine(_shaderRoot, "RadixCount.comp"));
            _radixScatterProgram = CompileComputeProgram(Path.Combine(_shaderRoot, "RadixScatter.comp"));
            _radixCopyProgram = CompileComputeProgram(Path.Combine(_shaderRoot, "RadixCopy.comp"));

            IsAvailable = true;
            LastError = string.Empty;
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            LastError = ex.Message;
            DisposePrograms();
        }

        return IsAvailable;
    }

    private static int CompileComputeProgram(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Compute shader not found: {path}");
        }

        var source = File.ReadAllText(path);
        var shader = GL.CreateShader(ShaderType.ComputeShader);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out var shaderStatus);
        if (shaderStatus == 0)
        {
            var log = GL.GetShaderInfoLog(shader);
            GL.DeleteShader(shader);
            throw new InvalidOperationException($"Compute shader compile failed ({Path.GetFileName(path)}): {log}");
        }

        var program = GL.CreateProgram();
        GL.AttachShader(program, shader);
        GL.LinkProgram(program);

        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var programStatus);
        GL.DetachShader(program, shader);
        GL.DeleteShader(shader);

        if (programStatus == 0)
        {
            var log = GL.GetProgramInfoLog(program);
            GL.DeleteProgram(program);
            throw new InvalidOperationException($"Compute program link failed ({Path.GetFileName(path)}): {log}");
        }

        return program;
    }

    private static int NextPow2(int value)
    {
        var n = 1;
        while (n < value)
        {
            n <<= 1;
        }

        return n;
    }

    private void DisposePrograms()
    {
        if (_bitonicProgram != 0)
        {
            GL.DeleteProgram(_bitonicProgram);
            _bitonicProgram = 0;
        }

        if (_radixCountProgram != 0)
        {
            GL.DeleteProgram(_radixCountProgram);
            _radixCountProgram = 0;
        }

        if (_radixScatterProgram != 0)
        {
            GL.DeleteProgram(_radixScatterProgram);
            _radixScatterProgram = 0;
        }

        if (_radixCopyProgram != 0)
        {
            GL.DeleteProgram(_radixCopyProgram);
            _radixCopyProgram = 0;
        }
    }

    public void Dispose()
    {
        DisposePrograms();
    }
}
