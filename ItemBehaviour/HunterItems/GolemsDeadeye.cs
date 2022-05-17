using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using static StormSurge.Utils.LanguageProvider;

namespace StormSurge.ItemBehaviour
{
    class GolemsDeadeye : BaseItemBehaviour
    {
        static string prefix = "ITEM_HUNTER_" + "GOLEMEYE";
        ItemLanguage lang = new ItemLanguage()
        {
            nameToken = new LanguagePair($"{prefix}_NAME", "Golem's Deadeye"),
            pickupToken = new LanguagePair($"{prefix}_PICKUP", "Gain power from consistent attacks."),
            descToken = new LanguagePair($"{prefix}_DESC", ""),
            loreToken = new LanguagePair($"{prefix}_LORE", ""),
        };
        protected override string itemDefName => "GolemsDeadeye";

        protected override void AddItemBehaviour()
        {
            throw new NotImplementedException();
        }

    }
    class GolemDeadeyeComponent : UnityEngine.MonoBehaviour, IOnDamageDealtServerReceiver
    {

        // LunarDetonatorPassiveAttachment passiveController;

        public void OnDamageDealtServer(DamageReport damageReport)
        {
            throw new NotImplementedException();
        }
    }
}
