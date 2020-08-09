using System;
using System.Runtime.InteropServices;
using ImeSharp.Native;

namespace ImeSharp
{
    public static class InputMethod
    {
        private static IntPtr _windowHandle;

        private static IntPtr _prevWndProc;
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private static WndProcDelegate _wndProcDelegate;

        // If the system is IMM enabled, this is true.
        private static bool _immEnabled = SafeSystemMetrics.IsImmEnabled;

        private static SecurityCriticalDataClass<IntPtr> _defaultImc;
        private static IntPtr DefaultImc
        {
            get
            {
                if (_defaultImc == null)
                {
                    IntPtr himc = NativeMethods.ImmGetContext(_windowHandle);

                    // Store the default imc to _defaultImc.
                    _defaultImc = new SecurityCriticalDataClass<IntPtr>(himc);

                    NativeMethods.ImmReleaseContext(_windowHandle, himc);
                }
                return _defaultImc.Value;
            }
        }

        private static TextServicesContext _textServicesContext;
        public static TextServicesContext TextServicesContext
        {
            get { return _textServicesContext; }
            set { _textServicesContext = value; }
        }

        private static DefaultTextStore _defaultTextStore;
        public static DefaultTextStore DefaultTextStore
        {
            get { return _defaultTextStore; }
            set { _defaultTextStore = value; }
        }

        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr returnCode = NativeMethods.CallWindowProc(_prevWndProc, hWnd, msg, wParam, lParam);

            //TODO:
            switch (msg)
            {
                case NativeMethods.WM_CHAR:
                    break;
                case NativeMethods.WM_KEYDOWN:
                    break;
                case NativeMethods.WM_KEYUP:
                    break;
                default:
                    break;
            }

            return returnCode;
        }

        /// <summary>
        /// Initialize InputMethod with a Window Handle.
        /// </summary>
        public static void Initialize(IntPtr windowHandle)
        {
            if (_windowHandle != IntPtr.Zero)
                throw new InvalidOperationException("InputMethod can only be initialized once!");

            _windowHandle = windowHandle;
            _wndProcDelegate = new WndProcDelegate(WndProc);

            var wndProcPtr = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
            _prevWndProc = (IntPtr)NativeMethods.SetWindowLongPtr(_windowHandle, NativeMethods.GWL_WNDPROC, wndProcPtr);
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

        public static void EnableOrDisableInputMethod(bool bEnabled)
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

            //
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
                        NativeMethods.ImmAssociateContext(_windowHandle, _defaultImc.Value);
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

    }
}
