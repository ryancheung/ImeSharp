using System;
using System.Collections;
using System.Collections.Generic;

namespace ImeSharp
{
    public unsafe struct ImeCompositionString : IEnumerable<char>
    {
        internal const int ImeCharBufferSize = 64;

        public static readonly ImeCompositionString Empty = new ImeCompositionString((List<char>)null);

        internal struct Enumerator : IEnumerator<char>
        {
            private ImeCompositionString _imeString;
            private char _currentCharacter;
            private int _currentIndex;

            public Enumerator(ImeCompositionString imeString)
            {
                _imeString = imeString;
                _currentCharacter = '\0';
                _currentIndex = -1;
            }

            public bool MoveNext()
            {
                int size = _imeString.Count;

                _currentIndex++;

                if (_currentIndex == size)
                    return false;

                fixed (char* ptr = _imeString.buffer)
                {
                    _currentCharacter = *(ptr + _currentIndex);
                }

                return true;
            }

            public void Reset()
            {
                _currentIndex = -1;
            }

            public void Dispose()
            {
            }

            public char Current { get { return _currentCharacter; } }
            object IEnumerator.Current { get { return Current; } }
        }

        public int Count => _size;

        public char this[int index]
        {
            get
            {
                if (index >= Count || index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                fixed (char* ptr = buffer)
                {
                    return *(ptr + index);
                }
            }
        }

        private int _size;

        fixed char buffer[ImeCharBufferSize];

        public ImeCompositionString(string characters)
        {
            if (string.IsNullOrEmpty(characters))
            {
                _size = 0;
                return;
            }

            _size = characters.Length;
            if (_size > ImeCharBufferSize)
                _size = ImeCharBufferSize - 1;

            for (var i = 0; i < _size; i++)
                buffer[i] = characters[i];
        }

        public ImeCompositionString(List<char> characters)
        {
            if (characters == null || characters.Count == 0)
            {
                _size = 0;
                return;
            }

            _size = characters.Count;
            if (_size > ImeCharBufferSize)
                _size = ImeCharBufferSize - 1;

            for (var i = 0; i < _size; i++)
                buffer[i] = characters[i];
        }

        public ImeCompositionString(char[] characters, int count)
        {
            if (characters == null || count <= 0)
            {
                _size = 0;
                return;
            }

            _size = count;
            if (_size > ImeCharBufferSize)
                _size = ImeCharBufferSize - 1;

            if (_size > characters.Length)
                _size = characters.Length;

            for (var i = 0; i < _size; i++)
                buffer[i] = characters[i];
        }

        public override string ToString()
        {
            fixed (char* ptr = buffer)
            {
                return new string(ptr, 0, _size);
            }
        }

        public IEnumerator<char> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
