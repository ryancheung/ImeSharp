using System;

namespace ImeSharp
{
    public delegate void TextInputCallback(char character);
    public delegate void TextCompositionCallback(string compositionText, int cursorPosition);
}
