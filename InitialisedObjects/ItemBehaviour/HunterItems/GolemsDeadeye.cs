using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using static StormSurge.Utils.LanguageProvider;

namespace StormSurge.ItemBehaviour
{
    class GolemsDeadeye : ItemBase
    {
        #region LoadedContent
        static BuffDef? _deadeyeEffect;
        static BuffDef DeadeyeEffect
        {
            get
            {
                _deadeyeEffect ??= Assets.ContentPack.buffDefs.Find("DeadeyeEffect");
                return _deadeyeEffect;
            }
        }

        static NetworkSoundEventDef? _deadeyeEffectSound;
        static NetworkSoundEventDef? DeadeyeEffectSound
        {
            get
            {
                _deadeyeEffectSound ??= Assets.ContentPack.networkSoundEventDefs.Find("nseGolemsDeadeyeEffect");
                return _deadeyeEffectSound;
            }
        }
        #endregion
        static string prefix = "ITEM_HUNTER_" + "GOLEMEYE";
        protected override ItemLanguage lang => new()
        {
            nameToken = new LanguagePair($"{prefix}_NAME", "Golem's Deadeye"),
            pickupToken = new LanguagePair($"{prefix}_PICKUP", "Gain power from consistent attacks."),
            descToken = new LanguagePair($"{prefix}_DESC", "placeholder"),
            loreToken = new LanguagePair($"{prefix}_LORE", "placeholder"),
        };
        protected override string itemDefName => "GolemsDeadeye";
        protected override string configName => "Golem Deadeye";
        public override void AddItemBehaviour()
        {}

        [HarmonyILManipulator, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.TakeDamage))]
        public static void IlTakeDamage(ILContext il)
        {
            var curs = new ILCursor(il);
            curs.GotoNext(MoveType.Before, x => x.MatchLdsfld(typeof(RoR2Content.Items),nameof(RoR2Content.Items.NearbyDamageBonus)));
            int index = curs.Index;
            int num = -1;
            curs.GotoNext(x => x.MatchLdloc(out _));
            curs.GotoNext(x => x.MatchLdloc(out num));
            curs.Index = index;
            curs.Emit(OpCodes.Ldarg_0); // Body local? Emitting for delegate I think??
            curs.Emit(OpCodes.Ldarg_1);
            curs.Emit(OpCodes.Ldloc, num);
            curs.EmitDelegate<Func<HealthComponent, DamageInfo, float, float>>((self, info, amount) =>
            {
                var initialised = GetInstance<GolemsDeadeye>();
                CharacterBody attacker = info.attacker.GetComponent<CharacterBody>();
                if (!attacker) return amount;
                int itemStack = attacker.inventory.GetItemCount(initialised!.itemDef);
                if(itemStack > 0)
                {
                    int buffStack = self.body.GetBuffCount(DeadeyeEffect);
                    float percentBoost = initialised.baseDamage!.Value + (initialised.damagePerStack!.Value * itemStack - 1);
                    float finalBoost = percentBoost * buffStack;
                    //RoR2.Audio.EntitySoundManager.EmitSoundServer(DeadeyeEffectSound!.index, self.body.gameObject);
                    amount *= 1 + (finalBoost / 100);
                }
                return amount;
            });
            curs.Emit(OpCodes.Stloc, num);
        }

        #region Config Entries
        public ConfigEntry<float>? baseDamage;
        public ConfigEntry<float>? damagePerStack;
        public ConfigEntry<int>? buffCap;
        public ConfigEntry<int>? buffCapPerStack;
        protected override bool AddConfig()
        {

            baseDamage = Config.configFile!.Bind(configName, "Base Damage %", 5f, "How much damage is increased by the Deadeye debuff, with  1 stack.");
            damagePerStack = Config.configFile!.Bind(configName, "Damage % Per Stack", 2.5f, "How much damage is increased by the Deadeye debuff, for each additional stack.");
            buffCap = Config.configFile!.Bind(configName, "Max debuffs", 3, "The max number of Deadeye debuffs an enemy can have, using  1 stack.");
            buffCapPerStack = Config.configFile!.Bind(configName, "Extra debuffs per stack", 2, "The number of extra Deadeye debuffs an enemy can have, for each additional stack.");

            return base.AddConfig();
        }
        #endregion
        class GolemDeadeyeComponent : RoR2.Items.BaseItemBodyBehavior
        {
            [ItemDefAssociation(useOnServer = true, useOnClient = false)]
            private static ItemDef? GetItemDef()
            {
                return GetInstance<GolemsDeadeye>()?.itemDef;
            }
            HealthComponent? lastTarget;
            void OnEnable()
            {
                GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt;
            }
            void OnDestroy()
            {
                GlobalEventManager.onServerDamageDealt -= GlobalEventManager_onServerDamageDealt;
                if (lastTarget && lastTarget!.body) lastTarget!.body.SetBuffCount(DeadeyeEffect.buffIndex, 0);
            }

            private void GlobalEventManager_onServerDamageDealt(DamageReport rep)
            {
                if (rep.attackerBody != body) return;
                if(lastTarget != rep.victim)
                {
                    if(lastTarget && lastTarget!.body) lastTarget!.body.SetBuffCount(DeadeyeEffect.buffIndex, 0);
                    lastTarget = rep.victim;
                }
                if(lastTarget)
                {
                    int buffCount = lastTarget.body.GetBuffCount(DeadeyeEffect);
                    GolemsDeadeye? deadeyeBehavior = GetInstance<GolemsDeadeye>();

                    //IF WE USE THIS THING AGAIN, MAKE A HELPER METHOD! 
                    if (buffCount < deadeyeBehavior!.buffCap!.Value + (deadeyeBehavior.buffCapPerStack!.Value * (body.inventory.GetItemCount(GetItemDef()) - 1)))
                    {
                        lastTarget.body.AddBuff(DeadeyeEffect);
                    }
                }
            }
        }
    }
    
}
