using System.Numerics;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SortingVisualizerApp.UI;

public sealed class ImGuiController : IDisposable
{
    private readonly GameWindow _window;
    private readonly List<char> _pressedChars = new();

    private int _vertexArray;
    private int _vertexBuffer;
    private int _indexBuffer;
    private int _vertexBufferSize = 10000;
    private int _indexBufferSize = 2000;

    private int _fontTexture;

    private int _shader;
    private int _shaderVert;
    private int _shaderFrag;
    private int _attribLocationTex;
    private int _attribLocationProjMtx;

    private bool _frameBegun;

    public ImGuiController(GameWindow window)
    {
        _window = window;

        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        SetStyle();
        io.Fonts.AddFontDefault();

        CreateDeviceResources();

        _window.TextInput += OnTextInput;

        SetPerFrameData(1f / 60f);
        ImGui.NewFrame();
        _frameBegun = true;
    }

    public void Update(float deltaSeconds)
    {
        if (_frameBegun)
        {
            ImGui.Render();
        }

        SetPerFrameData(deltaSeconds);
        UpdateImGuiInput();

        _frameBegun = true;
        ImGui.NewFrame();
    }

    public void Render()
    {
        if (!_frameBegun)
        {
            return;
        }

        _frameBegun = false;
        ImGui.Render();
        RenderImDrawData(ImGui.GetDrawData());
    }

