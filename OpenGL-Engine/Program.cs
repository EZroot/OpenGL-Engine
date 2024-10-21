using System;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTKVector2 = OpenTK.Mathematics.Vector2;
using SystemVector2 = System.Numerics.Vector2;
namespace OpenGLTriangle
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ImDrawVert
    {
        public SystemVector2 pos;
        public SystemVector2 uv;
        public uint col;
    }
    public class Program
    {
        static void Main(string[] args)
        {
            var gameWindowSettings = GameWindowSettings.Default;
            var nativeWindowSettings = new NativeWindowSettings()
            {
                Size = new Vector2i(800, 600),
                Title = "Opengl engine",
                Flags = ContextFlags.ForwardCompatible,
            };
            using (var game = new Game(gameWindowSettings, nativeWindowSettings))
            {
                game.Run();
            }
        }
    }
    public class Game : GameWindow
    {
        private int _triangleVao;
        private int _triangleVbo;
        private int _triangleShaderProgram;
        private ImGuiController _imGuiController;
        public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }
        public void GetDpiScale(GameWindow wnd, out float dpiScaleX, out float dpiScaleY)
        {
            MonitorHandle? currentMonitor = wnd.CurrentMonitor;
            if (currentMonitor != null)
            {
                bool success = this.TryGetCurrentMonitorScale(out dpiScaleX, out dpiScaleY);
                if (!success)
                {
                    dpiScaleX = 1.0f;
                    dpiScaleY = 1.0f;
                    Console.WriteLine("[DEBUG] Failed to get monitor scale factor. Using default 1.0f.");
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Retrieved DPI Scale: X={dpiScaleX}, Y={dpiScaleY}");
                }
            }
            else
            {
                dpiScaleX = 1.0f;
                dpiScaleY = 1.0f;
                Console.WriteLine("[DEBUG] No current monitor found. Using default DPI scale 1.0f.");
            }
        }
        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            GetDpiScale(this, out float dpiScaleX, out float dpiScaleY);
            var framebufferSize = this.FramebufferSize;
            _imGuiController = new ImGuiController(framebufferSize.X, framebufferSize.Y, dpiScaleX, dpiScaleY);
            Console.WriteLine($"[DEBUG] Initialized ImGuiController with FramebufferSize: {framebufferSize.X}x{framebufferSize.Y} and DPI Scale: X={dpiScaleX}, Y={dpiScaleY}");
            InitializeTriangle();
        }
        private void InitializeTriangle()
        {
float[] vertices = {
         0.0f,  0.8f, 0.0f,  1.0f, 0.0f, 0.0f,   
         0.8f, -0.8f, 0.0f,  0.0f, 1.0f, 0.0f,   
        -0.8f, -0.8f, 0.0f,  0.0f, 0.0f, 1.0f    
    };
            _triangleVao = GL.GenVertexArray();
            GL.BindVertexArray(_triangleVao);
            _triangleVbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _triangleVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            _triangleShaderProgram = CreateShaderProgram(_vertexShaderSource, _fragmentShaderSource);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }
        private readonly string _vertexShaderSource = @"
            #version 330 core
            layout(location = 0) in vec3 aPosition;
            layout(location = 1) in vec3 aColor;
            out vec3 vertexColor;
            void main()
            {
                gl_Position = vec4(aPosition, 1.0);
                vertexColor = aColor;
            }
        ";
        private readonly string _fragmentShaderSource = @"
            #version 330 core
            in vec3 vertexColor;
            out vec4 FragColor;
            void main()
            {
                FragColor = vec4(vertexColor, 1.0);
            }
        ";
        private int CreateShaderProgram(string vertexShaderSource, string fragmentShaderSource)
        {
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int vertexStatus);
            if (vertexStatus != (int)All.True)
            {
                string infoLog = GL.GetShaderInfoLog(vertexShader);
                Console.WriteLine($"ERROR::SHADER::VERTEX::COMPILATION_FAILED\n{infoLog}");
            }
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int fragmentStatus);
            if (fragmentStatus != (int)All.True)
            {
                string infoLog = GL.GetShaderInfoLog(fragmentShader);
                Console.WriteLine($"ERROR::SHADER::FRAGMENT::COMPILATION_FAILED\n{infoLog}");
            }
            int shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.LinkProgram(shaderProgram);
            GL.GetProgram(shaderProgram, GetProgramParameterName.LinkStatus, out int linkStatus);
            if (linkStatus != (int)All.True)
            {
                string infoLog = GL.GetProgramInfoLog(shaderProgram);
                Console.WriteLine($"ERROR::SHADER::PROGRAM::LINKING_FAILED\n{infoLog}");
            }
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            return shaderProgram;
        }
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
            {
                Close();
            }
        }
        private void CheckOpenGLError(string context)
{
    ErrorCode error;
    while ((error = GL.GetError()) != ErrorCode.NoError)
    {
        Console.WriteLine($"OpenGL Error in {context}: {error}");
    }
}
protected override void OnRenderFrame(FrameEventArgs args)
{
    base.OnRenderFrame(args);
    _imGuiController.Update(this, (float)args.Time);
    GL.Enable(EnableCap.DepthTest);  
    GL.Disable(EnableCap.Blend);     
    GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);  
    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    CheckOpenGLError("Clear");
    GL.UseProgram(_triangleShaderProgram);
    CheckOpenGLError("UseProgram");
    GL.BindVertexArray(_triangleVao);
    CheckOpenGLError("Bind VAO");
    GL.DrawArrays(PrimitiveType.Triangles, 0, 3);  
    CheckOpenGLError("DrawArrays");
    GL.BindVertexArray(0);
    GL.UseProgram(0);
    GL.Disable(EnableCap.DepthTest);  
    GL.Enable(EnableCap.Blend);       
    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);  
    ImGui.Begin("Sample Window");
    ImGui.Text("This is a UI window with a close button and background panel.");
    if (ImGui.Button("Close"))
    {
        Console.WriteLine("Close button clicked!");
    }
    ImGui.End();
    _imGuiController.Render();
    SwapBuffers();
}
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            var framebufferSize = this.FramebufferSize;
            GL.Viewport(0, 0, framebufferSize.X, framebufferSize.Y);
            _imGuiController.WindowResized(framebufferSize.X, framebufferSize.Y);
        }
        protected override void OnUnload()
        {
            base.OnUnload();
            GL.DeleteBuffer(_triangleVbo);
            GL.DeleteVertexArray(_triangleVao);
            GL.DeleteProgram(_triangleShaderProgram);
            _imGuiController.Dispose();
        }
    }
    public class ImGuiController : IDisposable
    {
        private int _vertexArray;
        private int _vertexBuffer;
        private int _indexBuffer;
        private int _vertexShader;
        private int _fragmentShader;
        private int _shaderProgram;
        private int _fontTexture;
        private int _attribLocationTex;
        private int _attribLocationProjMtx;
        private int _attribLocationPosition;
        private int _attribLocationUV;
        private int _attribLocationColor;
        private bool _frameBegun;
        private float _dpiScaleX = 1.0f;
        private float _dpiScaleY = 1.0f;
        private int _framebufferWidth;
        private int _framebufferHeight;
        public ImGuiController(int framebufferWidth, int framebufferHeight, float dpiScaleX, float dpiScaleY)
        {
            ImGui.CreateContext();
            ImGui.SetCurrentContext(ImGui.GetCurrentContext());
            ImGui.StyleColorsDark();
            ImGui.GetIO().Fonts.AddFontDefault();
            _dpiScaleX = dpiScaleX;
            _dpiScaleY = dpiScaleY;
            _framebufferWidth = framebufferWidth;
            _framebufferHeight = framebufferHeight;
            CreateDeviceResources();
            SetPerFrameImGuiData(1f / 60f, _framebufferWidth, _framebufferHeight);
            ImGui.NewFrame();
            _frameBegun = true;
        }
        public void CreateDeviceResources()
        {
            const string vertexSource = @"
                #version 330 core
                layout(location = 0) in vec2 aPosition;
                layout(location = 1) in vec2 aUV;
                layout(location = 2) in vec4 aColor;
                uniform mat4 projection_matrix;
                out vec2 vUV;
                out vec4 vColor;
                void main()
                {
                    vUV = aUV;
                    vColor = aColor;
                    gl_Position = projection_matrix * vec4(aPosition.xy, 0.0, 1.0);
                }
            ";
            const string fragmentSource = @"
                #version 330 core
                in vec2 vUV;
                in vec4 vColor;
                uniform sampler2D Texture;
                out vec4 FragColor;
                void main()
                {
                    FragColor = vColor * texture(Texture, vUV.st);
                }
            ";
            _vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(_vertexShader, vertexSource);
            GL.CompileShader(_vertexShader);
            GL.GetShader(_vertexShader, ShaderParameter.CompileStatus, out int vertexStatus);
            if (vertexStatus != (int)All.True)
            {
                string infoLog = GL.GetShaderInfoLog(_vertexShader);
                Console.WriteLine($"ERROR::SHADER::VERTEX::COMPILATION_FAILED\n{infoLog}");
            }
            _fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(_fragmentShader, fragmentSource);
            GL.CompileShader(_fragmentShader);
            GL.GetShader(_fragmentShader, ShaderParameter.CompileStatus, out int fragmentStatus);
            if (fragmentStatus != (int)All.True)
            {
                string infoLog = GL.GetShaderInfoLog(_fragmentShader);
                Console.WriteLine($"ERROR::SHADER::FRAGMENT::COMPILATION_FAILED\n{infoLog}");
            }
            _shaderProgram = GL.CreateProgram();
            GL.AttachShader(_shaderProgram, _vertexShader);
            GL.AttachShader(_shaderProgram, _fragmentShader);
            GL.LinkProgram(_shaderProgram);
            GL.GetProgram(_shaderProgram, GetProgramParameterName.LinkStatus, out int linkStatus);
            if (linkStatus != (int)All.True)
            {
                string infoLog = GL.GetProgramInfoLog(_shaderProgram);
                Console.WriteLine($"ERROR::SHADER::PROGRAM::LINKING_FAILED\n{infoLog}");
            }
            _attribLocationTex = GL.GetUniformLocation(_shaderProgram, "Texture");
            _attribLocationProjMtx = GL.GetUniformLocation(_shaderProgram, "projection_matrix");
            _attribLocationPosition = GL.GetAttribLocation(_shaderProgram, "aPosition");
            _attribLocationUV = GL.GetAttribLocation(_shaderProgram, "aUV");
            _attribLocationColor = GL.GetAttribLocation(_shaderProgram, "aColor");
            _vertexArray = GL.GenVertexArray();
            _vertexBuffer = GL.GenBuffer();
            _indexBuffer = GL.GenBuffer();
            GL.BindVertexArray(_vertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, 10000, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, 2000, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.EnableVertexAttribArray(_attribLocationPosition);
            GL.EnableVertexAttribArray(_attribLocationUV);
            GL.EnableVertexAttribArray(_attribLocationColor);
            GL.VertexAttribPointer(_attribLocationPosition, 2, VertexAttribPointerType.Float, false, Marshal.SizeOf<ImDrawVert>(), 0);
            GL.VertexAttribPointer(_attribLocationUV, 2, VertexAttribPointerType.Float, false, Marshal.SizeOf<ImDrawVert>(), 8);
            GL.VertexAttribPointer(_attribLocationColor, 4, VertexAttribPointerType.UnsignedByte, true, Marshal.SizeOf<ImDrawVert>(), 16);
            var io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);
            byte[] pixelData = new byte[width * height * bytesPerPixel];
            Marshal.Copy(pixels, pixelData, 0, pixelData.Length);
            _fontTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, pixelData);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            io.Fonts.SetTexID((IntPtr)_fontTexture);
            io.Fonts.ClearTexData();
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }
public void Update(GameWindow wnd, float deltaSeconds)
{
    if (_frameBegun)
    {
        ImGui.Render();
    }
    _framebufferWidth = wnd.FramebufferSize.X;
    _framebufferHeight = wnd.FramebufferSize.Y;
    _dpiScaleX = (float)wnd.FramebufferSize.X / (float)wnd.ClientSize.X;
    _dpiScaleY = (float)wnd.FramebufferSize.Y / (float)wnd.ClientSize.Y;
    SetPerFrameImGuiData(deltaSeconds, _framebufferWidth, _framebufferHeight);
    UpdateImGuiInput(wnd);
    _frameBegun = true;
    ImGui.NewFrame();
}
private void SetPerFrameImGuiData(float deltaSeconds, int framebufferWidth, int framebufferHeight)
{
    ImGuiIOPtr io = ImGui.GetIO();
    io.DisplaySize = new SystemVector2(framebufferWidth / _dpiScaleX, framebufferHeight / _dpiScaleY);
    io.DisplayFramebufferScale = new SystemVector2(_dpiScaleX, _dpiScaleY);
    io.DeltaTime = deltaSeconds;
}
        private byte BoolToByte(bool value) => value ? (byte)1 : (byte)0;
        private void UpdateImGuiInput(GameWindow wnd)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            var mouseState = wnd.MouseState;
            var keyboardState = wnd.KeyboardState;
            io.MouseDown[0] = mouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left);
            io.MouseDown[1] = mouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right);
            io.MouseDown[2] = mouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Middle);
            float clampedX = Math.Clamp(mouseState.X, 0.0f, _framebufferWidth / _dpiScaleX);
            float clampedY = Math.Clamp(mouseState.Y, 0.0f, _framebufferHeight / _dpiScaleY);
            var screenPoint = new SystemVector2(clampedX, clampedY);
            io.MousePos = screenPoint;
            io.MouseWheel = mouseState.ScrollDelta.Y;
            io.MouseWheelH = mouseState.ScrollDelta.X;
        }
