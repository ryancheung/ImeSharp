using System;
using System.Collections.Generic;
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
        internal static TextServicesContext TextServicesContext
        {
            get { return _textServicesContext; }
            set { _textServicesContext = value; }
        }

        private static TextStore _defaultTextStore;
        internal static TextStore DefaultTextStore
        {
            get { return _defaultTextStore; }
            set { _defaultTextStore = value; }
        }

        private static Imm32Manager _defaultImm32Manager;
        internal static Imm32Manager DefaultImm32Manager
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

        internal static NativeMethods.RECT TextInputRect;

        /// <summary>
        /// Set the position of the candidate window rendered by the OS.
        /// Let the OS render the candidate window by set param "showOSImeWindow" to <c>true</c> on <see cref="Initialize"/>.
        /// </summary>
        public static void SetTextInputRect(int x, int y, int width, int height, InputLanguage inputLanguage = InputLanguage.Chinese)
        {
            TextInputRect.left = x;
            TextInputRect.top = y;
            TextInputRect.right = x + width;
            TextInputRect.bottom = y + height;

            Imm32Manager.Current.SetCandidateWindow(TextInputRect, inputLanguage);
        }

        private static bool _showOSImeWindow;
        public static bool ShowOSImeWindow { get { return _showOSImeWindow; } }

        internal static int CandidatePageStart;
        internal static int CandidatePageSize;
        internal static int CandidateSelection;
        internal static IMEString[] CandidateList;

        internal static void ClearCandidates()
        {
            CandidateList = null;
            CandidatePageStart = 0;
            CandidatePageSize = 0;
            CandidateSelection = 0;
        }

        public static event EventHandler<TextCompositionEventArgs> TextComposition;
        public static event EventHandler<TextInputEventArgs> TextInput;

        public static TextInputCallback TextInputCallback { get; set; }
        public static TextCompositionCallback TextCompositionCallback { get; set; }

        /// <summary>
        /// Initialize InputMethod with a Window Handle.
        /// Let the OS render the candidate window by set <see paramref="showOSImeWindow"/> to <c>true</c>.
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

        internal static void OnTextInput(object sender, char character)
        {
            if (TextInput != null)
                TextInput.Invoke(sender, new TextInputEventArgs(character));

            if (TextInputCallback != null)
                TextInputCallback(character);
        }

        internal static void OnTextComposition(object sender, IMEString compositionText, int cursorPos)
        {
            if (compositionText.Count == 0) // Crash guard
                cursorPos = 0;

            if (cursorPos > compositionText.Count)  // Another crash guard
                cursorPos = compositionText.Count;

            if (TextComposition != null)
            {
                TextComposition.Invoke(sender,
                    new TextCompositionEventArgs(compositionText, cursorPos, CandidateList, CandidatePageStart, CandidatePageSize, CandidateSelection));
            }

            if (TextCompositionCallback != null)
                TextCompositionCallback(compositionText, cursorPos, CandidateList, CandidatePageStart, CandidatePageSize, CandidateSelection);
        }

        internal static void OnTextCompositionEnded(object sender)
        {
            if (TextComposition != null)
                TextComposition.Invoke(sender, new TextCompositionEventArgs(IMEString.Empty, 0));

            if (TextCompositionCallback != null)
                TextCompositionCallback(IMEString.Empty, 0, null, 0, 0, 0);
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

        private static void EnableOrDisableInputMethod(bool bEnabled)
        {
            // InputMethod enable/disabled status was changed on the current focus Element.
            if (TextServicesLoader.ServicesInstalled)
            {
                if (bEnabled)
                    TextServicesContext.Current.SetFocusOnDefaultTextStore();
                else
                    TextServicesContext.Current.SetFocusOnEmptyDim();
            }

            // Under IMM32 enabled system, we associate default hIMC or null hIMC.
            if (Imm32Manager.ImmEnabled)
            {
                if (bEnabled)
                    Imm32Manager.Current.Enable();
                else
                    Imm32Manager.Current.Disable();
            }
        }

        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            var current = Imm32Manager.Current;
            if (current.ProcessMessage(hWnd, msg, ref wParam, ref lParam))
                return IntPtr.Zero;

            return NativeMethods.CallWindowProc(_prevWndProc, hWnd, msg, wParam, lParam);
        }
    }
}
