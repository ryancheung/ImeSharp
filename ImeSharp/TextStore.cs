using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ImeSharp.Native;

namespace ImeSharp
{
    internal class TextStore : NativeMethods.ITextStoreACP,
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

        // Creates a TextStore instance.
        public TextStore(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;


            _viewCookie = Environment.TickCount;

            _editCookie = NativeMethods.TF_INVALID_COOKIE;
            _uiElementSinkCookie = NativeMethods.TF_INVALID_COOKIE;
            _textEditSinkCookie = NativeMethods.TF_INVALID_COOKIE;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Methods - ITextStoreACP
        //
        //------------------------------------------------------

        #region ITextStoreACP

        public int AdviseSink(ref Guid riid, object obj, NativeMethods.AdviseFlags flags)
        {
            NativeMethods.ITextStoreACPSink sink;

            if (riid != NativeMethods.IID_ITextStoreACPSink)
                throw new COMException("TextStore_CONNECT_E_CANNOTCONNECT");

            sink = obj as NativeMethods.ITextStoreACPSink;
            if (sink == null)
                throw new COMException("TextStore_E_NOINTERFACE");

            // It's legal to replace existing sink.
            if (_sink != null)
                Marshal.ReleaseComObject(_sink);

            _sink = sink;

            return NativeMethods.S_OK;
        }

        public int UnadviseSink(object obj)
        {
            if (obj != _sink)
                throw new COMException("TextStore_CONNECT_E_NOCONNECTION");

            Marshal.ReleaseComObject(_sink);
            _sink = null;

            return NativeMethods.S_OK;
        }

        private bool _LockDocument(NativeMethods.LockFlags dwLockFlags)
        {
            if (_locked)
                return false;

            _locked = true;
            _lockFlags = dwLockFlags;

            return true;
        }


        private void ResetIfRequired()
        {
            if (!_commited)
                return;

            _commited = false;
            int commitEnd = _commitStart + _commitLength;
            _inputBuffer.RemoveRange(_commitStart, _commitLength);

            NativeMethods.TS_TEXTCHANGE textChange;
            textChange.acpStart = _commitStart;
            textChange.acpOldEnd = commitEnd;
            textChange.acpNewEnd = _commitStart;

            _sink.OnTextChange(0, ref textChange);

            _acpStart = _acpEnd = _inputBuffer.Count;
            _sink.OnSelectionChange();
            _commitStart = _commitLength = 0;

            //Debug.WriteLine("TextStore reset!!!");
        }

        private void _UnlockDocument()
        {
            int hr;
            _locked = false;
            _lockFlags = 0;

            ResetIfRequired();

            //if there is a queued lock, grant it
            if (_lockRequestQueue.Count > 0)
            {
                RequestLock(_lockRequestQueue.Dequeue(), out hr);
            }

            //if any layout changes occurred during the lock, notify the manager
            if (_layoutChanged)
            {
                _layoutChanged = false;
                _sink.OnLayoutChange(NativeMethods.TsLayoutCode.TS_LC_CHANGE, _viewCookie);
            }
        }

        private bool _IsLocked(NativeMethods.LockFlags dwLockType)
        {
            return _locked && (_lockFlags & dwLockType) != 0;
        }

        public int RequestLock(NativeMethods.LockFlags dwLockFlags, out int hrSession)
        {
            if (_sink == null)
                throw new COMException("TextStore_NoSink");

            if (dwLockFlags == 0)
                throw new COMException("TextStore_BadLockFlags");

            hrSession = NativeMethods.E_FAIL;

            if (_locked)
            {
                //the document is locked

                if ((dwLockFlags & NativeMethods.LockFlags.TS_LF_SYNC) == NativeMethods.LockFlags.TS_LF_SYNC)
                {
                    /*
                    The caller wants an immediate lock, but this cannot be granted because
                    the document is already locked.
                    */
                    hrSession = NativeMethods.TS_E_SYNCHRONOUS;
                }
                else
                {
                    //the request is asynchronous

                    //Queue the lock request
                    _lockRequestQueue.Enqueue(dwLockFlags);
                    hrSession = NativeMethods.TS_S_ASYNC;
                }

                return NativeMethods.S_OK;
            }

            //lock the document
            _LockDocument(dwLockFlags);

            //call OnLockGranted
            hrSession = _sink.OnLockGranted(dwLockFlags);

            //unlock the document
            _UnlockDocument();

            return NativeMethods.S_OK;
        }

        public int GetStatus(out NativeMethods.TS_STATUS status)
        {
            status.dynamicFlags = 0;

            // This textstore supports Regions.
            status.staticFlags = 0;
            return NativeMethods.S_OK;
        }

        public int QueryInsert(int acpTestStart, int acpTestEnd, int cch, out int acpResultStart, out int acpResultEnd)
        {
            acpResultStart = acpResultEnd = 0;

            //Queryins
            if (acpTestStart > _inputBuffer.Count || acpTestEnd > _inputBuffer.Count)
                return NativeMethods.E_INVALIDARG;

            //Microsoft Pinyin seems does not init the result value, so we set the test value here, in case crash
            acpResultStart = acpTestStart;
            acpResultEnd = acpTestEnd;

            return NativeMethods.S_OK;
        }

        public int GetSelection(int index, int count, ref NativeMethods.TS_SELECTION_ACP selection, out int cFetched)
        {
            cFetched = 0;

            //does the caller have a lock
            if (!_IsLocked(NativeMethods.LockFlags.TS_LF_READ))
            {
                //the caller doesn't have a lock
                return NativeMethods.TS_E_NOLOCK;
            }

            //check the requested index
            if (NativeMethods.TS_DEFAULT_SELECTION == index)
            {
                index = 0;
            }
            else if (index > 1)
            {
                /*
                The index is too high. This app only supports one selection.
                */
                return NativeMethods.E_INVALIDARG;
            }

#if WINDOWS_UAP
            if (!_compositionJustStarted)
            {
                _acpStart = _acpEnd = 0;
            }
#endif

            selection.acpStart = _acpStart;
            selection.acpEnd = _acpEnd;
            selection.style.fInterimChar = _interimChar;

            if (_interimChar)
            {
                /*
                fInterimChar will be set when an intermediate character has been
                set. One example of when this will happen is when an IME is being
                used to enter characters and a character has been set, but the IME
                is still active.
                */
                selection.style.ase = NativeMethods.TsActiveSelEnd.TS_AE_NONE;
            }
            else
            {
                selection.style.ase = _activeSelectionEnd;
            }

            cFetched = 1;

            return NativeMethods.S_OK;
        }

        public int SetSelection(int count, NativeMethods.TS_SELECTION_ACP[] selections)
        {
            //this implementaiton only supports a single selection
            if (count != 1) return NativeMethods.E_INVALIDARG;

            //does the caller have a lock
            if (!_IsLocked(NativeMethods.LockFlags.TS_LF_READWRITE))
            {
                //the caller doesn't have a lock
                return NativeMethods.TS_E_NOLOCK;
            }

            _acpStart = selections[0].acpStart;
            _acpEnd = selections[0].acpEnd;
            _interimChar = selections[0].style.fInterimChar;

#if WINDOWS_UAP
            if (!_compositionJustStarted)
            {
                _acpStart = 0;
                _acpEnd = 0;
            }
#endif

            if (_interimChar)
            {
                /*
                fInterimChar will be set when an intermediate character has been
                set. One example of when this will happen is when an IME is being
                used to enter characters and a character has been set, but the IME
                is still active.
                */
                _activeSelectionEnd = NativeMethods.TsActiveSelEnd.TS_AE_NONE;
            }
            else
            {
                _activeSelectionEnd = selections[0].style.ase;
            }

            //if the selection end is at the start of the selection, reverse the parameters
            int lStart = _acpStart;
            int lEnd = _acpEnd;

            if (NativeMethods.TsActiveSelEnd.TS_AE_START == _activeSelectionEnd)
            {
                lStart = _acpEnd;
                lEnd = _acpStart;
            }

            return NativeMethods.S_OK;
        }

        public int GetText(int acpStart, int acpEnd, char[] pchPlain, int cchPlainReq, out int cchPlainRet,
            NativeMethods.TS_RUNINFO[] rgRunInfo, int cRunInfoReq, out int cRunInfoRet, out int acpNext)
        {
            cchPlainRet = 0;
            cRunInfoRet = 0;
            acpNext = 0;

            //does the caller have a lock
            if (!_IsLocked(NativeMethods.LockFlags.TS_LF_READ))
            {
                //the caller doesn't have a lock
                return NativeMethods.TS_E_NOLOCK;
            }

            bool fDoText = cchPlainReq > 0;
            bool fDoRunInfo = cRunInfoReq > 0;
            int cchTotal;
            int hr = NativeMethods.E_FAIL;

            cchPlainRet = 0;
            acpNext = acpStart;

            cchTotal = _inputBuffer.Count;

            //validate the start pos
            if ((acpStart < 0) || (acpStart > cchTotal))
            {
                hr = NativeMethods.TS_E_INVALIDPOS;
            }
            else
            {
                //are we at the end of the document
                if (acpStart == cchTotal)
                {
                    hr = NativeMethods.S_OK;
                }
                else
                {
                    int cchReq;

                    /*
                    acpEnd will be -1 if all of the text up to the end is being requested.
                    */

                    if (acpEnd >= acpStart)
                    {
                        cchReq = acpEnd - acpStart;
                    }
                    else
                    {
                        cchReq = cchTotal - acpStart;
                    }

                    if (fDoText)
                    {
                        if (cchReq > cchPlainReq)
                        {
                            cchReq = cchPlainReq;
                        }

                        //extract the specified text range
                        if (pchPlain != null && cchPlainReq > 0)
                        {
                            _inputBuffer.CopyTo(acpStart, pchPlain, 0, cchReq);
                        }
                    }

                    //it is possible that only the length of the text is being requested
                    cchPlainRet = cchReq;

                    if (fDoRunInfo)
                    {
                        /*
                        Runs are used to separate text characters from formatting characters.

                        In this example, sequences inside and including the <> are treated as
                        control sequences and are not displayed.

                        Plain text = "Text formatting."
                        Actual text = "Text <B><I>formatting</I></B>."

                        If all of this text were requested, the run sequence would look like this:

                        prgRunInfo[0].type = TS_RT_PLAIN;   //"Text "
                        prgRunInfo[0].uCount = 5;

                        prgRunInfo[1].type = TS_RT_HIDDEN;  //<B><I>
                        prgRunInfo[1].uCount = 6;

                        prgRunInfo[2].type = TS_RT_PLAIN;   //"formatting"
                        prgRunInfo[2].uCount = 10;

                        prgRunInfo[3].type = TS_RT_HIDDEN;  //</B></I>
                        prgRunInfo[3].uCount = 8;

                        prgRunInfo[4].type = TS_RT_PLAIN;   //"."
                        prgRunInfo[4].uCount = 1;

                        TS_RT_OPAQUE is used to indicate characters or character sequences
                        that are in the document, but are used privately by the application
                        and do not map to text.  Runs of text tagged with TS_RT_OPAQUE should
                        NOT be included in the pchPlain or cchPlainOut [out] parameters.
                        */

                        /*
                        This implementation is plain text, so the text only consists of one run.
                        If there were multiple runs, it would be an error to have consecuative runs
                        of the same type.
                        */
                        rgRunInfo[0].type = NativeMethods.TsRunType.TS_RT_PLAIN;
                        rgRunInfo[0].count = cchReq;
                    }

                    acpNext = acpStart + cchReq;

                    hr = NativeMethods.S_OK;
                }
            }

            return hr;
        }

        public int SetText(NativeMethods.SetTextFlags dwFlags, int acpStart, int acpEnd, char[] pchText, int cch, out NativeMethods.TS_TEXTCHANGE change)
        {
            int hr;

#if WINDOWS_UAP
            if (!_compositionJustStarted)
            {
                change = new NativeMethods.TS_TEXTCHANGE();
                return NativeMethods.S_FALSE;
            }
#endif

            /*
            dwFlags can be:
            TS_ST_CORRECTION
            */

            //set the selection to the specified range
            NativeMethods.TS_SELECTION_ACP[] tsa = new NativeMethods.TS_SELECTION_ACP[1];
            tsa[0].acpStart = acpStart;
            tsa[0].acpEnd = acpEnd;
            tsa[0].style.ase = NativeMethods.TsActiveSelEnd.TS_AE_START;
            tsa[0].style.fInterimChar = false;

            hr = SetSelection(1, tsa);

            if (hr == NativeMethods.S_OK)
            {
                int start, end;
                hr = InsertTextAtSelection(NativeMethods.InsertAtSelectionFlags.TS_IAS_NOQUERY, pchText, cch, out start, out end, out change);
            }
            else
            {
                change = new NativeMethods.TS_TEXTCHANGE();
            }

            return hr;
        }

        public int GetFormattedText(int startIndex, int endIndex, out object obj)
        {
            obj = null;
            return NativeMethods.E_NOTIMPL;
        }

        public int GetEmbedded(int index, ref Guid guidService, ref Guid riid, out object obj)
        {
            obj = null;
            return NativeMethods.E_NOTIMPL;
        }

        public int QueryInsertEmbedded(ref Guid guidService, ref int formatEtc, out bool insertable)
        {
            insertable = false;
            return NativeMethods.E_NOTIMPL;
        }

        public int InsertEmbedded(NativeMethods.InsertEmbeddedFlags flags, int startIndex, int endIndex, object obj, out NativeMethods.TS_TEXTCHANGE change)
        {
            change = new NativeMethods.TS_TEXTCHANGE();
            return NativeMethods.E_NOTIMPL;
        }

        public int InsertTextAtSelection(NativeMethods.InsertAtSelectionFlags dwFlags, char[] pchText, int cch, out int pacpStart, out int pacpEnd, out NativeMethods.TS_TEXTCHANGE pChange)
        {
            pacpStart = pacpEnd = 0;
            pChange = new NativeMethods.TS_TEXTCHANGE();

            //does the caller have a lock
            if (!_IsLocked(NativeMethods.LockFlags.TS_LF_READWRITE))
            {
                //the caller doesn't have a lock
                return NativeMethods.TS_E_NOLOCK;
            }

            int acpStart;
            int acpOldEnd;
            int acpNewEnd;

            acpOldEnd = _acpEnd;

            //set the start point after the insertion
            acpStart = _acpStart;

            //set the end point after the insertion
            acpNewEnd = _acpStart + cch;

            if ((dwFlags & NativeMethods.InsertAtSelectionFlags.TS_IAS_QUERYONLY) == NativeMethods.InsertAtSelectionFlags.TS_IAS_QUERYONLY)
            {
                pacpStart = acpStart;
                pacpEnd = acpOldEnd;
                return NativeMethods.S_OK;
            }

            //insert the text
            _inputBuffer.RemoveRange(acpStart, acpOldEnd - acpStart);
            _inputBuffer.InsertRange(acpStart, pchText);

            //set the selection
            _acpStart = acpStart;
            _acpEnd = acpNewEnd;

            if ((dwFlags & NativeMethods.InsertAtSelectionFlags.TS_IAS_NOQUERY) != NativeMethods.InsertAtSelectionFlags.TS_IAS_NOQUERY)
            {
                pacpStart = acpStart;
                pacpEnd = acpNewEnd;
            }

            //set the TS_TEXTCHANGE members
            pChange.acpStart = acpStart;
            pChange.acpOldEnd = acpOldEnd;
            pChange.acpNewEnd = acpNewEnd;

            //defer the layout change notification until the document is unlocked
            _layoutChanged = true;

            return NativeMethods.S_OK;
        }

        public int InsertEmbeddedAtSelection(NativeMethods.InsertAtSelectionFlags flags, object obj, out int startIndex, out int endIndex, out NativeMethods.TS_TEXTCHANGE change)
        {
            startIndex = endIndex = 0;
            change = new NativeMethods.TS_TEXTCHANGE();
            return NativeMethods.E_NOTIMPL;
        }

        public int RequestSupportedAttrs(NativeMethods.AttributeFlags flags, int count, Guid[] filterAttributes)
        {
            return NativeMethods.E_NOTIMPL;
        }

        public int RequestAttrsAtPosition(int index, int count, Guid[] filterAttributes, NativeMethods.AttributeFlags flags)
        {
            return NativeMethods.E_NOTIMPL;
        }


        public int RequestAttrsTransitioningAtPosition(int position, int count, Guid[] filterAttributes, NativeMethods.AttributeFlags flags)
        {
            return NativeMethods.E_NOTIMPL;
        }

        public int FindNextAttrTransition(int startIndex, int haltIndex, int count, Guid[] filterAttributes, NativeMethods.AttributeFlags flags, out int acpNext, out bool found, out int foundOffset)
        {
            acpNext = 0;
            found = false;
            foundOffset = 0;

            return NativeMethods.S_OK;
        }

        public int RetrieveRequestedAttrs(int count, NativeMethods.TS_ATTRVAL[] attributeVals, out int fetched)
        {
            fetched = 0;
            return NativeMethods.E_NOTIMPL;
        }

        public int GetEndACP(out int acp)
        {
            acp = 0;
            //does the caller have a lock
            if (!_IsLocked(NativeMethods.LockFlags.TS_LF_READ))
            {
                //the caller doesn't have a lock
                return NativeMethods.TS_E_NOLOCK;
            }

            acp = _inputBuffer.Count;

            return NativeMethods.S_OK;
        }

        public int GetActiveView(out int viewCookie)
        {
            viewCookie = _viewCookie;
            return NativeMethods.S_OK;
        }

        public int GetACPFromPoint(int viewCookie, ref NativeMethods.POINT tsfPoint, NativeMethods.GetPositionFromPointFlags flags, out int positionCP)
        {
            positionCP = 0;
            return NativeMethods.E_NOTIMPL;
        }

        public int GetTextExt(int viewCookie, int acpStart, int acpEnd, out NativeMethods.RECT rect, out bool clipped)
        {
            clipped = false;
            rect = InputMethod.TextInputRect;

            if (_viewCookie != viewCookie)
                return NativeMethods.E_INVALIDARG;

            //does the caller have a lock
            if (!_IsLocked(NativeMethods.LockFlags.TS_LF_READ))
            {
                //the caller doesn't have a lock
                return NativeMethods.TS_E_NOLOCK;
            }

            //According to Microsoft's doc, an ime should not make empty request,
            //but some ime draw comp text themseleves, when empty req will be make
            //Check empty request
            //if (acpStart == acpEnd) {
            //	return E_INVALIDARG;
            //}

            NativeMethods.MapWindowPoints(_windowHandle, IntPtr.Zero, ref rect, 2);

            return NativeMethods.S_OK;
        }

        public int GetScreenExt(int viewCookie, out NativeMethods.RECT rect)
        {
            rect = new NativeMethods.RECT();

            if (_viewCookie != viewCookie)
                return NativeMethods.E_INVALIDARG;

            NativeMethods.GetWindowRect(_windowHandle, out rect);
            return NativeMethods.S_OK;
        }

        public int GetWnd(int viewCookie, out IntPtr hwnd)
        {
            if (viewCookie != _viewCookie)
            {
                hwnd = IntPtr.Zero;
                return NativeMethods.S_FALSE;
            }

            hwnd = _windowHandle;

            return NativeMethods.S_OK;
        }

        #endregion ITextStoreACP2


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
            _compositionStart = _compositionLength = 0;
            _currentComposition.Clear();
#if WINDOWS_UAP
            _compositionJustStarted = true;
#endif

            InputMethod.OnTextCompositionStarted(this);
        }

