using System;
using System.Runtime.InteropServices;

namespace ImeSharp.Native
{
    public partial class NativeMethods
    {
        public const int S_OK = 0x00000000;
        public const int S_FALSE = 0x00000001;

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
    }
}