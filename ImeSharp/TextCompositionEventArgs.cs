using System;

namespace ImeSharp
{
    /// <summary>
    /// Arguments for the <see cref="IImmService.TextComposition" /> event.
    /// </summary>
    public struct TextCompositionEventArgs
    {
        /// <summary>
        // Construct a TextCompositionEventArgs with composition infos.
        /// </summary>
        public TextCompositionEventArgs(ImeCompositionString compositionString,
                                        int cursorPosition,
                                        string[] candidateList = null,
                                        int candidatePageStart = 0,
                                        int candidatePageSize = 0,
                                        int candidateSelection = 0)
        {
            CompositionString = compositionString;
            CursorPosition = cursorPosition;

            CandidateList = candidateList;
            CandidatePageStart = candidatePageStart;
            CandidatePageSize = candidatePageSize;
            CandidateSelection = candidateSelection;
        }

        /// <summary>
        /// The full string as it's composed by the IMM.
        /// </summary>    
        public readonly ImeCompositionString CompositionString;

        /// <summary>
        /// The position of the cursor inside the composed string.
        /// </summary>    
        public readonly int CursorPosition;

        /// <summary>
        /// The candidate text list for the composition.
        /// This property is only supported on WindowsDX and WindowsUniversal.
        /// If the composition string does not generate candidates this array is empty.
        /// </summary>    
        public readonly string[] CandidateList;

        /// <summary>
        /// First candidate index of current page.
        /// </summary>
        public readonly int CandidatePageStart;

        /// <summary>
        /// How many candidates should display per page.
        /// </summary>
        public readonly int CandidatePageSize;

        /// <summary>
        /// The selected candidate index.
        /// </summary>
        public readonly int CandidateSelection;
    }
}
