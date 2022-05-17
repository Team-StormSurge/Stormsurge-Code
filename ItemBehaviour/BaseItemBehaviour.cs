using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using static StormSurge.Utils.LanguageProvider;

namespace StormSurge.ItemBehaviour
{
    public abstract class BaseItemBehaviour
    {
        protected abstract string itemDefName { get; }
        private ItemDef _itemDef;
        protected ItemDef itemDef
        {
            get
            {
                _itemDef ??= Assets.ContentPack.itemDefs.Find(itemDefName);
                return _itemDef;
            }
        }
        [SystemInitializer]
        protected abstract void AddItemBehaviour();


    }
    public class ItemLanguage
    {
        public LanguagePair nameToken;
        public LanguagePair pickupToken;
        public LanguagePair descToken;
        public LanguagePair loreToken;
    }
}