        public void OnUpdateComposition(NativeMethods.ITfCompositionView view, NativeMethods.ITfRange rangeNew)
        {
            NativeMethods.ITfRange range;
            view.GetRange(out range);
            var rangeacp = (NativeMethods.ITfRangeACP)range;

            rangeacp.GetExtent(out _compositionStart, out _compositionLength);
        }

        public void OnEndComposition(NativeMethods.ITfCompositionView view)
        {

            NativeMethods.ITfRange range;
            view.GetRange(out range);
            var rangeacp = (NativeMethods.ITfRangeACP)range;

            rangeacp.GetExtent(out _commitStart, out _commitLength);

#if WINDOWS_UAP
            _commited = true;
            for (int i = _commitStart; i < _commitLength; i++)
                InputMethod.OnTextInput(this, _inputBuffer[i]);
#endif

            // Ensure composition string reset
            _compositionStart = _compositionLength = 0;
            _currentComposition.Clear();
#if WINDOWS_UAP
            _compositionJustStarted = false;
#endif

            InputMethod.ClearCandidates();
            InputMethod.OnTextCompositionEnded(this);
        }

        #endregion ITfContextOwnerCompositionSink

        #region ITfTextEditSink

        public int OnEndEdit(NativeMethods.ITfContext context, int ecReadOnly, NativeMethods.ITfEditRecord editRecord)
        {
#if WINDOWS_UAP
            if (!_compositionJustStarted)
                return NativeMethods.S_FALSE;
#endif

            NativeMethods.ITfProperty property;
            Guid composingGuid = NativeMethods.GUID_PROP_COMPOSING;
            context.GetProperty(ref composingGuid, out property);

            NativeMethods.ITfRangeACP rangeACP;
            TextServicesContext.Current.ContextOwnerServices.CreateRange(_compositionStart, _compositionLength, out rangeACP);

#if !WINDOWS_UAP
            NativeMethods.VARIANT val;
            property.GetValue(ecReadOnly, rangeACP as NativeMethods.ITfRange, out val);
            if (val.intVal == 0)
            {
                if (_commitLength == 0 || _inputBuffer.Count == 0)
                {
                    Marshal.ReleaseComObject(editRecord);
                    val.Clear();
                    return NativeMethods.S_OK;
                }

                //Debug.WriteLine("Composition result: {0}", new object[] { new string(_inputBuffer.GetRange(_commitStart, _commitLength).ToArray()) });

                _commited = true;
                for (int i = _commitStart; i < _commitLength; i++)
                    InputMethod.OnTextInput(this, _inputBuffer[i]);
            }
            val.Clear();

            if (_commited)
            {
                Marshal.ReleaseComObject(editRecord);
                return NativeMethods.S_OK;
            }
#endif

            if (_inputBuffer.Count == 0 && _compositionLength > 0) // Composition just ended
            {
                Marshal.ReleaseComObject(editRecord);
                return NativeMethods.S_OK;
            }

            _currentComposition.Clear();
            for (int i = _compositionStart; i < _compositionLength; i++)
                _currentComposition.Add(_inputBuffer[i]);

            InputMethod.OnTextComposition(this, new IMEString(_currentComposition), _acpEnd);

            //var compStr = new string(_currentComposition.ToArray());
            //compStr = compStr.Insert(_acpEnd, "|");
            //Debug.WriteLine("Composition string: {0}, cursor pos: {1}", compStr, _acpEnd);

            // Release editRecord so Finalizer won't do Release() to Cicero's object in GC thread.
            Marshal.ReleaseComObject(editRecord);

            return NativeMethods.S_OK;
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
            pbShow = InputMethod.ShowOSImeWindow;

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
            if (InputMethod.ShowOSImeWindow || !_supportUIElement) return;

            IntPtr uiElement;

            TextServicesContext.Current.UIElementMgr.GetUIElement(uiElementId, out uiElement);

            NativeMethods.ITfCandidateListUIElementBehavior candList;

            try
            {
                candList = (NativeMethods.ITfCandidateListUIElementBehavior)Marshal.GetObjectForIUnknown(uiElement);
            }
            catch (System.InvalidCastException)
            {
                _supportUIElement = false;
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

            IMEString[] candidates = new IMEString[pageSize];

            IntPtr bStrPtr;
            for (i = pageStart, j = 0; i < count && j < pageSize; i++, j++)
            {
                candList.GetString(i, out bStrPtr);
                candidates[j] = new IMEString(bStrPtr);
            }

            //Debug.WriteLine("TSF========TSF");
            //Debug.WriteLine("pageStart: {0}, pageSize: {1}, selection: {2}, currentPage: {3} candidates:", pageStart, pageSize, selection, currentPage);
            //for (int k = 0; k < candidates.Length; k++)
            //    Debug.WriteLine("  {2}{0}.{1}", k + 1, candidates[k], k == selection ? "*" : "");
            //Debug.WriteLine("TSF++++++++TSF");

            InputMethod.CandidatePageStart = pageStart;
            InputMethod.CandidatePageSize = pageSize;
            InputMethod.CandidateSelection = selection;
            InputMethod.CandidateList = candidates;

            if (_currentComposition != null)
                InputMethod.OnTextComposition(this, new IMEString(_currentComposition), _acpEnd);

            Marshal.ReleaseComObject(candList);
        }

        #endregion ITfUIElementSink

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        public static TextStore Current
        {
            get
            {
                TextStore defaultTextStore = InputMethod.DefaultTextStore;
                if (defaultTextStore == null)
                {
                    defaultTextStore = InputMethod.DefaultTextStore = new TextStore(InputMethod.WindowHandle);

                    defaultTextStore.Register();
                }

                return defaultTextStore;
            }
        }

        public NativeMethods.ITfDocumentMgr DocumentManager
        {
            get { return _documentMgr; }
            set { _documentMgr = value; }
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

        public int TextEditSinkCookie
        {
            get { return _textEditSinkCookie; }
            set { _textEditSinkCookie = value; }
        }

        public bool SupportUIElement { get { return _supportUIElement; } }


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        // This function calls TextServicesContext to create TSF document and start transitory extension.
        private void Register()
        {
            // Create TSF document and advise the sink to it.
            TextServicesContext.Current.RegisterTextStore(this);
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        // The TSF document object.  This is a native resource.
        private NativeMethods.ITfDocumentMgr _documentMgr;

        private int _viewCookie;

        // The edit cookie TSF returns from CreateContext.
        private int _editCookie;
        private int _uiElementSinkCookie;
        private int _textEditSinkCookie;

        private NativeMethods.ITextStoreACPSink _sink;
        private IntPtr _windowHandle;
        private int _acpStart;
        private int _acpEnd;
        private bool _interimChar;
        private NativeMethods.TsActiveSelEnd _activeSelectionEnd;
        private List<char> _inputBuffer = new List<char>();

        private bool _locked;
        private NativeMethods.LockFlags _lockFlags;
        private Queue<NativeMethods.LockFlags> _lockRequestQueue = new Queue<NativeMethods.LockFlags>();
        private bool _layoutChanged;

        private List<char> _currentComposition = new List<char>();
        private int _compositionStart;
        private int _compositionLength;
        private int _commitStart;
        private int _commitLength;
        private bool _commited;

        private bool _supportUIElement = true;

#if WINDOWS_UAP
        private bool _compositionJustStarted;
#endif
    }
}
