using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace SortingVisualizerApp.Rendering;

public sealed class ShaderProgram : IDisposable
{
    public int Handle { get; }

    public ShaderProgram(string vertexSource, string fragmentSource)
    {
        var vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        var fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertexShader);
        GL.AttachShader(Handle, fragmentShader);
        GL.LinkProgram(Handle);

        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out var linked);
        if (linked == 0)
        {
            var log = GL.GetProgramInfoLog(Handle);
            throw new InvalidOperationException($"Program link failed: {log}");
        }

        GL.DetachShader(Handle, vertexShader);
        GL.DetachShader(Handle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    public void Use()
    {
        GL.UseProgram(Handle);
    }

    public int GetUniformLocation(string name)
    {
        return GL.GetUniformLocation(Handle, name);
    }

    public void SetMatrix4(string name, Matrix4 matrix)
    {
        var location = GL.GetUniformLocation(Handle, name);
        GL.UniformMatrix4(location, false, ref matrix);
    }

    public void SetVector4(string name, Vector4 value)
    {
        var location = GL.GetUniformLocation(Handle, name);
        GL.Uniform4(location, value);
    }

    public void SetInt(string name, int value)
    {
        var location = GL.GetUniformLocation(Handle, name);
        GL.Uniform1(location, value);
    }

    private static int CompileShader(ShaderType type, string source)
    {
        var shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out var status);
        if (status == 0)
        {
            var log = GL.GetShaderInfoLog(shader);
            throw new InvalidOperationException($"{type} compile failed: {log}");
        }

        return shader;
    }

    public void Dispose()
    {
        GL.DeleteProgram(Handle);
    }
}
