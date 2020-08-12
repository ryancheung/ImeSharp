//
// 
// Description: DefaultTextStoreTextComposition class is the composition 
//              object for the input in DefaultTextStore.
//              Cicero's composition injected to DefaulteTextStore is
//              represent by this DefaultTextStoreTextComposition.
//              This has custom Complete method to control
//              Cicero's composiiton.
//
//

using System;
using System.Runtime.InteropServices;
using ImeSharp.Native;

namespace ImeSharp
{
    /// <summary>
    ///     DefaultTextStoreTextComposition class implements Complete for 
    ///     the composition in DefaultTextStore.
    /// </summary>
    public class DefaultTextStoreTextComposition : TextComposition
    {
        //------------------------------------------------------
        //
        //  ctor
        //
        //------------------------------------------------------

        /// <summary>
        ///     ctor
        /// </summary>
        public DefaultTextStoreTextComposition(string text, TextCompositionAutoComplete autoComplete) : base(text, autoComplete)
        {
        }

        //------------------------------------------------------
        //
        //  Public Interface Methods 
        //
        //------------------------------------------------------

        /// <summary>
        ///     Finalize the composition.
        ///     This does not call base.Complete() because TextComposition.Complete()
        ///     will call TextServicesManager.CompleteComposition() directly to generate TextCompositionEvent.
        ///     We finalize Cicero's composition and DefaultTextStore will automatically
        ///     generate the proper TextComposition events.
        /// </summary>
        public override void Complete()
        {
            //             VerifyAccess();

            var context = TextServicesContext.Current.EditContext;
            NativeMethods.ITfContextOwnerCompositionServices compositionService = context as NativeMethods.ITfContextOwnerCompositionServices;
            NativeMethods.ITfCompositionView composition = GetComposition(context);

            if (composition != null)
            {
                // Terminate composition if there is a composition view.
                compositionService.TerminateComposition(composition);
                Marshal.ReleaseComObject(composition);
            }

            Marshal.ReleaseComObject(context);
        }

        //------------------------------------------------------
        //
        //  private Methods 
        //
        //------------------------------------------------------

        /// <summary>
        ///     Get ITfContextView of the context.
        /// </summary>
        private NativeMethods.ITfCompositionView GetComposition(NativeMethods.ITfContext context)
        {
            NativeMethods.ITfContextComposition contextComposition;
            NativeMethods.IEnumITfCompositionView enumCompositionView;
            NativeMethods.ITfCompositionView[] compositionViews = new NativeMethods.ITfCompositionView[1];
            int fetched;

            contextComposition = (NativeMethods.ITfContextComposition)context;
            contextComposition.EnumCompositions(out enumCompositionView);

            enumCompositionView.Next(1, compositionViews, out fetched);

            Marshal.ReleaseComObject(enumCompositionView);
            return compositionViews[0];
        }
    }
}
