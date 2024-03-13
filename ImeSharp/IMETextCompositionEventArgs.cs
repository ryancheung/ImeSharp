using System;

namespace ImeSharp
{
    /// <summary>
    /// Arguments for the <see cref="IImmService.TextComposition" /> event.
    /// </summary>
    public struct IMETextCompositionEventArgs
    {
        /// <summary>
        // Construct a TextCompositionEventArgs with composition infos.
        /// </summary>
        public IMETextCompositionEventArgs(IMEString compositionText, int cursorPosition)
        {
            CompositionText = compositionText;
            CursorPosition = cursorPosition;
        }

        /// <summary>
        /// The full string as it's composed by the IMM.
        /// </summary>    
        public readonly IMEString CompositionText;

        /// <summary>
        /// The position of the cursor inside the composed string.
        /// </summary>    
        public readonly int CursorPosition;
    }
}
