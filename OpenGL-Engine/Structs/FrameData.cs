using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGL_Engine.Structs
{
    internal struct FrameData
    {
        public float DpiScaleX;
        public float DpiScaleY;
        public int FramebufferWidth;
        public int FramebufferHeight;
        public bool FrameBegun;
    }
}
