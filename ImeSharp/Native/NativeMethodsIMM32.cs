using System;
using System.Runtime.InteropServices;

namespace ImeSharp.Native
{
    public partial class NativeMethods
    {
        [DllImport("imm32.dll", SetLastError = true)]
        public static extern IntPtr ImmCreateContext();

        [DllImport("imm32.dll", SetLastError = true)]
        public static extern bool ImmDestroyContext(IntPtr hIMC);

        [DllImport("imm32.dll", SetLastError = true)]
        public static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);

        [DllImport("imm32.dll", SetLastError = true)]
        public static extern IntPtr ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

        [DllImport("imm32.dll", CharSet = CharSet.Unicode)]
        public static extern uint ImmGetCandidateList(IntPtr hIMC, uint deIndex, IntPtr candidateList, uint dwBufLen);

        [DllImport("imm32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int ImmGetCompositionString(IntPtr hIMC, int CompositionStringFlag, IntPtr buffer, int bufferLength);

        [DllImport("imm32.dll", SetLastError = true)]
        public static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern bool TranslateMessage(IntPtr message);
    }
}