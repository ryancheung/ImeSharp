using System;
using System.Runtime.InteropServices;

namespace ImeSharp.Native
{
    public partial class NativeMethods
    {
        #region Constants

        public const int WM_IME_SETCONTEXT = 0x0281;
        public const int WM_IME_NOTIFY = 0x0282;
        public const int WM_IME_CONTROL = 0x0283;
        public const int WM_IME_COMPOSITIONFULL = 0x0284;
        public const int WM_IME_SELECT = 0x0285;
        public const int WM_IME_CHAR = 0x0286;
        public const int WM_IME_REQUEST = 0x0288;
        public const int WM_IME_KEYDOWN = 0x0290;
        public const int WM_IME_KEYUP = 0x0291;
        public const int WM_IME_STARTCOMPOSITION = 0x010D;
        public const int WM_IME_ENDCOMPOSITION = 0x010E;
        public const int WM_IME_COMPOSITION = 0x010F;
        public const int WM_IME_KEYLAST = 0x010F;

        // wParam of report message WM_IME_NOTIFY
        public const int IMN_CLOSESTATUSWINDOW = 0x0001;
        public const int IMN_OPENSTATUSWINDOW = 0x0002;
        public const int IMN_CHANGECANDIDATE = 0x0003;
        public const int IMN_CLOSECANDIDATE = 0x0004;
        public const int IMN_OPENCANDIDATE = 0x0005;
        public const int IMN_SETCONVERSIONMODE = 0x0006;
        public const int IMN_SETSENTENCEMODE = 0x0007;
        public const int IMN_SETOPENSTATUS = 0x0008;
        public const int IMN_SETCANDIDATEPOS = 0x0009;
        public const int IMN_SETCOMPOSITIONFONT = 0x000A;
        public const int IMN_SETCOMPOSITIONWINDOW = 0x000B;
        public const int IMN_SETSTATUSWINDOWPOS = 0x000C;
        public const int IMN_GUIDELINE = 0x000D;
        public const int IMN_PRIVATE = 0x000E;

        // wParam of report message WM_IME_REQUEST
        public const int IMR_COMPOSITIONWINDOW = 0x0001;
        public const int IMR_CANDIDATEWINDOW = 0x0002;
        public const int IMR_COMPOSITIONFONT = 0x0003;
        public const int IMR_RECONVERTSTRING = 0x0004;
        public const int IMR_CONFIRMRECONVERTSTRING = 0x0005;
        public const int IMR_QUERYCHARPOSITION = 0x0006;
        public const int IMR_DOCUMENTFEED = 0x0007;

        // parameter of ImmGetCompositionString
        public const int GCS_COMPREADSTR = 0x0001;
        public const int GCS_COMPREADATTR = 0x0002;
        public const int GCS_COMPREADCLAUSE = 0x0004;
        public const int GCS_COMPSTR = 0x0008;
        public const int GCS_COMPATTR = 0x0010;
        public const int GCS_COMPCLAUSE = 0x0020;
        public const int GCS_CURSORPOS = 0x0080;
        public const int GCS_DELTASTART = 0x0100;
        public const int GCS_RESULTREADSTR = 0x0200;
        public const int GCS_RESULTREADCLAUSE = 0x0400;
        public const int GCS_RESULTSTR = 0x0800;
        public const int GCS_RESULTCLAUSE = 0x1000;

        public const int GCS_COMP = (GCS_COMPSTR | GCS_COMPATTR | GCS_COMPCLAUSE);
        public const int GCS_COMPREAD = (GCS_COMPREADSTR | GCS_COMPREADATTR | GCS_COMPREADCLAUSE);
        public const int GCS_RESULT = (GCS_RESULTSTR | GCS_RESULTCLAUSE);
        public const int GCS_RESULTREAD = (GCS_RESULTREADSTR | GCS_RESULTREADCLAUSE);

        #endregion Constants

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
