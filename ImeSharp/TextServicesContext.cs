//
//
// Description: Manages Text Services Framework state.
//
//

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using ImeSharp.Native;

namespace ImeSharp
{
    //------------------------------------------------------
    //
    //  TextServicesContext class
    //
    //------------------------------------------------------

    /// <summary>
    /// This class manages the ITfThreadMgr, EmptyDim and the reference to
    /// the default TextStore.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class TextServicesContext
    {
        public static TextServicesContext Current
        {
            get
            {
                if (InputMethod.TextServicesContext == null)
                    InputMethod.TextServicesContext = new TextServicesContext();

                return InputMethod.TextServicesContext;
            }
        }

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Instantiates a TextServicesContext.
        /// </summary>
        private TextServicesContext()
        {
            Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA, "SetDispatcherThreaad on MTA thread");
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  public Methods
        //
        //------------------------------------------------------

        #region public Methods

        /// <summary>
        /// Releases all unmanaged resources allocated by the
        /// TextServicesContext.
        /// </summary>
        /// <remarks>
        /// if appDomainShutdown == false, this method must be called on the
        /// Dispatcher thread.  Otherwise, the caller is an AppDomain.Shutdown
        /// listener, and is calling from a worker thread.
        /// </remarks>
        public void Uninitialize(bool appDomainShutdown)
        {
            // Unregister DefaultTextStore.
            if (_defaultTextStore != null)
            {
                UnadviseUIElementSink();
                if (_defaultTextStore.DocumentManager != null)
                {
                    _defaultTextStore.DocumentManager.Pop(NativeMethods.PopFlags.TF_POPF_ALL);
                    Marshal.ReleaseComObject(_defaultTextStore.DocumentManager);
                    _defaultTextStore.DocumentManager = null;
                }

                _defaultTextStore = null;
            }

            // Free up any remaining textstores.
            if (_istimactivated == true)
            {
                // Shut down the thread manager when the last TextStore goes away.
                // On XP, if we're called on a worker thread (during AppDomain shutdown)
                // we can't call call any methods on _threadManager.  The problem is
                // that there's no proxy registered for ITfThreadMgr on OS versions
                // previous to Vista.  Not calling Deactivate will leak the IMEs, but
                // in practice (1) they're singletons, so it's not unbounded; and (2)
                // most applications will share the thread with other AppDomains that
                // have a UI, in which case the IME won't be released until the process
                // shuts down in any case.  In theory we could also work around this
                // problem by creating our own XP proxy/stub implementation, which would
                // be added to WPF setup....
                if (!appDomainShutdown || System.Environment.OSVersion.Version.Major >= 6)
                {
                    _threadManager.Deactivate();
                }
                _istimactivated = false;
            }

            // Release the empty dim.
            if (_dimEmpty != null)
            {
                if (_dimEmpty != null)
                {
                    Marshal.ReleaseComObject(_dimEmpty);
                }
                _dimEmpty = null;
            }

            // Release the ThreadManager.
            // We don't do this in UnregisterTextStore because someone may have
            // called get_ThreadManager after the last TextStore was unregistered.
            if (_threadManager != null)
            {
                if (_threadManager != null)
                {
                    Marshal.ReleaseComObject(_threadManager);
                }
                _threadManager = null;
            }
        }

        /// <summary>
        /// Feeds a keystroke to the Text Services Framework, wrapper for
        /// ITfKeystrokeMgr::TestKeyUp/TestKeyDown/KeyUp/KeyDown.
        /// </summary>
        /// <remarks>
        /// Must be called on the main dispatcher thread.
        /// </remarks>
        /// <returns>
        /// true if the keystroke will be eaten by the Text Services Framework,
        /// false otherwise.
        /// Callers should stop further processing of the keystroke on true,
        /// continue otherwise.
        /// </returns>
        public bool Keystroke(int wParam, int lParam, KeyOp op)
        {
            bool fConsume;
            NativeMethods.ITfKeystrokeMgr keystrokeMgr;

            // We delay load cicero until someone creates an ITextStore.
            // Or this thread may not have a ThreadMgr.
            if ((_threadManager == null) || (_threadManager == null))
                return false;

            keystrokeMgr = _threadManager as NativeMethods.ITfKeystrokeMgr;

            switch (op)
            {
                case KeyOp.TestUp:
                    keystrokeMgr.TestKeyUp(wParam, lParam, out fConsume);
                    break;
                case KeyOp.TestDown:
                    keystrokeMgr.TestKeyDown(wParam, lParam, out fConsume);
                    break;
                case KeyOp.Up:
                    keystrokeMgr.KeyUp(wParam, lParam, out fConsume);
                    break;
                case KeyOp.Down:
                    keystrokeMgr.KeyDown(wParam, lParam, out fConsume);
                    break;
                default:
                    fConsume = false;
                    break;
            }

            return fConsume;
        }

        // Called by framework's TextStore class.  This method registers a
        // document with TSF.  The TextServicesContext must maintain this list
        // to ensure all native resources are released after gc or uninitialization.
        public void RegisterTextStore(DefaultTextStore defaultTextStore)
        {
            // We must cache the DefaultTextStore because we'll need it from
            // a worker thread if the AppDomain is torn down before the Dispatcher
            // is shutdown.
            _defaultTextStore = defaultTextStore;

            NativeMethods.ITfThreadMgrEx threadManager = ThreadManager;

            if (threadManager != null)
            {
                NativeMethods.ITfDocumentMgr doc;
                int editCookie = NativeMethods.TF_INVALID_COOKIE;

                // Activate TSF on this thread if this is the first TextStore.
                if (_istimactivated == false)
                {
                    //temp variable created to retrieve the value
                    // which is then stored in the critical data.
                    int clientIdTemp;
                    threadManager.ActivateEx(out clientIdTemp, NativeMethods.TfTMAE.TF_TMAE_UIELEMENTENABLEDONLY);
                    _clientId = clientIdTemp;
                    _istimactivated = true;
                }

                // Create a TSF document.
                threadManager.CreateDocumentMgr(out doc);
                doc.CreateContext(_clientId, 0 /* flags */, _defaultTextStore, out _context, out editCookie);
                doc.Push(_context);

                // Same DocumentManager and EditCookie in _defaultTextStore.
                _defaultTextStore.DocumentManager = doc;
                _defaultTextStore.EditCookie = editCookie;

                // Start the transitory extenstion so we can have Level 1 composition window from Cicero.
                AdviseUIElementSink();
            }
        }


        // Cal ITfThreadMgr.SetFocus() with the dim for the default text store
        public void SetFocusOnDefaultTextStore()
        {
            SetFocusOnDim(DefaultTextStore.Current.DocumentManager);
        }

        // Cal ITfThreadMgr.SetFocus() with the empty dim.
        public void SetFocusOnEmptyDim()
        {
            SetFocusOnDim(EmptyDocumentManager);
        }


        #endregion public Methods

        //------------------------------------------------------
        //
        //  public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// The default ITfThreadMgrEx object.
        /// </summary>
        public NativeMethods.ITfThreadMgrEx ThreadManager
        {
            // The ITfThreadMgr for this thread.
            get
            {
                if (_threadManager == null)
                {
                    _threadManager = TextServicesLoader.Load();
                }

                return _threadManager;
            }
        }

        /// <summary>
        /// Return the created ITfContext object.
        /// </summary>
        public NativeMethods.ITfContext Context
        {
            get { return _context; }
        }

        //------------------------------------------------------
        //
        //  public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  public Enums
        //
        //------------------------------------------------------

        #region public Enums

        /// <summary>
        /// Specifies the type of keystroke operation to perform in the
        /// TextServicesContext.Keystroke method.
        /// </summary>
        public enum KeyOp
        {
            /// <summary>
            /// ITfKeystrokeMgr::TestKeyUp
            /// </summary>
            TestUp,

            /// <summary>
            /// ITfKeystrokeMgr::TestKeyDown
            /// </summary>
            TestDown,

            /// <summary>
            /// ITfKeystrokeMgr::KeyUp
            /// </summary>
            Up,

            /// <summary>
            /// ITfKeystrokeMgr::KeyDown
            /// </summary>
            Down
        };

        #endregion public Enums

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        // Cal ITfThreadMgr.AssociateFocus() with dim
        private void SetFocusOnDim(NativeMethods.ITfDocumentMgr dim)
        {
            NativeMethods.ITfThreadMgrEx threadmgr = ThreadManager;

            if (threadmgr != null)
            {
                NativeMethods.ITfDocumentMgr prevDocMgr;
                threadmgr.AssociateFocus(InputMethod.WindowHandle, dim, out prevDocMgr);
            }
        }

        private void AdviseUIElementSink()
        {
            var source = ThreadManager as NativeMethods.ITfSource;
            var guid = NativeMethods.IID_ITfUIElementSink;
            int uiElementSinkCookie;
            source.AdviseSink(ref guid, _defaultTextStore, out uiElementSinkCookie);
            _defaultTextStore.UIElementSinkCookie = uiElementSinkCookie;
        }

        private void UnadviseUIElementSink()
        {
            var source = _defaultTextStore.DocumentManager as NativeMethods.ITfSource;

            if (_defaultTextStore.UIElementSinkCookie != NativeMethods.TF_INVALID_COOKIE)
            {
                source.UnadviseSink(_defaultTextStore.UIElementSinkCookie);
                _defaultTextStore.UIElementSinkCookie = NativeMethods.TF_INVALID_COOKIE;
            }
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        // Create an empty dim on demand.
        private NativeMethods.ITfDocumentMgr EmptyDocumentManager
        {
            get
            {
                if (_dimEmpty == null)
                {
                    NativeMethods.ITfThreadMgrEx threadManager = ThreadManager;
                    if (threadManager == null)
                    {
                        return null;
                    }

                    NativeMethods.ITfDocumentMgr dimEmptyTemp;
                    // Create a TSF document.
                    threadManager.CreateDocumentMgr(out dimEmptyTemp);
                    _dimEmpty = dimEmptyTemp;
                }
                return _dimEmpty;
            }
        }


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private DefaultTextStore _defaultTextStore;

        private NativeMethods.ITfContext _context;

        // This is true if thread manager is activated.
        private bool _istimactivated;

        // The root TSF object, created on demand.
        private NativeMethods.ITfThreadMgrEx _threadManager;

        // TSF ClientId from Activate call.
        private int _clientId;

        // The empty dim for this thread. Created on demand.
        private NativeMethods.ITfDocumentMgr _dimEmpty;

        #endregion Private Fields
    }
}
