using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RoR2;
using static StormSurge.Utils.LanguageProvider;

namespace StormSurge.ItemBehaviour
{
    class PackBond : ItemBase
    {
        static string prefix = "ITEM_HUNTER_" + "PACKBOND";
        protected override ItemLanguage lang => new()
        {
            nameToken = new LanguagePair($"{prefix}_NAME", "Pack Bond"),
            pickupToken = new LanguagePair($"{prefix}_PICKUP", "Gain protection from each ally."),
            descToken = new LanguagePair($"{prefix}_DESC", "placeholder"),
            loreToken = new LanguagePair($"{prefix}_LORE", "placeholder"),
        };
        protected override string itemDefName => "PackBond";
        public override void AddItemBehaviour()
        {
            UnityEngine.Debug.LogWarning("System Init Pack Bond");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
        public static void UpdatePackBondStack(CharacterBody self)
        {
            int allies = 0;
            foreach (CharacterMaster body in CharacterMaster.readOnlyInstancesList)
            {
                if (body.teamIndex == self.teamComponent.teamIndex &&
                (body.hasBody) &&
                (!body.playerCharacterMasterController || body.playerCharacterMasterController.isConnected))
                    allies++;
            }
            self.armor += allies * self.inventory.GetItemCount(GetInstance<PackBond>()!.itemDef);
        }
    }
}
