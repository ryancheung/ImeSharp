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
                                       NativeMethods.ITfTransitoryExtensionSink,
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
            _transitoryExtensionSinkCookie = NativeMethods.TF_INVALID_COOKIE;
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
            status = new NativeMethods.TS_STATUS();
        }

        public void GetWnd(out IntPtr hwnd)
        {
            hwnd = IntPtr.Zero;
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

        #region ITfTransitoryExtensionSink

        // Transitory Document has been updated.
        // This is the notification of the changes of the result string and the composition string.
        public void OnTransitoryExtensionUpdated(NativeMethods.ITfContext context, int ecReadOnly, NativeMethods.ITfRange rangeResult, NativeMethods.ITfRange rangeComposition, out bool fDeleteResultRange)
        {

            fDeleteResultRange = true;

            if (rangeResult != null)
            {
                string result = StringFromITfRange(rangeResult, ecReadOnly);
                if (result.Length > 0)
                {
                    if (_composition == null)
                    {
                        // We don't have the composition now and we got the result string.
                        // The result text is result and automatic termination is true.
                        _composition = new DefaultTextStoreTextComposition(result, TextCompositionAutoComplete.On);
                        TextCompositionManager.StartComposition(_composition);

                        // relese composition.
                        _composition = null;
                    }
                    else
                    {
                        // Finalize the composition.
                        _composition.SetCompositionText("");
                        _composition.SetText(result);

                        TextCompositionManager.CompleteComposition(_composition);

                        // relese composition.
                        _composition = null;
                    }
                }
            }

            if (rangeComposition != null)
            {
                string comp = StringFromITfRange(rangeComposition, ecReadOnly);
                if (comp.Length > 0)
                {
                    if (_composition == null)
                    {
                        // Start the new composition.
                        _composition = new DefaultTextStoreTextComposition("", TextCompositionAutoComplete.Off);
                        _composition.SetCompositionText(comp);
                        TextCompositionManager.StartComposition(_composition);
                    }
                    else
                    {
                        // Update the current composition.
                        _composition.SetCompositionText(comp);
                        _composition.SetText("");
                        TextCompositionManager.UpdateComposition(_composition);
                    }
                }
            }
        }

        #endregion ITfTransitoryExtensionSink

        #region ITfUIElementSink

        public int BeginUIElement(int dwUIElementId, [MarshalAs(UnmanagedType.Bool)] ref bool pbShow)
        {
            // Hide OS rendered Candidate list Window
            pbShow = false;

            return NativeMethods.S_OK;
        }

        public int UpdateUIElement(int dwUIElementId)
        {
            return NativeMethods.S_OK;
        }

        public int EndUIElement(int dwUIElementId)
        {
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
            get { return _doc.Value; }

            set { _doc = new SecurityCriticalData<NativeMethods.ITfDocumentMgr>(value); }
        }

        // EditCookie for ITfContext.
        public int EditCookie
        {
            // get { return _editCookie; }
            set { _editCookie = value; }
        }

        public int TransitoryExtensionSinkCookie
        {
            get { return _transitoryExtensionSinkCookie; }
            set { _transitoryExtensionSinkCookie = value; }
        }

        //
        // Get Transitory's DocumentMgr from GUID_COMPARTMENT_TRANSITORYEXTENSION_DOCUMENTMANAGER.
        //
        public NativeMethods.ITfDocumentMgr TransitoryDocumentManager
        {
            get
            {

                NativeMethods.ITfDocumentMgr doc;
                NativeMethods.ITfCompartmentMgr compartmentMgr;
                NativeMethods.ITfCompartment compartment;

                // get compartment manager of the parent doc.
                compartmentMgr = (NativeMethods.ITfCompartmentMgr)DocumentManager;

                // get compartment.
                Guid guid = NativeMethods.GUID_COMPARTMENT_TRANSITORYEXTENSION_DOCUMENTMANAGER;
                compartmentMgr.GetCompartment(ref guid, out compartment);

                // get value of the compartment.
                object obj;
                compartment.GetValue(out obj);
                doc = obj as NativeMethods.ITfDocumentMgr;

                Marshal.ReleaseComObject(compartment);
                return doc;
            }
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
        private SecurityCriticalData<NativeMethods.ITfDocumentMgr> _doc;

        // The edit cookie TSF returns from CreateContext.
        private int _editCookie;

        // The transitory extension sink cookie.
        private int _transitoryExtensionSinkCookie;
    }
}
