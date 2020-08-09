using System;
using System.Runtime.InteropServices;
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

        public static bool ShowTextServiceUI;

        private static ImmCompositionString _immCompositionString;
        private static ImmCompositionInt _immCursorPosition;
        public static string[] Candidates { get; private set; }

        /// <summary>
        /// First candidate index of current page
        /// </summary>
        public static uint CandidatesPageStart { get; private set; }

        /// <summary>
        /// How many candidates should display per page
        /// </summary>
        public static uint CandidatesPageSize { get; private set; }

        /// <summary>
        /// The selected canddiate index
        /// </summary>
        public static uint CandidatesSelection { get; private set; }

        private static bool _compositionEnded;

        public static bool IsTextInputActive { get; private set; }
        public static event EventHandler<TextCompositionEventArgs> TextComposition;
        public static event EventHandler<TextInputEventArgs> TextInput;

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

            _immCompositionString = new ImmCompositionString(DefaultImc, NativeMethods.GCS_COMPSTR);
            _immCursorPosition = new ImmCompositionInt(DefaultImc, NativeMethods.GCS_CURSORPOS);
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

        public static void EnableOrDisableInputMethodIMM32(bool bEnabled)
        {
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

        public static void EnableOrDisableInputMethodTSF(bool bEnabled)
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

        public static void EnableOrDisableInputMethod(bool bEnabled)
        {
            IsTextInputActive = bEnabled;

            EnableOrDisableInputMethodTSF(bEnabled);
            EnableOrDisableInputMethodIMM32(bEnabled);
        }

        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            //TODO:
            switch (msg)
            {
                case NativeMethods.WM_KEYDOWN:
                    break;
                case NativeMethods.WM_KEYUP:
                    break;
                case NativeMethods.WM_IME_SETCONTEXT:
                    if ((int)wParam == 1)
                    {
                        if (!ShowTextServiceUI)
                            lParam = IntPtr.Zero;
                    }
                    break;
                case NativeMethods.WM_IME_NOTIFY:
                    IMENotify((int)wParam);
                    if (!ShowTextServiceUI)
                        return IntPtr.Zero;
                    break;
                case NativeMethods.WM_IME_STARTCOMPOSITION:
                    _compositionEnded = false;
                    IMEStartComposion((int)lParam);
                    if (!ShowTextServiceUI)
                        return IntPtr.Zero;
                    break;
                case NativeMethods.WM_IME_COMPOSITION:
                    IMEComposition((int)lParam);
                    break;
                case NativeMethods.WM_IME_ENDCOMPOSITION:
                    _compositionEnded = true;
                    IMEEndComposition((int)lParam);
                    if (!ShowTextServiceUI)
                        return IntPtr.Zero;
                    break;
                case NativeMethods.WM_CHAR:
                    if (IsTextInputActive)
                    {
                        Console.WriteLine("WM_CHAR: {0}", (char)wParam.ToInt32());
                        CharEvent((int)wParam);
                    }
                    break;
                default:
                    break;
            }

            return NativeMethods.CallWindowProc(_prevWndProc, hWnd, msg, wParam, lParam);
        }
        private static void IMEStartComposion(int lParam)
        {
            ClearComposition();
        }

        private static void IMEComposition(int lParam)
        {
            if (_immCompositionString.Update(lParam))
            {
                _immCursorPosition.Update();

                if (TextComposition != null)
                    TextComposition.Invoke(null, new TextCompositionEventArgs(
                        _immCompositionString.ToString(), _immCursorPosition.Value,
                        Candidates, CandidatesPageStart, CandidatesPageSize, CandidatesSelection));
            }
        }

        private static void ClearComposition()
        {
            _immCompositionString.Clear();
        }

        private static void IMEEndComposition(int lParam)
        {
            ClearComposition();
            IMECloseCandidate();

            if (TextComposition != null)
                TextComposition.Invoke(null, new TextCompositionEventArgs(null, 0));
        }

        private static void IMENotify(int WParam)
        {
            switch (WParam)
            {
                case NativeMethods.IMN_OPENCANDIDATE:
                case NativeMethods.IMN_CHANGECANDIDATE:
                    IMEChangeCandidate();
                    break;
                case NativeMethods.IMN_CLOSECANDIDATE:
                    IMECloseCandidate();
                    break;
                default:
                    break;
            }
        }

        private static void IMEChangeCandidate()
        {
            if (_compositionEnded)
                IMECloseCandidate();
            else
                UpdateCandidates();

            if (TextComposition != null)
                TextComposition.Invoke(null, new TextCompositionEventArgs(
                    _immCompositionString.ToString(), _immCursorPosition.Value,
                    Candidates, CandidatesPageStart, CandidatesPageSize, CandidatesSelection));
        }

        private static void UpdateCandidates()
        {
            uint length = NativeMethods.ImmGetCandidateList(DefaultImc, 0, IntPtr.Zero, 0);
            if (length > 0)
            {
                IntPtr pointer = Marshal.AllocHGlobal((int)length);
                length = NativeMethods.ImmGetCandidateList(DefaultImc, 0, pointer, length);
                NativeMethods.CandidateList cList = (NativeMethods.CandidateList)Marshal.PtrToStructure(pointer, typeof(NativeMethods.CandidateList));

                CandidatesSelection = cList.dwSelection;
                CandidatesPageStart = cList.dwPageStart;
                CandidatesPageSize = cList.dwPageSize;

                if (cList.dwCount > 1)
                {
                    Candidates = new string[cList.dwCount];
                    for (int i = 0; i < cList.dwCount; i++)
                    {
                        int sOffset = Marshal.ReadInt32(pointer, 24 + 4 * i);
                        Candidates[i] = Marshal.PtrToStringUni(pointer + sOffset);
                    }
                }

                Marshal.FreeHGlobal(pointer);
            }
            else
                IMECloseCandidate();
        }

        private static void IMECloseCandidate()
        {
            CandidatesSelection = CandidatesPageStart = CandidatesPageSize = 0;
            Candidates = null;
        }

        private static void CharEvent(int wParam)
        {
            if (TextInput != null)
                TextInput.Invoke(null, new TextInputEventArgs((char)wParam));
        }

    }
}
