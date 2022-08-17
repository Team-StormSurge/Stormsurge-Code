using System;
using System.Collections.Generic;
using System.Text;

namespace StormSurge.Utils.ReferenceHelper
{
    public class InstRef<T> where T : class?
    {
        Func<T> refMethod;
        public InstRef(Func<T> RefMethod)
        {
            this.refMethod = RefMethod;
        }
        T _backingField;
        public T Reference
        { 
            get
            {
                if (_backingField == default)
                {
                    _backingField = refMethod();
                    UnityEngine.Debug.LogWarning($"{_backingField} is being initialised; was {default(T)?.ToString() ?? "null"}");
                }
                return _backingField;
            }
            private set
            {_backingField = value;}
        }
        public static implicit operator T(InstRef<T> self)
        {
            return self.Reference;
        }
        public static implicit operator InstRef<T>(Func<T> newMethod)
        {
            return new InstRef<T>(newMethod);
        }
    }
}
