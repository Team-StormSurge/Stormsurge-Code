using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RoR2;
using static StormSurge.Utils.LanguageProvider;

namespace StormSurge.ItemBehaviour
{
    class SaviorsIdol : ItemBase
    {
        static string prefix = "ITEM_HUNTER_" + "SAVIORIDOL";
        protected override ItemLanguage lang => new()
        {
            nameToken = new LanguagePair($"{prefix}_NAME", "Savior Idol"),
            pickupToken = new LanguagePair($"{prefix}_PICKUP", "In peril, find clarity."),
            descToken = new LanguagePair($"{prefix}_DESC", "placeholder"),
            loreToken = new LanguagePair($"{prefix}_LORE", "placeholder"),
        };

        protected override string itemDefName => "SaviorIdol";
        public override void AddItemBehaviour()
        {

        }

        [HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
        public static void ReculateIdolEffect()
        {

        }
        [HarmonyPostfix, HarmonyPatch(typeof(CharacterMaster), nameof(CharacterMaster.OnInventoryChanged))]
        public static void RecalculateIdolStack()
        {
            /*var HP = self.GetBody().healthComponent;
            int idolLuck = (int)UnityEngine.Mathf.Max(25 - (HP.health * 100f / HP.fullHealth), 0);
            self.luck += UnityEngine.Mathf.Min(self.inventory.GetItemCount(GetInstance<SaviorsIdol>()?.itemDef));*/
            UnityEngine.Debug.LogError("Bitch you thought, this is stupid and I'm not doing it yet");
        }
    }
}
