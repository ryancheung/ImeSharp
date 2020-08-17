namespace ImeSharp
{
    public struct TextInputEventArgs
    {
        public TextInputEventArgs(char character)
        {
            Character = character;
        }

        public readonly char Character;
    }
}