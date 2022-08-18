using System;
using System.Collections.Generic;
using System.Text;

namespace StormSurge.Interactables
{
    public abstract class CardBase : InitialisedBase
    {
        public override void InitFunction()
        {
            AddCard();
        }
        protected abstract void AddCard();
    }
}
