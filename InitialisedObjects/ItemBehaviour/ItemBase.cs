using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using static StormSurge.Utils.LanguageProvider;
using StormSurge.InitialisedObjects;
using HarmonyLib;

namespace StormSurge.ItemBehaviour
{

    [HarmonyPatch]
    public abstract class ItemBase : InitialisedBase
    {
        protected abstract string itemDefName { get; }
        protected abstract ItemLanguage lang { get; }
        protected ItemLanguage tokens;

        private ItemDef _itemDef;
        public ItemDef itemDef
        {
            get
            {
                if(_itemDef == null) _itemDef = Assets.ContentPack.itemDefs.Find(itemDefName);
                return _itemDef;
            }
        }
        public override void InitFunction()
        {
            //if (!AddConfig()) return;
            AddConfig();
            AddItemBehaviour();
            tokens = lang;
        }

        
        
        public abstract void AddItemBehaviour();
    }
    public class ItemLanguage
    {
        public LanguagePair nameToken;
        public LanguagePair pickupToken;
        public LanguagePair descToken;
        public LanguagePair loreToken;
    }
}
