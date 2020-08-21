using System;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Diagnostics;
using ImeSharp.Native;

namespace ImeSharp
{
    internal class Imm32Manager
    {

        // If the system is IMM enabled, this is true.
        private static bool _immEnabled = SafeSystemMetrics.IsImmEnabled;

        public static bool ImmEnabled { get { return _immEnabled; } }

        static Imm32Manager()
        {
            SetCurrentCulture();
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

        private static CultureInfo _currentCulture;

        internal static void SetCurrentCulture()
        {
            var hkl =  NativeMethods.GetKeyboardLayout(0);
            var keyboardLayout = NativeMethods.IntPtrToInt32(hkl) & 0xFFFF;
            _currentCulture = new CultureInfo(keyboardLayout);
        }

        private IntPtr _windowHandle;

        private IntPtr _defaultImc;
        private IntPtr DefaultImc
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

        private static ImmCompositionStringHandler _compositionStringHandler;
        private static ImmCompositionIntHandler _compositionCursorHandler;

        public Imm32Manager(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;

            _compositionStringHandler = new ImmCompositionStringHandler(DefaultImc, NativeMethods.GCS_COMPSTR);
            _compositionCursorHandler = new ImmCompositionIntHandler(DefaultImc, NativeMethods.GCS_CURSORPOS);
        }

        public static Imm32Manager Current
        {
            get
            {
                var defaultImm32Manager = InputMethod.DefaultImm32Manager;

                if (defaultImm32Manager == null)
                {
                    defaultImm32Manager = new Imm32Manager(InputMethod.WindowHandle);
                    InputMethod.DefaultImm32Manager = defaultImm32Manager;
                }

                return defaultImm32Manager;
            }
        }

        public void Enable()
        {
            if (DefaultImc != IntPtr.Zero)
            {
                // Create a temporary system caret
                NativeMethods.CreateCaret(_windowHandle, IntPtr.Zero, 2, 10);
                NativeMethods.ImmAssociateContext(_windowHandle, _defaultImc);
            }
        }

        public void Disable()
        {
            NativeMethods.ImmAssociateContext(_windowHandle, IntPtr.Zero);
            NativeMethods.DestroyCaret();
        }

        const int kCaretMargin = 1;

        const string ChineseLangTag = "zh-CN";
        const string JapaneseLangTag = "ja-JP";
        const string KoreaLangTag = "ko-KR";

        // Set candidate window position.
        // Borrowed from https://github.com/chromium/chromium/blob/master/ui/base/ime/win/imm32_manager.cc
        public void SetCandidateWindow(NativeMethods.RECT caretRect)
        {
            int x = caretRect.left;
            int y = caretRect.top;

            if (_currentCulture.IetfLanguageTag == ChineseLangTag)
            {
                // Chinese IMEs ignore function calls to ::ImmSetCandidateWindow()
                // when a user disables TSF (Text Service Framework) and CUAS (Cicero
                // Unaware Application Support).
                // On the other hand, when a user enables TSF and CUAS, Chinese IMEs
                // ignore the position of the current system caret and uses the
                // parameters given to ::ImmSetCandidateWindow() with its 'dwStyle'
                // parameter CFS_CANDIDATEPOS.
                // Therefore, we do not only call ::ImmSetCandidateWindow() but also
                // set the positions of the temporary system caret.
                var candidateForm = new NativeMethods.CANDIDATEFORM();
                candidateForm.dwStyle = NativeMethods.CFS_CANDIDATEPOS;
                candidateForm.ptCurrentPos.x = x;
                candidateForm.ptCurrentPos.y = y;
                NativeMethods.ImmSetCandidateWindow(_defaultImc, ref candidateForm);
            }

            if (_currentCulture.IetfLanguageTag == JapaneseLangTag)
                NativeMethods.SetCaretPos(x, caretRect.bottom);
            else
                NativeMethods.SetCaretPos(x, y);

            // Set composition window position also to ensure move the candidate window position.
            var compositionForm = new NativeMethods.COMPOSITIONFORM();
            compositionForm.dwStyle = NativeMethods.CFS_POINT;
            compositionForm.ptCurrentPos.x = x;
            compositionForm.ptCurrentPos.y = y;
            NativeMethods.ImmSetCompositionWindow(_defaultImc, ref compositionForm);

            if (_currentCulture.IetfLanguageTag == KoreaLangTag)
            {
                // Chinese IMEs and Japanese IMEs require the upper-left corner of
                // the caret to move the position of their candidate windows.
                // On the other hand, Korean IMEs require the lower-left corner of the
                // caret to move their candidate windows.
                y += kCaretMargin;
            }

            // Need to return here since some Chinese IMEs would stuck if set
            // candidate window position with CFS_EXCLUDE style.
            if (_currentCulture.IetfLanguageTag == ChineseLangTag) return;

            // Japanese IMEs and Korean IMEs also use the rectangle given to
            // ::ImmSetCandidateWindow() with its 'dwStyle' parameter CFS_EXCLUDE
            // to move their candidate windows when a user disables TSF and CUAS.
            // Therefore, we also set this parameter here.
            var excludeRectangle = new NativeMethods.CANDIDATEFORM();
            compositionForm.dwStyle = NativeMethods.CFS_EXCLUDE;
            compositionForm.ptCurrentPos.x = x;
            compositionForm.ptCurrentPos.y = y;
            compositionForm.rcArea.left = x;
            compositionForm.rcArea.top = y;
            compositionForm.rcArea.right = caretRect.right;
            compositionForm.rcArea.bottom = caretRect.bottom;
            NativeMethods.ImmSetCandidateWindow(_defaultImc, ref excludeRectangle);
        }

        internal bool ProcessMessage(IntPtr hWnd, uint msg, ref IntPtr wParam, ref IntPtr lParam)
        {
            switch (msg)
            {
                case NativeMethods.WM_INPUTLANGCHANGE:
                    SetCurrentCulture();
                    break;
                case NativeMethods.WM_IME_SETCONTEXT:
                    if (wParam.ToInt32() == 1 && InputMethod.Enabled)
                    {
                        // Must re-associate ime context or things won't work.
                        NativeMethods.ImmAssociateContext(_windowHandle, DefaultImc);

                        if (!NativeMethods.ImmGetOpenStatus(DefaultImc))
                            NativeMethods.ImmSetOpenStatus(DefaultImc, true);

                        var lParam64 = lParam.ToInt64();
                        if (!InputMethod.ShowOSImeWindow)
                            lParam64 &= ~NativeMethods.ISC_SHOWUICANDIDATEWINDOW;
                        else
                            lParam64 &= ~NativeMethods.ISC_SHOWUICOMPOSITIONWINDOW;
                        lParam = (IntPtr)(int)lParam64;
                    }
                    else
                        NativeMethods.ImmSetOpenStatus(DefaultImc, false);
                    break;
                case NativeMethods.WM_IME_NOTIFY:
                    IMENotify(wParam.ToInt32());
                    if (!InputMethod.ShowOSImeWindow)
                        return true;
                    break;
                case NativeMethods.WM_IME_STARTCOMPOSITION:
                    //Debug.WriteLine("NativeMethods.WM_IME_STARTCOMPOSITION");
                    IMEStartComposion(lParam.ToInt32());
                    // Force to not show composition window, `lParam64 &= ~ISC_SHOWUICOMPOSITIONWINDOW` don't work sometime.
                    return true;
                case NativeMethods.WM_IME_COMPOSITION:
                    //Debug.WriteLine("NativeMethods.WM_IME_COMPOSITION");
                    IMEComposition(lParam.ToInt32());
                    break;
                case NativeMethods.WM_IME_ENDCOMPOSITION:
                    //Debug.WriteLine("NativeMethods.WM_IME_ENDCOMPOSITION");
                    IMEEndComposition(lParam.ToInt32());
                    if (!InputMethod.ShowOSImeWindow)
                        return true;
                    break;
                case NativeMethods.WM_CHAR:
                    {
                        if (InputMethod.Enabled)
                            InputMethod.OnTextInput(this, (char)wParam.ToInt32());

                        break;
                    }
            }

            return false;
        }

        private void IMENotify(int WParam)
        {
            switch (WParam)
            {
                case NativeMethods.IMN_OPENCANDIDATE:
                case NativeMethods.IMN_CHANGECANDIDATE:
                    IMEChangeCandidate();
                    break;
                case NativeMethods.IMN_CLOSECANDIDATE:
                    InputMethod.ClearCandidates();
                    break;
                default:
                    break;
            }
        }

        private void IMEChangeCandidate()
        {
            if (TextServicesLoader.ServicesInstalled) // TSF is enabled
            {
                if (!TextStore.Current.SupportUIElement) // But active IME not support UIElement
                    UpdateCandidates(); // We have to fetch candidate list here.

                return;
            }

            // Normal candidate list fetch in IMM32
            UpdateCandidates();
            // Send event on candidate updates
            InputMethod.OnTextComposition(this, new IMEString(_compositionStringHandler.Values, _compositionStringHandler.Count), _compositionCursorHandler.Value);
        }

        private void UpdateCandidates()
        {
            uint length = NativeMethods.ImmGetCandidateList(DefaultImc, 0, IntPtr.Zero, 0);
            if (length > 0)
            {
                IntPtr pointer = Marshal.AllocHGlobal((int)length);
                length = NativeMethods.ImmGetCandidateList(DefaultImc, 0, pointer, length);
                NativeMethods.CANDIDATELIST cList = (NativeMethods.CANDIDATELIST)Marshal.PtrToStructure(pointer, typeof(NativeMethods.CANDIDATELIST));

                var selection = (int)cList.dwSelection;
                var pageStart = (int)cList.dwPageStart;
                var pageSize = (int)cList.dwPageSize;

                IMEString[] candidates = new IMEString[pageSize];

                int i, j;
                for (i = pageStart, j = 0; i < cList.dwCount && j < pageSize; i++, j++)
                {
                    int sOffset = Marshal.ReadInt32(pointer, 24 + 4 * i);
                    candidates[j] = new IMEString(pointer + sOffset);
                }

                //Debug.WriteLine("IMM========IMM");
                //Debug.WriteLine("pageStart: {0}, pageSize: {1}, selection: {2}, candidates:", pageStart, pageSize, selection);
                //for (int k = 0; k < candidates.Length; k++)
                //    Debug.WriteLine("  {2}{0}.{1}", k + 1, candidates[k], k == selection ? "*" : "");
                //Debug.WriteLine("IMM++++++++IMM");

                InputMethod.CandidatePageStart = pageStart;
                InputMethod.CandidatePageSize = pageSize;
                InputMethod.CandidateSelection = selection;
                InputMethod.CandidateList = candidates;

                Marshal.FreeHGlobal(pointer);
            }
        }

        private void ClearComposition()
        {
            _compositionStringHandler.Clear();
        }

        private void IMEStartComposion(int lParam)
        {
            InputMethod.OnTextCompositionStarted(this);
            ClearComposition();
        }

        private void IMEComposition(int lParam)
        {
            if (_compositionStringHandler.Update(lParam))
            {
                _compositionCursorHandler.Update();

                InputMethod.OnTextComposition(this, new IMEString(_compositionStringHandler.Values, _compositionStringHandler.Count), _compositionCursorHandler.Value);
            }
        }

        private void IMEEndComposition(int lParam)
        {
            InputMethod.ClearCandidates();
            ClearComposition();

            InputMethod.OnTextCompositionEnded(this);
        }
    }
}
