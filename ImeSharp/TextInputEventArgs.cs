namespace ImeSharp
{
    public struct TextInputEventArgs
    {
        public TextInputEventArgs(char result)
        {
            Result = result;
        }

        public readonly char Result;
    }
}
