using System;

namespace ImeSharp
{
    public class SecurityCriticalDataClass<T>
    {
        public SecurityCriticalDataClass(T value)
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
