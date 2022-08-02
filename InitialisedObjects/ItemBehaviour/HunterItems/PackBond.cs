using System;
using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using static StormSurge.Utils.LanguageProvider;

namespace StormSurge.ItemBehaviour
{
    class PackBond : ItemBase
    {
        #region LoadedContent
        static NetworkSoundEventDef? _packBondBlockSound;
        static NetworkSoundEventDef PackBondBlockSound
        {
            get
            {
                _packBondBlockSound ??= Assets.ContentPack.networkSoundEventDefs.Find("nsePackBondBlock");
                return _packBondBlockSound;
            }
        }
        #endregion
        static string prefix = "ITEM_HUNTER_" + "PACKBOND";
        protected override ItemLanguage lang => new()
        {
            nameToken = new LanguagePair($"{prefix}_NAME", "Pack Bond"),
            pickupToken = new LanguagePair($"{prefix}_PICKUP", "Grant flat damage reduction to all allies."),
            descToken = new LanguagePair($"{prefix}_DESC", "placeholder"),
            loreToken = new LanguagePair($"{prefix}_LORE", "placeholder"),
        };
        protected override string itemDefName => "PackBond";
        protected override string configName => "Pack Bond";
        public override void AddItemBehaviour()
        {}

        #region Config Entries
        private ConfigEntry<float>? armorPerAlly;
        protected override bool AddConfig()
        {
            armorPerAlly = Config.configFile!.Bind(configName, "Armor Stat", 5f, "The damage reduction granted to each ally, per stack of Pack Bond.");
            return base.AddConfig();
        }
        #endregion


        [HarmonyILManipulator, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.TakeDamage))]
        public static void IlTakeDamage(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(MoveType.Before, x => x.MatchLdfld<HealthComponent.ItemCounts>(nameof(HealthComponent.ItemCounts.armorPlate)));
            var where = c.Index;
            int num = -1;
            c.GotoNext(x => x.MatchLdloc(out num));
            c.Index = where;
            c.Emit(OpCodes.Ldarg_0); // Body local? Emitting for delegate I think??
            c.Emit(OpCodes.Ldloc, num);
            c.EmitDelegate<Func<HealthComponent, float, float>>((self, amount) =>
            {
                int itemCount = 0;
                PackBond? initialised;
                initialised = GetInstance<PackBond>();
                foreach(TeamComponent tComp in TeamComponent.GetTeamMembers(self.body.teamComponent.teamIndex))
                {
                    var bod = tComp.body;
                    if (!bod.inventory) continue;
                    itemCount += bod.inventory.GetItemCount(initialised!.itemDef);
                }
                //int itemCount = self.body.inventory.GetItemCount(initialised!.itemDef);
                float damageReduc = itemCount * initialised!.armorPerAlly!.Value;
                amount = UnityEngine.Mathf.Max(1f, amount - damageReduc);
                if(itemCount > 0) RoR2.Audio.EntitySoundManager.EmitSoundServer(PackBondBlockSound.index, self.body.gameObject);
                return amount;
            });
            c.Emit(OpCodes.Stloc, num);
        }
    }
}
