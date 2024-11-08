using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGL_Engine.Utils
{
    public static class DpiUtils
    {
        public static void GetDpiScale(GameWindow wnd, out float dpiScaleX, out float dpiScaleY)
        {
            MonitorHandle? currentMonitor = wnd.CurrentMonitor;
            if (currentMonitor != null)
            {
                bool success = wnd.TryGetCurrentMonitorScale(out dpiScaleX, out dpiScaleY);//this.TryGetCurrentMonitorScale(out dpiScaleX, out dpiScaleY);
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
    }
}
