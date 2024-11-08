using System;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using OpenGL_Engine.Gui;
using OpenGL_Engine.Structs;
using OpenGL_Engine.Utils;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using ImDrawVert = OpenGL_Engine.Structs.ImDrawVert;
using OpenTKVector2 = OpenTK.Mathematics.Vector2;
using SystemVector2 = System.Numerics.Vector2;
namespace OpenGL_Engine
{

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
        private GuiRenderer _imGuiController;
        public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }
        
        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            DpiUtils.GetDpiScale(this, out float dpiScaleX, out float dpiScaleY);
            var framebufferSize = this.FramebufferSize;
            _imGuiController = new GuiRenderer(framebufferSize.X, framebufferSize.Y, dpiScaleX, dpiScaleY);
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
            _triangleShaderProgram = CreateShaderProgram(VertexShaderSource, FragmentShaderSource);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }
        private readonly string VertexShaderSource = @"
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
        private readonly string FragmentShaderSource = @"
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

protected override void OnRenderFrame(FrameEventArgs args)
{
    base.OnRenderFrame(args);
    _imGuiController.Update(this, (float)args.Time);
    GL.Enable(EnableCap.DepthTest);  
    GL.Disable(EnableCap.Blend);     
    GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);  
    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            OpenGLUtils.CheckOpenGLError("Clear");
    GL.UseProgram(_triangleShaderProgram);
            OpenGLUtils.CheckOpenGLError("UseProgram");
    GL.BindVertexArray(_triangleVao);
            OpenGLUtils.CheckOpenGLError("Bind VAO");
    GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            OpenGLUtils.CheckOpenGLError("DrawArrays");
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
}
