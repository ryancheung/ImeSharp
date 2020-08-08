using System;

namespace ImeSharp
{
    public struct SecurityCriticalData<T>
    {
        public SecurityCriticalData(T value)
        {
            _value = value;
        }

        public T Value
        {
            get
            {
                return _value;
            }
        }

        private T _value;
    }
}
