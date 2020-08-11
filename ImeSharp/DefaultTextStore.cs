using System;
using System.Runtime.InteropServices;
using ImeSharp.Native;

namespace ImeSharp
{
    // This class has the default text store implementation.
    // DefaultTextStore is a TextStore to be shared by any element of the Dispatcher.
    // When the keyboard focus is on the element, Cicero input goes into this by default.
    // This DefaultTextStore will be used unless an Element (such as TextBox) set
    // the focus on the document manager for its own TextStore.
    public class DefaultTextStore : NativeMethods.ITfContextOwner,
                                       NativeMethods.ITfContextOwnerCompositionSink,
                                       NativeMethods.ITfUIElementSink
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates a DefaultTextStore instance.
        public DefaultTextStore()
        {
            _editCookie = NativeMethods.TF_INVALID_COOKIE;
            _uiElementSinkCookie = NativeMethods.TF_INVALID_COOKIE;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods - ITfContextOwner
        //
        //------------------------------------------------------

        #region ITfContextOwner


        //
        //  ITfContextOwner implementation for Cicero's default text store.
        //  
        // These methods may need to return real values.
        public void GetACPFromPoint(ref NativeMethods.POINT point, NativeMethods.GetPositionFromPointFlags flags, out int position)
        {
            position = 0;
        }

        public void GetTextExt(int start, int end, out NativeMethods.RECT rect, out bool clipped)
        {
            rect = new NativeMethods.RECT();
            clipped = false;
        }

        public void GetScreenExt(out NativeMethods.RECT rect)
        {
            rect = new NativeMethods.RECT();
        }

        public void GetStatus(out NativeMethods.TS_STATUS status)
        {
            // Disable IME by default.
            status = new NativeMethods.TS_STATUS();
        }

        public void GetWnd(out IntPtr hwnd)
        {
            hwnd = InputMethod.WindowHandle;
        }

        public void GetValue(ref Guid guidAttribute, out object varValue)
        {
            varValue = null;
        }

        #endregion ITfContextOwner


        //------------------------------------------------------
        //
        //  Public Methods - ITfContextOwnerCompositionSink
        //
        //------------------------------------------------------

        #region ITfContextOwnerCompositionSink

        public void OnStartComposition(NativeMethods.ITfCompositionView view, out bool ok)
        {
            // Return true in ok to start the composition.
            ok = true;
        }

        public void OnUpdateComposition(NativeMethods.ITfCompositionView view, NativeMethods.ITfRange rangeNew)
        {
        }

        public void OnEndComposition(NativeMethods.ITfCompositionView view)
        {
        }

        #endregion ITfContextOwnerCompositionSink

        //------------------------------------------------------
        //
        //  Public Methods - ITfTransitoryExtensionSink
        //
        //------------------------------------------------------

        #region ITfUIElementSink

        public int BeginUIElement(int dwUIElementId, [MarshalAs(UnmanagedType.Bool)] ref bool pbShow)
        {
            // Hide OS rendered Candidate list Window
            pbShow = true;
            //TODO: Fetch candidate list by ITfCandidateListUIElement interface

            return NativeMethods.S_OK;
        }

        public int UpdateUIElement(int dwUIElementId)
        {
            //TODO: Fetch candidate list by ITfCandidateListUIElement interface
            return NativeMethods.S_OK;
        }

        public int EndUIElement(int dwUIElementId)
        {
            //TODO: Close candidate list
            return NativeMethods.S_OK;
        }

        #endregion ITfUIElementSink

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  public Methods
        //
        //------------------------------------------------------


        public static DefaultTextStore Current
        {
            get
            {
                DefaultTextStore defaultTextStore = InputMethod.DefaultTextStore;
                if (defaultTextStore == null)
                {
                    defaultTextStore = InputMethod.DefaultTextStore = new DefaultTextStore();

                    defaultTextStore.Register();
                }

                return defaultTextStore;
            }
        }

        //------------------------------------------------------
        //
        //  public Properties
        //
        //------------------------------------------------------

        // Pointer to ITfDocumentMgr interface.
        public NativeMethods.ITfDocumentMgr DocumentManager
        {
            get { return _doc; }
            set { _doc = value; }
        }

        // EditCookie for ITfContext.
        public int EditCookie
        {
            // get { return _editCookie; }
            set { _editCookie = value; }
        }

        public int UIElementSinkCookie
        {
            get { return _uiElementSinkCookie; }
            set { _uiElementSinkCookie = value; }
        }

        //------------------------------------------------------
        //
        //  public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        // get the text from ITfRange.
        private string StringFromITfRange(NativeMethods.ITfRange range, int ecReadOnly)
        {
            // Transitory Document uses ther TextStore, which is ACP base.
            NativeMethods.ITfRangeACP rangeacp = (NativeMethods.ITfRangeACP)range;
            int start;
            int count;
            int countRet;
            rangeacp.GetExtent(out start, out count);
            char[] text = new char[count];
            rangeacp.GetText(ecReadOnly, 0, text, count, out countRet);
            return new string(text);
        }

        // This function calls TextServicesContext to create TSF document and start transitory extension.
        private void Register()
        {
            // Create TSF document and advise the sink to it.
            TextServicesContext.Current.RegisterTextStore(this);
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        // The current active composition.
        private TextComposition _composition;

        // The TSF document object.  This is a native resource.
        private NativeMethods.ITfDocumentMgr _doc;

        // The edit cookie TSF returns from CreateContext.
        private int _editCookie;

        // The transitory extension sink cookie.
        private int _uiElementSinkCookie;
    }
}
