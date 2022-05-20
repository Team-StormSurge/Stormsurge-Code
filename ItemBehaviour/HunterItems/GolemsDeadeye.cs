using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using static StormSurge.Utils.LanguageProvider;

namespace StormSurge.ItemBehaviour
{
    class GolemsDeadeye : ItemBase
    {
        static string prefix = "ITEM_HUNTER_" + "GOLEMEYE";
        protected override ItemLanguage lang => new()
        {
            nameToken = new LanguagePair($"{prefix}_NAME", "Golem's Deadeye"),
            pickupToken = new LanguagePair($"{prefix}_PICKUP", "Gain power from consistent attacks."),
            descToken = new LanguagePair($"{prefix}_DESC", "placeholder"),
            loreToken = new LanguagePair($"{prefix}_LORE", "placeholder"),
        };
        protected override string itemDefName => "GolemsDeadeye";
        public override void AddItemBehaviour()
        {
            UnityEngine.Debug.LogWarning("System Init Golem's Deadeye");
        }

    }
    class GolemDeadeyeComponent : UnityEngine.MonoBehaviour, IOnDamageDealtServerReceiver
    {

        // LunarDetonatorPassiveAttachment passiveController;
        ////USE BASEITEMBODYBEHAVIOUR for addcompontent-style implementation?

        public void OnDamageDealtServer(DamageReport damageReport)
        {
            throw new NotImplementedException();
        }
    }
}
