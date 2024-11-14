using System;
using System.Reflection;
using System.Windows.Forms;

namespace Mapper
{
    internal static class WndProcHelper
    {
        public static void InvokeWndProc(Control control, ref Message m)
        {
            var methodInfo = typeof(Control).GetMethod("WndProc", BindingFlags.Instance | BindingFlags.NonPublic);
            methodInfo?.Invoke(control, new object[] { m });
        }
    }
    
    public class NoMouseWheelPanel : Panel
    {
        protected override void WndProc(ref Message m)
        {
            if (!this.DesignMode)
            {
                const int WM_MOUSEWHEEL = 0x020A;

                if (m.Msg == WM_MOUSEWHEEL)
                {
                    // Get the mouse position relative to the form
                    //Point mousePosition = this.PointToClient(Control.MousePosition);
                    //Point mousePosition = Control.MousePosition;

                    // Check if the mouse position is within the bounds of the Panel
                    //if (this.Bounds.Contains(mousePosition))
                    {
                        if (this.Parent != null)
                        {
                            WndProcHelper.InvokeWndProc(this.Parent, ref m);
                        }
                        return;
                    }
                }
            }

            base.WndProc(ref m);
        }
    }

    public class NoMouseWheelPictureBox : PictureBox
    {
        protected override void WndProc(ref Message m)
        {
            if (!this.DesignMode)
            {
                const int WM_MOUSEWHEEL = 0x020A;

                if (m.Msg == WM_MOUSEWHEEL)
                {
                    // Get the mouse position relative to the form
                    //Point mousePosition = this.PointToClient(Control.MousePosition);
                    //Point mousePosition = Control.MousePosition;
                    // Check if the mouse position is within the bounds of the PictureBox
                    //if (this.Bounds.Contains(mousePosition))
                    {
                        if (this.Parent != null)
                        {
                            WndProcHelper.InvokeWndProc(this.Parent, ref m);
                        }
                        return;
                    }
                }
            }

            base.WndProc(ref m);
        }
    }

    public class ZoomPanel : Panel
    {
        public delegate void ZoomEventHandler(object sender, ZoomEventArgs e);
        public event ZoomEventHandler? ZoomChanged;

        public class ZoomEventArgs : EventArgs
        {
            public int Delta { get; }

            public ZoomEventArgs(int delta)
            {
                Delta = delta;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (!this.DesignMode)
            {
                const int WM_MOUSEWHEEL = 0x020A;

                if (m.Msg == WM_MOUSEWHEEL)
                {
                    // Get the mouse position relative to the form
                    //Point mousePosition = this.PointToClient(Control.MousePosition);
                    //Point mousePosition = Control.MousePosition;
                    // Check if the mouse position is within the bounds of the Panel
                    //if (this.Bounds.Contains(mousePosition))
                    {
                        int delta = (int)m.WParam >> 16;

                        // Raise the custom zoom event
                        try
                        {
                            ZoomChanged?.Invoke(this, new ZoomEventArgs(delta));
                        }
                        catch
                        {
                            // Handle exceptions as needed
                        }

                        //if (this.Parent != null)
                        //{
                        //    WndProcHelper.InvokeWndProc(this.Parent, ref m);
                        //}
                        return;
                    }
                }
            }

            base.WndProc(ref m);
        }
    }
}