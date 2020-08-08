using System;

namespace ImeSharp
{
    public sealed class TextCompositionManager
    {
        /// <summary>
        ///     Start the composition.
        /// </summary>
        public static void StartComposition(TextComposition composition)
        {
            if (composition == null)
                throw new ArgumentNullException("composition");

            //TODO: Raise composition events
        }

        /// <summary>
        ///     Update the composition.
        /// </summary>
        public static void UpdateComposition(TextComposition composition)
        {
            if (composition == null)
                throw new ArgumentNullException("composition");

            //TODO: Raise composition event
        }

        /// <summary>
        ///     Complete the composition.
        /// </summary>
        public static void CompleteComposition(TextComposition composition)
        {
            if (composition == null)
                throw new ArgumentNullException("composition");

            //TODO: Raise composition result event
        }
    }
}