public void Render()
{
    if (_frameBegun)
    {
        _frameBegun = false;
        ImGui.Render();
        ImDrawDataPtr draw_data = ImGui.GetDrawData();
        if (draw_data.CmdListsCount == 0)
            return;
        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);  
        GL.Enable(EnableCap.ScissorTest);
        GL.Viewport(0, 0, _framebufferWidth, _framebufferHeight);
        Matrix4 projection = Matrix4.CreateOrthographicOffCenter(
            0.0f,
            _framebufferWidth,
            _framebufferHeight,
            0.0f,
            -1.0f,
            1.0f
        );
        GL.UseProgram(_shaderProgram);
        GL.Uniform1(_attribLocationTex, 0);  
        GL.UniformMatrix4(_attribLocationProjMtx, false, ref projection);  
        GL.BindVertexArray(_vertexArray);
        for (int n = 0; n < draw_data.CmdListsCount; n++)
        {
            ImDrawListPtr cmd_list = draw_data.CmdLists[n];
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, cmd_list.VtxBuffer.Size * Marshal.SizeOf<ImDrawVert>(), cmd_list.VtxBuffer.Data, BufferUsageHint.StreamDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, cmd_list.IdxBuffer.Size * sizeof(ushort), cmd_list.IdxBuffer.Data, BufferUsageHint.StreamDraw);
            for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException();  
                }
                else
                {
                    GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);
                    int scissorX = (int)(pcmd.ClipRect.X);
                    int scissorY = (int)(_framebufferHeight - pcmd.ClipRect.W);
                    int scissorWidth = (int)(pcmd.ClipRect.Z - pcmd.ClipRect.X);
                    int scissorHeight = (int)(pcmd.ClipRect.W - pcmd.ClipRect.Y);
                    GL.Scissor(scissorX, scissorY, scissorWidth, scissorHeight);
                    GL.DrawElementsBaseVertex(
                        PrimitiveType.Triangles,
                        (int)pcmd.ElemCount,
                        DrawElementsType.UnsignedShort,
                        (IntPtr)(pcmd.IdxOffset * sizeof(ushort)),
                        (int)pcmd.VtxOffset
                    );
                }
            }
        }
        GL.Disable(EnableCap.ScissorTest);
        GL.Disable(EnableCap.Blend);  
        GL.Enable(EnableCap.DepthTest);  
        GL.BindVertexArray(0);
        GL.UseProgram(0);
    }
}
        public void WindowResized(int framebufferWidth, int framebufferHeight)
        {
            _framebufferWidth = framebufferWidth;
            _framebufferHeight = framebufferHeight;
            SetPerFrameImGuiData(1f / 60f, framebufferWidth, framebufferHeight);
        }
        public void Dispose()
        {
            GL.DeleteTexture(_fontTexture);
            GL.DeleteProgram(_shaderProgram);
            GL.DeleteShader(_fragmentShader);
            GL.DeleteShader(_vertexShader);
            GL.DeleteBuffer(_vertexBuffer);
            GL.DeleteBuffer(_indexBuffer);
            GL.DeleteVertexArray(_vertexArray);
        }
    }
}
