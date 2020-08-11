using System;
using System.Text;
using System.Runtime.InteropServices;

namespace ImeSharp.Native
{
    public partial class NativeMethods
    {
        #region Constants

        public const int S_OK = 0x00000000;
        public const int S_FALSE = 0x00000001;

        public const int WM_KEYFIRST = 0x0100;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_CHAR = 0x0102;
        public const int WM_DEADCHAR = 0x0103;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;
        public const int WM_SYSCHAR = 0x0106;
        public const int WM_SYSDEADCHAR = 0x0107;
        public const int WM_UNICHAR = 0x0109;
        public const int WM_KEYLAST = 0x0109;
        public const int UNICODE_NOCHAR = 0xFFFF;

        public const int WM_NOTIFY = 0x004E;
        public const int WM_INPUTLANGCHANGEREQUEST = 0x0050;
        public const int WM_INPUTLANGCHANGE = 0x0051;
        public const int WM_TCARD = 0x0052;
        public const int WM_HELP = 0x0053;
        public const int WM_USERCHANGED = 0x0054;
        public const int WM_NOTIFYFORMAT = 0x0055;

        public const int GWL_WNDPROC = -4;

        public const int WM_ACTIVATE = 0x0006;
        // WM_ACTIVATE state values
        public const int WA_INACTIVE = 0;
        public const int WA_ACTIVE = 1;
        public const int WA_CLICKACTIVE = 2;

        public const int WM_SETFOCUS = 0x0007;
        public const int WM_KILLFOCUS = 0x0008;

        public const int CLSCTX_INPROC_SERVER = 0x1;

        #endregion Constants

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(SM nIndex);

        // We have this wrapper because casting IntPtr to int may
        // generate OverflowException when one of high 32 bits is set.
        public static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetKeyboardLayout(int dwLayout);

        public delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        // This static method is required because legacy OSes do not support
        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);


        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, StringBuilder lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, ref IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);


        [DllImport("ole32.dll", CharSet = CharSet.Auto, ExactSpelling = true, PreserveSig = false)]
        public static extern int CoCreateInstance(Guid rclsid, IntPtr pUnkOuter, int dwClsContext, Guid riid, out IntPtr ppv);
    }
}
