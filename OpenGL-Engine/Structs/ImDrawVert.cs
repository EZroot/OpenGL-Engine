using System.Numerics;
using System.Runtime.InteropServices;

namespace OpenGL_Engine.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ImDrawVert
    {
        public Vector2 pos;
        public Vector2 uv;
        public uint col;
    }
}
