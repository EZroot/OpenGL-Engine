using ImGuiNET;
using OpenGL_Engine.Structs;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ImDrawVert = OpenGL_Engine.Structs.ImDrawVert;
using Vector2 = System.Numerics.Vector2;

namespace OpenGL_Engine.Gui
{
    internal class GuiRenderer : IDisposable
    {
        private BufferObjects _buffers;           // Holds VAO, VBO, and IBO
        private ShaderData _shaderData;           // Holds shader-related fields
        private ImGuiAttributes _imguiAttributes; // Holds ImGui attributes
        private FrameData _frameData;

        public GuiRenderer(int framebufferWidth, int framebufferHeight, float dpiScaleX, float dpiScaleY)
        {
            ImGui.CreateContext();
            ImGui.SetCurrentContext(ImGui.GetCurrentContext());
            ImGui.StyleColorsDark();
            ImGui.GetIO().Fonts.AddFontDefault();
            _frameData.DpiScaleX = dpiScaleX;
            _frameData.DpiScaleY = dpiScaleY;
            _frameData.FramebufferWidth = framebufferWidth;
            _frameData.FramebufferHeight = framebufferHeight;
            CreateDeviceResources();
            SetPerFrameImGuiData(1f / 60f, _frameData.FramebufferWidth, _frameData.FramebufferHeight);
            ImGui.NewFrame();
            _frameData.FrameBegun = true;
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
            _shaderData.VertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(_shaderData.VertexShader, vertexSource);
            GL.CompileShader(_shaderData.VertexShader);
            GL.GetShader(_shaderData.VertexShader, ShaderParameter.CompileStatus, out int vertexStatus);
            if (vertexStatus != (int)All.True)
            {
                string infoLog = GL.GetShaderInfoLog(_shaderData.VertexShader);
                Console.WriteLine($"ERROR::SHADER::VERTEX::COMPILATION_FAILED\n{infoLog}");
            }
            _shaderData.FragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(_shaderData.FragmentShader, fragmentSource);
            GL.CompileShader(_shaderData.FragmentShader);
            GL.GetShader(_shaderData.FragmentShader, ShaderParameter.CompileStatus, out int fragmentStatus);
            if (fragmentStatus != (int)All.True)
            {
                string infoLog = GL.GetShaderInfoLog(_shaderData.FragmentShader);
                Console.WriteLine($"ERROR::SHADER::FRAGMENT::COMPILATION_FAILED\n{infoLog}");
            }
            _shaderData.ProgramId = GL.CreateProgram();
            GL.AttachShader(_shaderData.ProgramId, _shaderData.VertexShader);
            GL.AttachShader(_shaderData.ProgramId, _shaderData.FragmentShader);
            GL.LinkProgram(_shaderData.ProgramId);
            GL.GetProgram(_shaderData.ProgramId, GetProgramParameterName.LinkStatus, out int linkStatus);
            if (linkStatus != (int)All.True)
            {
                string infoLog = GL.GetProgramInfoLog(_shaderData.ProgramId);
                Console.WriteLine($"ERROR::SHADER::PROGRAM::LINKING_FAILED\n{infoLog}");
            }
            _imguiAttributes.AttribLocationTex = GL.GetUniformLocation(_shaderData.ProgramId, "Texture");
            _imguiAttributes.AttribLocationProjMtx = GL.GetUniformLocation(_shaderData.ProgramId, "projection_matrix");
            _imguiAttributes.AttribLocationPosition = GL.GetAttribLocation(_shaderData.ProgramId, "aPosition");
            _imguiAttributes.AttribLocationUV = GL.GetAttribLocation(_shaderData.ProgramId, "aUV");
            _imguiAttributes.AttribLocationColor = GL.GetAttribLocation(_shaderData.ProgramId, "aColor");
            _buffers.VertexArray = GL.GenVertexArray();
            _buffers.VertexBuffer = GL.GenBuffer();
            _buffers.IndexBuffer = GL.GenBuffer();
            GL.BindVertexArray(_buffers.VertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _buffers.VertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, 10000, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _buffers.IndexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, 2000, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.EnableVertexAttribArray(_imguiAttributes.AttribLocationPosition);
            GL.EnableVertexAttribArray(_imguiAttributes.AttribLocationUV);
            GL.EnableVertexAttribArray(_imguiAttributes.AttribLocationColor);
            GL.VertexAttribPointer(_imguiAttributes.AttribLocationPosition, 2, VertexAttribPointerType.Float, false, Marshal.SizeOf<ImDrawVert>(), 0);
            GL.VertexAttribPointer(_imguiAttributes.AttribLocationUV, 2, VertexAttribPointerType.Float, false, Marshal.SizeOf<ImDrawVert>(), 8);
            GL.VertexAttribPointer(_imguiAttributes.AttribLocationColor, 4, VertexAttribPointerType.UnsignedByte, true, Marshal.SizeOf<ImDrawVert>(), 16);
            var io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);
            byte[] pixelData = new byte[width * height * bytesPerPixel];
            Marshal.Copy(pixels, pixelData, 0, pixelData.Length);
            _imguiAttributes.FontTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _imguiAttributes.FontTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, pixelData);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            io.Fonts.SetTexID((IntPtr)_imguiAttributes.FontTexture);
            io.Fonts.ClearTexData();
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }
        public void Update(GameWindow wnd, float deltaSeconds)
        {
            if (_frameData.FrameBegun)
            {
                ImGui.Render();
            }
            _frameData.FramebufferWidth = wnd.FramebufferSize.X;
            _frameData.FramebufferHeight = wnd.FramebufferSize.Y;
            _frameData.DpiScaleX = (float)wnd.FramebufferSize.X / (float)wnd.ClientSize.X;
            _frameData.DpiScaleY = (float)wnd.FramebufferSize.Y / (float)wnd.ClientSize.Y;
            SetPerFrameImGuiData(deltaSeconds, _frameData.FramebufferWidth, _frameData.FramebufferHeight);
            UpdateImGuiInput(wnd);
            _frameData.FrameBegun = true;
            ImGui.NewFrame();
        }
        private void SetPerFrameImGuiData(float deltaSeconds, int framebufferWidth, int framebufferHeight)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = new Vector2(framebufferWidth / _frameData.DpiScaleX, framebufferHeight / _frameData.DpiScaleY);
            io.DisplayFramebufferScale = new Vector2(_frameData.DpiScaleX, _frameData.DpiScaleY);
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
            float clampedX = Math.Clamp(mouseState.X, 0.0f, _frameData.FramebufferWidth / _frameData.DpiScaleX);
            float clampedY = Math.Clamp(mouseState.Y, 0.0f, _frameData.FramebufferHeight / _frameData.DpiScaleY);
            var screenPoint = new Vector2(clampedX, clampedY);
            io.MousePos = screenPoint;
            io.MouseWheel = mouseState.ScrollDelta.Y;
            io.MouseWheelH = mouseState.ScrollDelta.X;
        }
        public void Render()
        {
            if (_frameData.FrameBegun)
            {
                _frameData.FrameBegun = false;
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
                GL.Viewport(0, 0, _frameData.FramebufferWidth, _frameData.FramebufferHeight);
                Matrix4 projection = Matrix4.CreateOrthographicOffCenter(
                    0.0f,
                    _frameData.FramebufferWidth,
                    _frameData.FramebufferHeight,
                    0.0f,
                    -1.0f,
                    1.0f
                );
                GL.UseProgram(_shaderData.ProgramId);
                GL.Uniform1(_imguiAttributes.AttribLocationTex, 0);
                GL.UniformMatrix4(_imguiAttributes.AttribLocationProjMtx, false, ref projection);
                GL.BindVertexArray(_buffers.VertexArray);
                for (int n = 0; n < draw_data.CmdListsCount; n++)
                {
                    ImDrawListPtr cmd_list = draw_data.CmdLists[n];
                    GL.BindBuffer(BufferTarget.ArrayBuffer, _buffers.VertexBuffer);
                    GL.BufferData(BufferTarget.ArrayBuffer, cmd_list.VtxBuffer.Size * Marshal.SizeOf<ImDrawVert>(), cmd_list.VtxBuffer.Data, BufferUsageHint.StreamDraw);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, _buffers.IndexBuffer);
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
                            int scissorY = (int)(_frameData.FramebufferHeight - pcmd.ClipRect.W);
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
            _frameData.FramebufferWidth = framebufferWidth;
            _frameData.FramebufferHeight = framebufferHeight;
            SetPerFrameImGuiData(1f / 60f, framebufferWidth, framebufferHeight);
        }
        public void Dispose()
        {
            GL.DeleteTexture(_imguiAttributes.FontTexture);
            GL.DeleteProgram(_shaderData.ProgramId);
            GL.DeleteShader(_shaderData.FragmentShader);
            GL.DeleteShader(_shaderData.VertexShader);
            GL.DeleteBuffer(_buffers.VertexBuffer);
            GL.DeleteBuffer(_buffers.IndexBuffer);
            GL.DeleteVertexArray(_buffers.VertexArray);
        }
    }
}
