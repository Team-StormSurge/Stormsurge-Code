using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using HarmonyLib;

namespace StormSurge
{
    /// <summary>
    /// The class that we use for any behaviours that associate with our modded items.
    /// </summary>
    [HarmonyPatch]
    public abstract class ItemBase : InitialisedBase
    {
        //Name of the ItemDef with which we associate this behaviour
        /// <summary>
        /// Name string used to find the ItemDef for this item
        /// </summary>
        protected abstract string itemDefName { get; }

        //the ItemDef with which we've associated this behaviour; if it returns null, we didn't find the item behaviour! 
        private ItemDef _itemDef;
        public ItemDef itemDef
        {
            get
            {
                if(_itemDef == null) _itemDef = Assets.ContentPack.itemDefs.Find(itemDefName);
                return _itemDef;
            }
        }
        //runs any initial code for our behaviour, including adding config and subscribing to events
        public override void InitFunction()
        {
            //if (!AddConfig()) return;
            AddConfig();
            AddItemBehaviour();
        }

        
        /// <summary>
        /// Used to subscribe to any events for this item behaviour, etc.
        /// </summary>
        public abstract void AddItemBehaviour();
    }
}
