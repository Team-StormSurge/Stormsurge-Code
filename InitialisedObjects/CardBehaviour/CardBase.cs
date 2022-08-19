using System;
using System.Collections.Generic;
using System.Text;

namespace StormSurge.Interactables
{
    /// <summary>
    /// The class that we use for any behaviours that associate with our modded interactables.
    /// </summary>
    public abstract class CardBase : InitialisedBase
    {
        public override void InitFunction()
        {
            AddCard();
        }
        /// <summary>
        /// Runs any miscellaneous behaviours for instantiating our Interactable behaviour.
        /// </summary>
        protected abstract void AddCard();
    }
}
