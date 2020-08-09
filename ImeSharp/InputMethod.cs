using System;
using System.Runtime.InteropServices;
using System.Text;
using ImeSharp.Native;

namespace ImeSharp
{
    public static class InputMethod
    {
        private static IntPtr _windowHandle;

        private static IntPtr _prevWndProc;
        private static NativeMethods.WndProcDelegate _wndProcDelegate;

        // If the system is IMM enabled, this is true.
        private static bool _immEnabled = SafeSystemMetrics.IsImmEnabled;

        private static SecurityCriticalDataClass<IntPtr> _defaultImc;
        private static IntPtr DefaultImc
        {
            get
            {
                if (_defaultImc == null)
                {
                    IntPtr himc = NativeMethods.ImmCreateContext();

                    // Store the default imc to _defaultImc.
                    _defaultImc = new SecurityCriticalDataClass<IntPtr>(himc);
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

        /// <summary>
        /// Initialize InputMethod with a Window Handle.
        /// </summary>
        public static void Initialize(IntPtr windowHandle)
        {
            if (_windowHandle != IntPtr.Zero)
                throw new InvalidOperationException("InputMethod can only be initialized once!");

            _windowHandle = windowHandle;
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
        
        public static void SetConversionMode(NativeMethods.IMEConversionMode eStatus)
        {
            int iConversionMode = 0;
            int iSentence = 0;
            NativeMethods.ImmGetConversionStatus(DefaultImc, ref iConversionMode, ref iSentence);
            NativeMethods.ImmSetConversionStatus(DefaultImc, (int)eStatus, iSentence);
            NativeMethods.ImmReleaseContext(_windowHandle, DefaultImc);
        }
        
        public static NativeMethods.IMEConversionMode GetConversionMode()
        {
            int conversionMode = 0;
            int sentence = 0;
            NativeMethods.ImmGetConversionStatus(DefaultImc, ref conversionMode, ref sentence);
            NativeMethods.ImmReleaseContext(_windowHandle, DefaultImc);
            return (NativeMethods.IMEConversionMode)conversionMode;
        }

        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            //TODO:
            switch (msg)
            {
                case NativeMethods.WM_IME_SETCONTEXT:
                    
                    if (_immEnabled)
                    {
                        lParam = DisableOSCompositionWindow(lParam);    
                    }
                    
                    break;
                case NativeMethods.WM_CHAR:
                    break;
                case NativeMethods.WM_KEYDOWN:
                    break;
                case NativeMethods.WM_KEYUP:
                    break;
                case NativeMethods.WM_IME_NOTIFY:
                    
                    switch (NativeMethods.IntPtrToInt32(wParam))
                    {
                        case NativeMethods.IMN_CHANGECANDIDATE:
                            break;
                    }
                    break;
                case NativeMethods.WM_CLOSE:
                    //if the context is created manually, it also should be destroyed manually
                    NativeMethods.ImmDestroyContext(DefaultImc);
                    break;
                
                default:
                    break;
            }

            IntPtr returnCode = NativeMethods.CallWindowProc(_prevWndProc, hWnd, msg, wParam, lParam);
            return returnCode;
        }
        
        /// <summary>
        /// Retrieves the candidate list.
        /// </summary>
        private static void RetrieveCandidateList()
        {
            uint size = NativeMethods.ImmGetCandidateList(DefaultImc, 0, IntPtr.Zero, 0);
            if (size <= 0)
            {
                return;
            }

            var candidateList = new NativeMethods.CandidateList();
            var ptr = Marshal.AllocHGlobal((int)size);
            NativeMethods.ImmGetCandidateList(DefaultImc, 0, ptr, size);
            Marshal.PtrToStructure(ptr, candidateList);
            
            var buffer = new byte[size];
            
            Marshal.Copy(ptr, buffer, 0, (int)size);
            Marshal.FreeHGlobal(ptr);

            const int iMembers = 6;
            var candidates = new string[candidateList.dwCount];
            for (var index = 0; index < candidateList.dwCount; index++)
            {
                var ithOffset = BitConverter.ToUInt32(buffer, (iMembers + index) * (sizeof(uint)));
                int strLen;
                if (index == candidateList.dwCount - 1)
                {
                    strLen = (int)(candidateList.dwSize - ithOffset - 2);
                }
                else
                {
                    var offset = BitConverter.ToUInt32(buffer, (iMembers + index + 1) * (sizeof(uint)));
                    strLen = (int)(offset - ithOffset - 2);
                }

                string ithStr = Encoding.Unicode.GetString(buffer, (int)ithOffset, strLen);
                candidates[index] = ithStr;
            }
            //TODO event to emit the candidate list(string[]) and selection (candidateList.dwSelection) or cache?
        }
        
        /// <summary>
        /// Turn off default IME composition window.
        /// </summary>
        /// <param name="lParam">lParam from WM_IME_SETCONTEXT</param>
        /// <returns>Modified lParam that has to be used within CallWindowProc </returns>
        private static IntPtr DisableOSCompositionWindow(IntPtr lParam)
        {
            int lParamValue = NativeMethods.IntPtrToInt32(lParam);
            lParamValue &= ~NativeMethods.ISC_SHOWUICANDIDATEWINDOW;
            lParam = (IntPtr)lParamValue;
            return lParam;
        }

    }
}
