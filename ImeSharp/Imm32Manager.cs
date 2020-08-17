using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using ImeSharp.Native;

namespace ImeSharp
{
    public class Imm32Manager
    {

        // If the system is IMM enabled, this is true.
        private static bool _immEnabled = SafeSystemMetrics.IsImmEnabled;

        public static bool ImmEnabled { get { return _immEnabled; } }

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


        public Imm32Manager(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
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
                NativeMethods.ImmAssociateContext(_windowHandle, _defaultImc);
            }
        }

        public void Disable()
        {
            NativeMethods.ImmAssociateContext(_windowHandle, IntPtr.Zero);
        }

        internal bool ProcessMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case NativeMethods.WM_IME_SETCONTEXT:
                    if (wParam.ToInt32() == 1 && InputMethod.Enabled)
                    {
                        // Must re-associate ime context or things won't work.
                        NativeMethods.ImmAssociateContext(_windowHandle, DefaultImc);

                        if (!NativeMethods.ImmGetOpenStatus(DefaultImc))
                            NativeMethods.ImmSetOpenStatus(DefaultImc, true);
                    }
                    else
                        NativeMethods.ImmSetOpenStatus(DefaultImc, false);
                    break;
                case NativeMethods.WM_IME_NOTIFY:
                    IMENotify(wParam.ToInt32());
                    if (!InputMethod.ShowOSImeWindow)
                        return true;
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
                    if (InputMethod.Enabled)
                    {
                        Debug.WriteLine("WM_CHAR: {0}", (char)wParam.ToInt32());
                    }
                    break;
                default:
                    break;
            }

            return false;
        }

        private void IMENotify(int WParam)
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

        private void IMEChangeCandidate()
        {
            if (!TextStore.Current.SupportUIElement)
                UpdateCandidates();
        }

        private void UpdateCandidates()
        {
            uint length = NativeMethods.ImmGetCandidateList(DefaultImc, 0, IntPtr.Zero, 0);
            if (length > 0)
            {
                IntPtr pointer = Marshal.AllocHGlobal((int)length);
                length = NativeMethods.ImmGetCandidateList(DefaultImc, 0, pointer, length);
                NativeMethods.CandidateList cList = (NativeMethods.CandidateList)Marshal.PtrToStructure(pointer, typeof(NativeMethods.CandidateList));

                var selection = (int)cList.dwSelection;
                var pageStart = (int)cList.dwPageStart;
                var pageSize = (int)cList.dwPageSize;

                string[] candidates = new string[pageSize];

                int i, j;
                for (i = pageStart, j = 0; i < cList.dwCount && j < pageSize; i++, j++)
                {
                    int sOffset = Marshal.ReadInt32(pointer, 24 + 4 * i);
                    candidates[j] = Marshal.PtrToStringUni(pointer + sOffset);
                }

                Debug.WriteLine("IMM========IMM");
                Debug.WriteLine("pageStart: {0}, pageSize: {1}, selection: {2}, candidates:", pageStart, pageSize, selection);
                for (int k = 0; k < candidates.Length; k++)
                    Debug.WriteLine("  {2}{0}.{1}", k + 1, candidates[k], k == selection ? "*" : "");
                Debug.WriteLine("IMM++++++++IMM");

                InputMethod.CandidatePageStart = pageStart;
                InputMethod.CandidatePageSize = pageSize;
                InputMethod.CandidateSelection = selection;
                InputMethod.CandidateList = candidates;

                Marshal.FreeHGlobal(pointer);
            }
        }
    }
}