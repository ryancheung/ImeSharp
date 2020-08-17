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

        private static Imm32Manager _defaultImm32Manager;
        public static Imm32Manager DefaultImm32Manager
        {
            get { return _defaultImm32Manager; }
            set { _defaultImm32Manager = value; }
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

        internal static int CandidatePageStart;
        internal static int CandidatePageSize;
        internal static int CandidateSelection;
        internal static string[] CandidateList;

        internal static void ClearCandidates()
        {
            CandidateList = null;
            CandidatePageStart = 0;
            CandidatePageSize = 0;
            CandidateSelection = 0;
        }

        public static event EventHandler<TextCompositionEventArgs> TextComposition;
        public static event EventHandler<TextInputEventArgs> TextInput;

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

        public static void OnTextInput(string resultText)
        {
            if (TextInput != null)
            {
                foreach (var c in resultText)
                    TextInput.Invoke(TextStore.Current, new TextInputEventArgs(c));
            }
        }

        public static void OnTextComposition(string compositionText, int cursorPos)
        {
            if (TextComposition != null)
            {
                TextComposition.Invoke(TextStore.Current,
                    new TextCompositionEventArgs(compositionText, cursorPos, CandidateList, CandidatePageStart, CandidatePageSize, CandidateSelection));
            }
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
            if (Imm32Manager.ImmEnabled)
            {
                if (bEnabled)
                    Imm32Manager.Current.Enable();
                else
                    Imm32Manager.Current.Disable();
            }
        }

        private static void EnableOrDisableInputMethod(bool bEnabled)
        {
            EnableOrDisableInputMethodTSF(bEnabled);
            EnableOrDisableInputMethodIMM32(bEnabled);
        }

        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            var current = Imm32Manager.Current;
            if (current.ProcessMessage(hWnd, msg, wParam, lParam))
                return IntPtr.Zero;

            switch (msg)
            {
                case NativeMethods.WM_CHAR:
                {
                    if (_enabled)
                        TextInput.Invoke(TextStore.Current, new TextInputEventArgs((char)wParam.ToInt32()));

                    break;
                }
            }

            return NativeMethods.CallWindowProc(_prevWndProc, hWnd, msg, wParam, lParam);
        }
    }
}
