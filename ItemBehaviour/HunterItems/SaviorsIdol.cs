using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using static StormSurge.Utils.LanguageProvider;

namespace StormSurge.ItemBehaviour
{
    class SaviorsIdol : BaseItemBehaviour
    {
        static string prefix = "ITEM_HUNTER_" + "SAVIORIDOL";
        ItemLanguage lang = new ItemLanguage()
        {
            nameToken = new LanguagePair($"{prefix}_NAME", "Savior Idol"),
            pickupToken = new LanguagePair($"{prefix}_PICKUP", "In peril, find clarity."),
            descToken = new LanguagePair($"{prefix}_DESC", ""),
            loreToken = new LanguagePair($"{prefix}_LORE", ""),
        };

        protected override string itemDefName => "SaviorIdol";

        protected override void AddItemBehaviour()
        {
            On.RoR2.CharacterBody.RecalculateStats += (On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self) =>
            {
                orig(self);
            };
            On.RoR2.CharacterMaster.OnInventoryChanged += (On.RoR2.CharacterMaster.orig_OnInventoryChanged orig, CharacterMaster self) =>
            {
                orig(self);
                var HP = self.GetBody().healthComponent;
                int idolLuck = (int) UnityEngine.Mathf.Max(25 - (HP.health * 100f / HP.fullHealth), 0);
                self.luck += UnityEngine.Mathf.Min(self.inventory.GetItemCount(itemDef));
            };
        }
    }
}
