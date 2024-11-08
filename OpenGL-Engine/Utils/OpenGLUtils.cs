using OpenGL_Engine.Debug;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGL_Engine.Utils
{
    public static class OpenGLUtils
    {
        public static void CheckOpenGLError(string context)
        {
            ErrorCode error;
            while ((error = GL.GetError()) != ErrorCode.NoError)
            {
                Logger.Log($"<color=red>OpenGL Error</color> in {context}: <color=red>{error}</color>");
            }
        }
    }
}
