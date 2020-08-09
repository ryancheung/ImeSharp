using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using ImeSharp.Native;

namespace ImeSharp
{
    internal abstract class ImmCompositionResultHandler
    {
        protected IntPtr _imeContext;

        public int Flag { get; private set; }

        internal ImmCompositionResultHandler(IntPtr imeContext, int flag)
        {
            this.Flag = flag;
            _imeContext = imeContext;
        }

        internal virtual void Update() { }

        internal bool Update(int lParam)
        {
            if ((lParam & Flag) == Flag)
            {
                Update();
                return true;
            }
            return false;
        }
    }

    internal class ImmCompositionString : ImmCompositionResultHandler, IEnumerable<byte>
    {
        private byte[] _values;

        public int Length { get; private set; }

        public byte[] Values { get { return _values; } }

        public byte this[int index] { get { return _values[index]; } }

        internal ImmCompositionString(IntPtr imeContext, int flag) : base(imeContext, flag)
        {
            Clear();
        }

        public IEnumerator<byte> GetEnumerator()
        {
            foreach (byte b in _values)
                yield return b;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public override string ToString()
        {
            if (Length <= 0)
                return string.Empty;

            return Encoding.Unicode.GetString(_values, 0, Length);
        }

        internal void Clear()
        {
            _values = new byte[0];
            Length = 0;
        }

        internal override void Update()
        {
            Length = NativeMethods.ImmGetCompositionString(_imeContext, Flag, IntPtr.Zero, 0);
            IntPtr pointer = Marshal.AllocHGlobal(Length);
            try
            {
                NativeMethods.ImmGetCompositionString(_imeContext, Flag, pointer, Length);
                _values = new byte[Length];
                Marshal.Copy(pointer, _values, 0, Length);
            }
            finally
            {
                Marshal.FreeHGlobal(pointer);
            }
        }
    }

    internal class ImmCompositionInt : ImmCompositionResultHandler
    {
        public int Value { get; private set; }

        internal ImmCompositionInt(IntPtr imeContext, int flag) : base(imeContext, flag) { }

        public override string ToString()
        {
            return Value.ToString();
        }

        internal override void Update()
        {
            Value = NativeMethods.ImmGetCompositionString(_imeContext, Flag, IntPtr.Zero, 0);
        }
    }
}
