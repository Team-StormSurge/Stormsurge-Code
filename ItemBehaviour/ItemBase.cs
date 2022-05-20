using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using static StormSurge.Utils.LanguageProvider;
using StormSurge.InitialisedObjects;

namespace StormSurge.ItemBehaviour
{

    public abstract class ItemBase : InitialisedBase
    {
        protected abstract string itemDefName { get; }
        protected abstract ItemLanguage lang { get; }
        protected override string name => lang.nameToken.ingameText;

        private ItemDef _itemDef;
        protected ItemDef itemDef
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
        }

        
        
        public abstract void AddItemBehaviour();
    }
    public struct ItemLanguage
    {
        public LanguagePair nameToken;
        public LanguagePair pickupToken;
        public LanguagePair descToken;
        public LanguagePair loreToken;
    }
}
