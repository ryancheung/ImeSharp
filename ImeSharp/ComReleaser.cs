using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ImeSharp
{
    internal class ComReleaser : IDisposable
    {
        public T CreateComObject<T>() where T : class
        {
            var comClass = Attribute.GetCustomAttribute(typeof(T), typeof(CoClassAttribute)) as CoClassAttribute;
            if (comClass == null)
            {
                return default(T);
            }
            var obj = Activator.CreateInstance(Type.GetTypeFromCLSID(comClass.CoClass.GUID)) as T;
            RegisterObject(obj);
            return obj;
        }

        public void RegisterObject(object o)
        {
            if (!Marshal.IsComObject(o))
            {
                return;
            }
            comObjects_.Add(o);
        }

        public void RegisterCleanup(Action action)
        {
            cleanupActions_.Add(action);
        }

        public void Dispose()
        {
            cleanupActions_.Reverse();
            cleanupActions_.ForEach(action => action());
            cleanupActions_.Clear();
            comObjects_.Reverse();
            comObjects_.ForEach(o => Marshal.ReleaseComObject(o));
            comObjects_.Clear();
        }

        private readonly List<Action> cleanupActions_ = new List<Action>();

        private readonly List<object> comObjects_ = new List<object>();
    }
}
