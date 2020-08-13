using System;
using System.Diagnostics;

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

            Debug.WriteLine("StartComposition, composition string: {0}", new object [] { composition.CompositionText });
            //TODO: Raise composition events
        }

        /// <summary>
        ///     Update the composition.
        /// </summary>
        public static void UpdateComposition(TextComposition composition)
        {
            if (composition == null)
                throw new ArgumentNullException("composition");

            Debug.WriteLine("UpdateComposition, composition string: {0}", new object [] { composition.CompositionText });
            //TODO: Raise composition event
        }

        /// <summary>
        ///     Complete the composition.
        /// </summary>
        public static void CompleteComposition(TextComposition composition)
        {
            if (composition == null)
                throw new ArgumentNullException("composition");

            Debug.WriteLine("CompleteComposition, composition string: {0}, result text: {1}", composition.CompositionText, composition.Text);
            //TODO: Raise composition result event
        }
    }
}
