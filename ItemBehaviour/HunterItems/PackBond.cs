using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using static StormSurge.Utils.LanguageProvider;

namespace StormSurge.ItemBehaviour
{
    class PackBond : BaseItemBehaviour
    {
        static string prefix = "ITEM_HUNTER_" + "PACKBOND";
        ItemLanguage lang = new ItemLanguage()
        {
            nameToken = new LanguagePair($"{prefix}_NAME", "Pack Bond"),
            pickupToken = new LanguagePair($"{prefix}_PICKUP", "Gain protection from each ally."),
            descToken = new LanguagePair($"{prefix}_DESC", ""),
            loreToken = new LanguagePair($"{prefix}_LORE", ""),
        };
        protected override string itemDefName => "PackBond";

        protected override void AddItemBehaviour()
        {
            On.RoR2.CharacterBody.RecalculateStats += (On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self) =>
            {
                orig(self);
                int allies = 0;
                foreach(CharacterMaster body in CharacterMaster.readOnlyInstancesList)
                {
                    if(body.teamIndex == self.teamComponent.teamIndex && 
                    (body.hasBody) && 
                    (!body.playerCharacterMasterController || body.playerCharacterMasterController.isConnected))
                        allies++;
                }
                self.armor += allies * self.inventory.GetItemCount(itemDef);
            };
        }
    }
}