    public void WindowResized(int width, int height)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(width, height);
    }

    private void SetPerFrameData(float deltaSeconds)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(_window.ClientSize.X, _window.ClientSize.Y);
        io.DisplayFramebufferScale = Vector2.One;
        io.DeltaTime = Math.Max(1f / 1000f, deltaSeconds);
    }

    private void UpdateImGuiInput()
    {
        var io = ImGui.GetIO();
        var mouseState = _window.MouseState;
        var keyboard = _window.KeyboardState;

        io.MouseDown[0] = mouseState.IsButtonDown(MouseButton.Left);
        io.MouseDown[1] = mouseState.IsButtonDown(MouseButton.Right);
        io.MouseDown[2] = mouseState.IsButtonDown(MouseButton.Middle);

        io.MousePos = new Vector2(mouseState.X, mouseState.Y);
        io.MouseWheel = mouseState.ScrollDelta.Y;
        io.MouseWheelH = mouseState.ScrollDelta.X;

        io.KeyCtrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        io.KeyAlt = keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt);
        io.KeyShift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
        io.KeySuper = keyboard.IsKeyDown(Keys.LeftSuper) || keyboard.IsKeyDown(Keys.RightSuper);

        foreach (var mapping in KeyMappings)
        {
            io.AddKeyEvent(mapping.ImGuiKey, keyboard.IsKeyDown(mapping.Key));
        }

        lock (_pressedChars)
        {
            foreach (var c in _pressedChars)
            {
                io.AddInputCharacter(c);
            }

            _pressedChars.Clear();
        }
    }

    private void CreateDeviceResources()
    {
        _vertexBuffer = GL.GenBuffer();
        _indexBuffer = GL.GenBuffer();
        _vertexArray = GL.GenVertexArray();

        RecreateFontTexture();

        const string vertexSource = """
#version 330 core
layout(location = 0) in vec2 in_position;
layout(location = 1) in vec2 in_texCoord;
layout(location = 2) in vec4 in_color;

uniform mat4 projection_matrix;
out vec2 frag_uv;
out vec4 frag_color;

void main()
{
    frag_uv = in_texCoord;
    frag_color = in_color;
    gl_Position = projection_matrix * vec4(in_position, 0.0, 1.0);
}
""";

        const string fragmentSource = """
#version 330 core
in vec2 frag_uv;
in vec4 frag_color;
uniform sampler2D in_fontTexture;
out vec4 output_color;

void main()
{
    output_color = frag_color * texture(in_fontTexture, frag_uv);
}
""";

        _shader = GL.CreateProgram();
        _shaderVert = GL.CreateShader(ShaderType.VertexShader);
        _shaderFrag = GL.CreateShader(ShaderType.FragmentShader);

        GL.ShaderSource(_shaderVert, vertexSource);
        GL.ShaderSource(_shaderFrag, fragmentSource);
        GL.CompileShader(_shaderVert);
        GL.CompileShader(_shaderFrag);

        GL.GetShader(_shaderVert, ShaderParameter.CompileStatus, out var vertStatus);
        if (vertStatus == 0)
        {
            throw new InvalidOperationException($"ImGui vertex shader failed: {GL.GetShaderInfoLog(_shaderVert)}");
        }

        GL.GetShader(_shaderFrag, ShaderParameter.CompileStatus, out var fragStatus);
        if (fragStatus == 0)
        {
            throw new InvalidOperationException($"ImGui fragment shader failed: {GL.GetShaderInfoLog(_shaderFrag)}");
        }

        GL.AttachShader(_shader, _shaderVert);
        GL.AttachShader(_shader, _shaderFrag);
        GL.LinkProgram(_shader);

        GL.GetProgram(_shader, GetProgramParameterName.LinkStatus, out var linkStatus);
        if (linkStatus == 0)
        {
            throw new InvalidOperationException($"ImGui shader link failed: {GL.GetProgramInfoLog(_shader)}");
        }

        _attribLocationTex = GL.GetUniformLocation(_shader, "in_fontTexture");
        _attribLocationProjMtx = GL.GetUniformLocation(_shader, "projection_matrix");

        GL.BindVertexArray(_vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        unsafe
        {
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, sizeof(ImDrawVert), 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(ImDrawVert), 8);

            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, sizeof(ImDrawVert), 16);
        }

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
    }

    private void RecreateFontTexture()
    {
        var io = ImGui.GetIO();

        io.Fonts.GetTexDataAsRGBA32(out nint pixels, out var width, out var height, out _);

        if (_fontTexture != 0)
        {
            GL.DeleteTexture(_fontTexture);
        }

        _fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

        io.Fonts.SetTexID((IntPtr)_fontTexture);
        io.Fonts.ClearTexData();
    }

    private unsafe void RenderImDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.TotalVtxCount <= 0)
        {
            return;
        }

        var io = ImGui.GetIO();
        drawData.ScaleClipRects(io.DisplayFramebufferScale);

        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.ScissorTest);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.UseProgram(_shader);
        GL.Uniform1(_attribLocationTex, 0);

        var projection = OpenTK.Mathematics.Matrix4.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0f, -1f, 1f);
        GL.UniformMatrix4(_attribLocationProjMtx, false, ref projection);

        GL.BindVertexArray(_vertexArray);

        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];
            var vertexSize = cmdList.VtxBuffer.Size * sizeof(ImDrawVert);
            if (vertexSize > _vertexBufferSize)
            {
                while (vertexSize > _vertexBufferSize)
                {
                    _vertexBufferSize *= 2;
                }

                GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
                GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            }

            var indexSize = cmdList.IdxBuffer.Size * sizeof(ushort);
            if (indexSize > _indexBufferSize)
            {
                while (indexSize > _indexBufferSize)
                {
                    _indexBufferSize *= 2;
                }

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
                GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertexSize, (IntPtr)cmdList.VtxBuffer.Data);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, indexSize, (IntPtr)cmdList.IdxBuffer.Data);

            for (var cmdIndex = 0; cmdIndex < cmdList.CmdBuffer.Size; cmdIndex++)
            {
                var cmd = cmdList.CmdBuffer[cmdIndex];
                if (cmd.UserCallback != IntPtr.Zero)
                {
                    continue;
                }

                GL.BindTexture(TextureTarget.Texture2D, (int)cmd.TextureId);

                var clip = cmd.ClipRect;
                GL.Scissor(
                    (int)clip.X,
                    (int)(io.DisplaySize.Y - clip.W),
                    Math.Max(0, (int)(clip.Z - clip.X)),
                    Math.Max(0, (int)(clip.W - clip.Y)));

                GL.DrawElementsBaseVertex(
                    PrimitiveType.Triangles,
                    (int)cmd.ElemCount,
                    DrawElementsType.UnsignedShort,
                    (IntPtr)(cmd.IdxOffset * sizeof(ushort)),
                    (int)cmd.VtxOffset);
            }
        }

        GL.Disable(EnableCap.ScissorTest);
        GL.BindVertexArray(0);
        GL.UseProgram(0);
    }

    private static void SetStyle()
    {
        var style = ImGui.GetStyle();
        style.WindowRounding = 2.0f;
        style.FrameRounding = 1.0f;
        style.ScrollbarRounding = 1.0f;
        style.GrabRounding = 1.0f;

        var colors = style.Colors;
        colors[(int)ImGuiCol.Text] = new Vector4(0.95f, 0.95f, 0.95f, 1.0f);
        colors[(int)ImGuiCol.WindowBg] = new Vector4(0.05f, 0.05f, 0.05f, 0.96f);
        colors[(int)ImGuiCol.ChildBg] = new Vector4(0.08f, 0.08f, 0.08f, 1.0f);
        colors[(int)ImGuiCol.PopupBg] = new Vector4(0.08f, 0.08f, 0.08f, 1.0f);
        colors[(int)ImGuiCol.Border] = new Vector4(0.2f, 0.2f, 0.2f, 1.0f);
        colors[(int)ImGuiCol.Button] = new Vector4(0.18f, 0.18f, 0.18f, 1.0f);
        colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.26f, 0.26f, 0.26f, 1.0f);
        colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.36f, 0.36f, 0.36f, 1.0f);
        colors[(int)ImGuiCol.FrameBg] = new Vector4(0.13f, 0.13f, 0.13f, 1.0f);
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.2f, 0.2f, 0.2f, 1.0f);
        colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.28f, 0.28f, 0.28f, 1.0f);
        colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
        colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.95f, 0.95f, 0.95f, 1.0f);
        colors[(int)ImGuiCol.CheckMark] = new Vector4(0.2f, 0.75f, 0.95f, 1.0f);
        colors[(int)ImGuiCol.Header] = new Vector4(0.16f, 0.16f, 0.16f, 1.0f);
        colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.25f, 0.25f, 0.25f, 1.0f);
        colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.34f, 0.34f, 0.34f, 1.0f);
    }

    private void OnTextInput(TextInputEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.AsString))
        {
            lock (_pressedChars)
            {
                _pressedChars.AddRange(e.AsString);
            }
        }
    }

    public void Dispose()
    {
        _window.TextInput -= OnTextInput;

        if (_fontTexture != 0)
        {
            GL.DeleteTexture(_fontTexture);
        }

        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);
        GL.DeleteVertexArray(_vertexArray);

        GL.DetachShader(_shader, _shaderVert);
        GL.DetachShader(_shader, _shaderFrag);
        GL.DeleteShader(_shaderVert);
        GL.DeleteShader(_shaderFrag);
        GL.DeleteProgram(_shader);

        ImGui.DestroyContext();
    }

    private readonly struct KeyMapping(Keys key, ImGuiKey imGuiKey)
    {
        public Keys Key { get; } = key;
        public ImGuiKey ImGuiKey { get; } = imGuiKey;
    }

    private static readonly KeyMapping[] KeyMappings =
    {
        new(Keys.Tab, ImGuiKey.Tab),
        new(Keys.Left, ImGuiKey.LeftArrow),
        new(Keys.Right, ImGuiKey.RightArrow),
        new(Keys.Up, ImGuiKey.UpArrow),
        new(Keys.Down, ImGuiKey.DownArrow),
        new(Keys.PageUp, ImGuiKey.PageUp),
        new(Keys.PageDown, ImGuiKey.PageDown),
        new(Keys.Home, ImGuiKey.Home),
        new(Keys.End, ImGuiKey.End),
        new(Keys.Delete, ImGuiKey.Delete),
        new(Keys.Backspace, ImGuiKey.Backspace),
        new(Keys.Enter, ImGuiKey.Enter),
        new(Keys.Escape, ImGuiKey.Escape),
        new(Keys.A, ImGuiKey.A),
        new(Keys.C, ImGuiKey.C),
        new(Keys.V, ImGuiKey.V),
        new(Keys.X, ImGuiKey.X),
        new(Keys.Y, ImGuiKey.Y),
        new(Keys.Z, ImGuiKey.Z),
        new(Keys.Space, ImGuiKey.Space)
    };
}
