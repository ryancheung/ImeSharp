using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ImeSharp.Native;

namespace ImeSharp
{
    public static class InputMethod
    {
        private static IntPtr _windowHandle;
        public static IntPtr WindowHandle { get { return _windowHandle; } }

        private static IntPtr _prevWndProc;
        private static NativeMethods.WndProcDelegate _wndProcDelegate;

        // If the system is IMM enabled, this is true.
        private static bool _immEnabled = SafeSystemMetrics.IsImmEnabled;

        private static IntPtr _defaultImc;
        private static IntPtr DefaultImc
        {
            get
            {
                if (_defaultImc == IntPtr.Zero)
                {
                    IntPtr himc = NativeMethods.ImmCreateContext();

                    // Store the default imc to _defaultImc.
                    _defaultImc = himc;
                }
                return _defaultImc;
            }
        }

        private static TextServicesContext _textServicesContext;
        public static TextServicesContext TextServicesContext
        {
            get { return _textServicesContext; }
            set { _textServicesContext = value; }
        }

        private static TextStore _defaultTextStore;
        public static TextStore DefaultTextStore
        {
            get { return _defaultTextStore; }
            set { _defaultTextStore = value; }
        }

        private static bool _enabled;
        public static bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled == value) return;

                _enabled = value;

                EnableOrDisableInputMethod(_enabled);
            }
        }

        private static bool _showOSImeWindow;
        public static bool ShowOSImeWindow { get { return _showOSImeWindow; } }

        /// <summary>
        /// Initialize InputMethod with a Window Handle.
        /// </summary>
        public static void Initialize(IntPtr windowHandle, bool showOSImeWindow = true)
        {
            if (_windowHandle != IntPtr.Zero)
                throw new InvalidOperationException("InputMethod can only be initialized once!");

            _windowHandle = windowHandle;
            _showOSImeWindow = showOSImeWindow;

            _wndProcDelegate = new NativeMethods.WndProcDelegate(WndProc);
            _prevWndProc = (IntPtr)NativeMethods.SetWindowLongPtr(_windowHandle, NativeMethods.GWL_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
        }

        /// <summary>
        /// return true if the current keyboard layout is a real IMM32-IME.
        /// </summary>
        public static bool IsImm32ImeCurrent()
        {
            if (!_immEnabled)
                return false;

            IntPtr hkl = NativeMethods.GetKeyboardLayout(0);

            return IsImm32Ime(hkl);
        }

        /// <summary>
        /// return true if the keyboard layout is a real IMM32-IME.
        /// </summary>
        public static bool IsImm32Ime(IntPtr hkl)
        {
            if (hkl == IntPtr.Zero)
                return false;

            return ((NativeMethods.IntPtrToInt32(hkl) & 0xf0000000) == 0xe0000000);
        }

        /// <summary>
        /// return true if current OS version is Windows 7 or below.
        /// </summary>
        public static bool IsWindows7OrBelow()
        {
            if (Environment.OSVersion.Version.Major <= 5)
                return true;

            if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor <= 1)
                return true;

            return false;
        }

        private static void EnableOrDisableInputMethodTSF(bool bEnabled)
        {
            // InputMethod enable/disabled status was changed on the current focus Element.
            if (TextServicesLoader.ServicesInstalled)
            {
                if (bEnabled)
                {
                    // Enabled. SetFocus to the default text store.
                    TextServicesContext.Current.SetFocusOnDefaultTextStore();
                }
                else
                {
                    // Disabled. SetFocus to the empty dim.
                    TextServicesContext.Current.SetFocusOnEmptyDim();
                }
            }
        }

        private static void EnableOrDisableInputMethodIMM32(bool bEnabled)
        {
            // Under IMM32 enabled system, we associate default hIMC or null hIMC.
            //
            if (_immEnabled)
            {
                if (bEnabled)
                {
                    //
                    // Enabled. Use the default hIMC.
                    //
                    if (DefaultImc != IntPtr.Zero)
                    {
                        NativeMethods.ImmAssociateContext(_windowHandle, _defaultImc);
                    }
                }
                else
                {
                    //
                    // Disable. Use null hIMC.
                    //
                    NativeMethods.ImmAssociateContext(_windowHandle, IntPtr.Zero);
                }
            }
        }

        private static void EnableOrDisableInputMethod(bool bEnabled)
        {
            EnableOrDisableInputMethodTSF(bEnabled);
            EnableOrDisableInputMethodIMM32(bEnabled);
        }

        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            //TODO:
            switch (msg)
            {
                case NativeMethods.WM_IME_SETCONTEXT:
                    // Must re-associate ime context or things won't work.
                    if (wParam.ToInt32() == 1 && _enabled)
                        NativeMethods.ImmAssociateContext(_windowHandle, DefaultImc);
                    break;
                case NativeMethods.WM_IME_NOTIFY:
                    IMENotify(wParam.ToInt32());
                    if (!ShowOSImeWindow)
                        return IntPtr.Zero;
                    Debug.WriteLine("NativeMethods.WM_IME_NOTIFY");
                    break;
                case NativeMethods.WM_IME_STARTCOMPOSITION:
                    Debug.WriteLine("NativeMethods.WM_IME_STARTCOMPOSITION");
                    break;
                case NativeMethods.WM_IME_COMPOSITION:
                    Debug.WriteLine("NativeMethods.WM_IME_COMPOSITION");
                    break;
                case NativeMethods.WM_IME_ENDCOMPOSITION:
                    Debug.WriteLine("NativeMethods.WM_IME_ENDCOMPOSITION");
                    break;
                case NativeMethods.WM_CHAR:
                    if (_enabled)
                    {
                        Debug.WriteLine("WM_CHAR: {0}", (char)wParam.ToInt32());
                    }
                    break;
                case NativeMethods.WM_KEYDOWN:
                    break;
                case NativeMethods.WM_KEYUP:
                    break;
                default:
                    break;
            }

            return NativeMethods.CallWindowProc(_prevWndProc, hWnd, msg, wParam, lParam);
        }

        private static void IMENotify(int WParam)
        {
            switch (WParam)
            {
                case NativeMethods.IMN_OPENCANDIDATE:
                case NativeMethods.IMN_CHANGECANDIDATE:
                    Debug.WriteLine("NativeMethods.IMN_CHANGECANDIDATE");
                    IMEChangeCandidate();
                    break;
                case NativeMethods.IMN_CLOSECANDIDATE:
                    Debug.WriteLine("NativeMethods.IMN_CLOSECANDIDATE");
                    //IMECloseCandidate();
                    break;
                default:
                    break;
            }
        }

        private static void IMEChangeCandidate()
        {
            UpdateCandidates();
        }

        private static void UpdateCandidates()
        {
            uint length = NativeMethods.ImmGetCandidateList(DefaultImc, 0, IntPtr.Zero, 0);
            if (length > 0)
            {
                IntPtr pointer = Marshal.AllocHGlobal((int)length);
                length = NativeMethods.ImmGetCandidateList(DefaultImc, 0, pointer, length);
                NativeMethods.CandidateList cList = (NativeMethods.CandidateList)Marshal.PtrToStructure(pointer, typeof(NativeMethods.CandidateList));

                var selection = cList.dwSelection;
                var pageStart = (int)cList.dwPageStart;
                var pageSize = cList.dwPageSize;

                string[] candidates = new string[pageSize];

                int i, j;
                for (i = pageStart, j = 0; i < cList.dwCount && j < pageSize; i++, j++)
                {
                    int sOffset = Marshal.ReadInt32(pointer, 24 + 4 * i);
                    candidates[j] = Marshal.PtrToStringUni(pointer + sOffset);
                }

                Debug.WriteLine("========");
                Debug.WriteLine("pageStart: {0}, pageSize: {1}, selection: {2}, candidates:", pageStart, pageSize, selection);
                for (int k = 0; k < candidates.Length; k++)
                    Debug.WriteLine("  {2}{0}.{1}", k + 1, candidates[k], k == selection ? "*" : "");
                Debug.WriteLine("++++++++");

                Marshal.FreeHGlobal(pointer);
            }
        }

    }
}
