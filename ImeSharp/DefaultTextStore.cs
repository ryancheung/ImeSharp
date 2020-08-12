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
                                       NativeMethods.ITfTextEditSink,
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
            _editSinkCookie = NativeMethods.TF_INVALID_COOKIE;
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
            _lastCompositionView = null;
        }

        private NativeMethods.ITfCompositionView _lastCompositionView;

        public void OnUpdateComposition(NativeMethods.ITfCompositionView view, NativeMethods.ITfRange rangeNew)
        {
            _lastCompositionView = view;
        }

        public void OnEndComposition(NativeMethods.ITfCompositionView view)
        {
            _lastCompositionView = null;

            NativeMethods.ITfRange range;
            view.GetRange(out range);
            var str = StringFromITfRange(range, _editSinkCookie);
            Console.WriteLine("result string: {0}", str);
        }

        #endregion ITfContextOwnerCompositionSink

        #region ITfTextEditSink

        public void OnEndEdit(NativeMethods.ITfContext context, int ecReadOnly, NativeMethods.ITfEditRecord editRecord)
        {
            if (_lastCompositionView != null)
            {
                NativeMethods.ITfRange range;
                _lastCompositionView.GetRange(out range);
                var str = StringFromITfRange(range, ecReadOnly);
                Console.WriteLine("composition string: {0}", str);
            }

            // Release editRecord so Finalizer won't do Release() to Cicero's object in GC thread.
            Marshal.ReleaseComObject(editRecord);
        }

        #endregion ITfTextEditSink

        //------------------------------------------------------
        //
        //  Public Methods - ITfUIElementSink
        //
        //------------------------------------------------------

        #region ITfUIElementSink

        public int BeginUIElement(int dwUIElementId, [MarshalAs(UnmanagedType.Bool)] ref bool pbShow)
        {
            // Hide OS rendered Candidate list Window
            pbShow = false;

            OnUIElement(dwUIElementId, true);

            return NativeMethods.S_OK;
        }

        public int UpdateUIElement(int dwUIElementId)
        {
            OnUIElement(dwUIElementId, false);
            return NativeMethods.S_OK;
        }

        public int EndUIElement(int dwUIElementId)
        {
            return NativeMethods.S_OK;
        }

        public const int MaxCandidateCount = 100;

        private void OnUIElement(int uiElementId, bool onStart)
        {
            IntPtr uiElement;

            TextServicesContext.Current.UIElementMgr.GetUIElement(uiElementId, out uiElement);

            NativeMethods.ITfCandidateListUIElementBehavior candList;

            try
            {
                candList = (NativeMethods.ITfCandidateListUIElementBehavior)Marshal.GetObjectForIUnknown(uiElement);
            }
            catch(System.InvalidCastException)
            {
                return;
            }

            int selection = 0;
            int currentPage = 0;
            int count = 0;
            int pageCount = 0;
            int pageStart = 0;
            int pageSize = 0;
            int i, j;

            candList.GetSelection(out selection);
            candList.GetCurrentPage(out currentPage);

            candList.GetCount(out count);
            // Limit max candidate count to 100, or candList.GetString() would crash.
            // Don't know why???
            if (count > MaxCandidateCount)
                count = MaxCandidateCount;

            candList.GetPageIndex(null, 0, out pageCount);

            if (pageCount > 0)
            {
                int[] pageStartIndexes = new int[pageCount];
                candList.GetPageIndex(pageStartIndexes, pageCount, out pageCount);
                pageStart = pageStartIndexes[currentPage];

                if (pageStart >= count - 1)
                {
                    candList.Abort();
                    return;
                }

                if (currentPage < pageCount - 1)
                    pageSize = Math.Min(count, pageStartIndexes[currentPage + 1]) - pageStart;
                else
                    pageSize = count - pageStart;
            }

            selection -= pageStart;

            string[] candidates = new string[pageSize];

            for (i = pageStart, j = 0; i < count && j < pageSize; i++, j++)
            {
                string candidate;
                candList.GetString(i, out candidate);

                candidates[j] = candidate;
            }

            Console.WriteLine("========");
            Console.WriteLine("pageStart: {0}, pageSize: {1}, selection: {2}, currentPage: {3} candidates:", pageStart, pageSize, selection, currentPage);
            for (int k = 0; k < candidates.Length; k++)
                Console.WriteLine("  {2}{0}.{1}", k + 1, candidates[k], k == selection ? "*" : "");
            Console.WriteLine("++++++++");

            Marshal.ReleaseComObject(candList);
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

        public int EditSinkCookie
        {
            get { return _editSinkCookie; }
            set { _editSinkCookie = value; }
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

        private int _editSinkCookie;
    }
}
