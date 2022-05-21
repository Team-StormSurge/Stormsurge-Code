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
        {}

        private ConfigEntry<float>? armorPerAlly;
        protected override bool AddConfig()
        {
            armorPerAlly = Config.configFile!.Bind(lang.nameToken.ingameText, "Armor Stat", 5f, "The armor granted to you per ally, per stack of Pack Bond.");
            return base.AddConfig();
        }


        [HarmonyILManipulator, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.TakeDamage))]
        public static void IlTakeDamage(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(MoveType.After, x => x.OpCode == OpCodes.Ldsfld && (x.Operand as FieldReference)?.Name == nameof(RoR2Content.Items.ArmorPlate),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchStloc(out _));
            var where = c.Index;
            int num2 = -1;
            c.GotoNext(x => x.MatchLdloc(out num2),
                x => x.MatchLdcR4(1f),
                x => x.MatchLdloc(out _));
            c.Index = where;
            c.Emit(OpCodes.Ldloc_1); // Body; 0 is master
            c.Emit(OpCodes.Ldloc, num2);
            c.EmitDelegate<Func<CharacterBody, float, float>>((body, amount) =>
            {
                int allies = 0;
                PackBond? initialised;
                initialised = GetInstance<PackBond>();
                foreach (CharacterMaster master in CharacterMaster.readOnlyInstancesList)
                {
                    if (master.teamIndex == body.teamComponent.teamIndex &&
                    (master.hasBody) &&
                    (!master.playerCharacterMasterController || master.playerCharacterMasterController.isConnected))
                        allies++;
                }
                int itemCount = body.inventory.GetItemCount(initialised!.itemDef);
                float damageReduc = allies * itemCount * initialised.armorPerAlly!.Value;
                amount = UnityEngine.Mathf.Min(1f, amount - damageReduc);
                if (itemCount > 0) UnityEngine.Debug.LogWarning($"Removing {damageReduc} damage from attack, for {itemCount} stacks and {allies} allies");
                return amount;
            });
            c.Emit(OpCodes.Stloc, num2);
        }
    }
}
