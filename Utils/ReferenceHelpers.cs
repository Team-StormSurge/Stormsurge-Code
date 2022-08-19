using System;
using System.Collections.Generic;
using System.Text;

namespace StormSurge.Utils.ReferenceHelper
{
    /// <summary>
    /// Specialised Helper Class that instantiates its value only upon first use, and can regenerate it if it is Garbage-Collected.
    /// </summary>
    /// <typeparam name="T">The Type of the instance that this object references.</typeparam>
    public class InstRef<T> where T : class? // only nullable classes can be used in InstRefs
    {
        Func<T> refMethod;
        /// <summary>
        /// Constructs a specialised Helper Class that instantiates its value only upon first use, and can regenerate it if it is Garbage-Collected.
        /// </summary>
        /// <param name="RefMethod">The method used to instantiate this Reference later.</param>
        public InstRef(Func<T> RefMethod) // constructor which takes a lambda or action- this method body should return a reference for our InstRef<T>!
        {
            this.refMethod = RefMethod;
        }
        T _backingField; // the field that saves the result of our ref method.
        public T Reference
        { 
            get
            {
                if (_backingField == default)
                {
                    _backingField = refMethod(); // if our field hasn't been instantiated, we do it now
                    ///UnityEngine.Debug.LogWarning($"{_backingField} is being initialised; was {default(T)?.ToString() ?? "null"}");
                }
                return _backingField; // return the result, even if we did not have to regenerate it this time
            }
            private set
            {_backingField = value;} //only this class can use this; just sets the backing field's value
        }
        public static implicit operator T(InstRef<T> self) //implicit operator can convert an InstRef to its T Instance
        {
            return self.Reference;
        }
        public static implicit operator InstRef<T>(Func<T> newMethod) //implicit operator can convert a lambda or method into an uninstantiated InstRef.
        {
            return new InstRef<T>(newMethod);
        }
    }
}
