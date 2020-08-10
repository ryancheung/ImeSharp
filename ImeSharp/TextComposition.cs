//
// 
// Description: TextComposition class is the object that contains
//              the input text. The text from keyboard input
//              is packed in this class when TextInput event is generated.
//              And this class also packs the state of the composition text when
//              the input text is being composed (for EA input, Speech).
//
//

using System;
using System.Diagnostics;

namespace ImeSharp
{
    //------------------------------------------------------
    //
    //  TextCompositionAutoComplete enum
    //
    //------------------------------------------------------

    /// <summary>
    /// The switch for automatic termination of the text composition
    /// </summary>
    public enum TextCompositionAutoComplete
    {
        /// <summary>
        /// AutomaticComplete is off.
        /// </summary>
        Off = 0,

        /// <summary>
        /// AutomaticComplete is on. 
        /// TextInput event will be generated automatically by TextCompositionManager after
        /// TextInputStart event is processed.
        /// </summary>
        On = 1,
    }

    public enum TextCompositionStage
    {
        /// <summary>
        /// The composition is not started yet.
        /// </summary>
        None = 0,

        /// <summary>
        /// The composition has started.
        /// </summary>
        Started = 1,

        /// <summary>
        /// The composition has completed or canceled.
        /// </summary>
        Done = 2,
    }

    /// <summary>
    ///     Text Composition class contains the result text of the text input and the state of the composition text.
    /// </summary>
    public class TextComposition
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     The constrcutor of TextComposition class.
        /// </summary>
        public TextComposition(string resultText) : this(resultText, TextCompositionAutoComplete.On)
        {
        }

        //
        ///     The constrcutor of TextComposition class.
        //
        public TextComposition(string resultText, TextCompositionAutoComplete autoComplete)
        {
            if (resultText == null)
            {
                throw new ArgumentException("result text cannot be null!");
            }

            _resultText = resultText;
            _compositionText = "";
            _systemText = "";
            _systemCompositionText = "";
            _controlText = "";

            _autoComplete = autoComplete;

            _stage = TextCompositionStage.None;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods 
        //
        //------------------------------------------------------

        /// <summary>
        ///     Finalize the composition.
        /// </summary>
        public virtual void Complete()
        {
            TextCompositionManager.CompleteComposition(this);
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        ///     The result text of the text input.
        /// </summary>
        public string Text
        {
            get { return _resultText; }
            protected set { _resultText = value; }
        }

        /// <summary>
        ///     The current composition text.
        /// </summary>
        public string CompositionText
        {
            get { return _compositionText; }

            protected set { _compositionText = value; }
        }

        /// <summary>
        ///     The current system text.
        /// </summary>
        public string SystemText
        {
            get { return _systemText; }

            protected set { _systemText = value; }
        }

        /// <summary>
        ///     The current system text.
        /// </summary>
        public string ControlText
        {
            get { return _controlText; }

            protected set { _controlText = value; }
        }

        /// <summary>
        ///     The current system text.
        /// </summary>
        public string SystemCompositionText
        {
            get { return _systemCompositionText; }

            protected set { _systemCompositionText = value; }
        }

        /// <summary>
        ///     The switch for automatic termination.
        /// </summary>
        public TextCompositionAutoComplete AutoComplete
        {
            get { return _autoComplete; }
        }


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

        /// <summary>
        ///     The current composition text.
        /// </summary>
        public void SetText(string resultText)
        {
            _resultText = resultText;
        }

        /// <summary>
        ///     The current composition text.
        /// </summary>
        public void SetCompositionText(string compositionText)
        {
            _compositionText = compositionText;
        }


        /// <summary>
        ///     Convert this composition to system composition.
        /// </summary>
        public void MakeSystem()
        {
            _systemText = _resultText;
            _systemCompositionText = _compositionText;

            _resultText = "";
            _compositionText = "";
            _controlText = "";
        }

        /// <summary>
        ///     Convert this composition to system composition.
        /// </summary>
        public void MakeControl()
        {
            // Onlt control char should be in _controlText.
            Debug.Assert((_resultText.Length == 1) && Char.IsControl(_resultText[0]));

            _controlText = _resultText;

            _resultText = "";
            _systemText = "";
            _compositionText = "";
            _systemCompositionText = "";
        }

        /// <summary>
        ///     Clear all the current texts.
        /// </summary>
        public void ClearTexts()
        {
            _resultText = "";
            _compositionText = "";
            _systemText = "";
            _systemCompositionText = "";
            _controlText = "";
        }

        //------------------------------------------------------
        //
        //  public Properties
        //
        //------------------------------------------------------

        // the stage of this text composition
        public TextCompositionStage Stage
        {
            get { return _stage; }
            set { _stage = value; }
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

        // The finalized and result string.
        private string _resultText;

        // The composition string.
        private string _compositionText;

        // The system string.
        private string _systemText;

        // The control string.
        private string _controlText;

        // The system composition string.
        private string _systemCompositionText;

        // If this is true, TextComposition Manager will terminate the compositon automatically.
        private readonly TextCompositionAutoComplete _autoComplete;

        // TextComposition stage.
        private TextCompositionStage _stage;
    }
}
