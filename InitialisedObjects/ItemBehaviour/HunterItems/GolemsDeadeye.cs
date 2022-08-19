using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using StormSurge.Utils.ReferenceHelper;

namespace StormSurge.ItemBehaviour
{
    /// <summary>
    /// Behaviours for the Golem's Deadeye, an item that grants damage boosts to consecutive hits.
    /// </summary>
    class GolemsDeadeye : ItemBase
    {
        //loaded content region
        #region LoadedContent
        //definition for Deadeye Mark debuff
        static InstRef<BuffDef> DeadeyeEffect = new
            (() => Assets.ContentPack.buffDefs.Find("DeadeyeEffect"));
        //definition for Deadeye Proc Sound Event
        static InstRef<NetworkSoundEventDef> DeadeyeEffectSound = new
            (() => Assets.ContentPack.networkSoundEventDefs.Find("nseGolemsDeadeyeEffect"));
        #endregion
        protected override string itemDefName => "GolemsDeadeye";
        protected override string configName => "Golem Deadeye";
        //not adding any Behaviour event listeners... hence this is empty (for now)
        public override void AddItemBehaviour()
        {}

        //IL Hook for HealthComponent.TakeDamage, used to scale damage based on Deadeye Marks
        [HarmonyILManipulator, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.TakeDamage))]
        public static void IlTakeDamage(ILContext il)
        {
            //create an IL cursor
            var curs = new ILCursor(il);
            //move our cursor to a relevant section (in this case, damage boosts around 'NearbyDamageBonus': Focus Crystal)
            curs.GotoNext(MoveType.Before, x => x.MatchLdsfld(typeof(RoR2Content.Items),nameof(RoR2Content.Items.NearbyDamageBonus)));
            //save the position of this code
            int index = curs.Index;

            //find the second local variable from here- save its location as num
            int num = -1;
            curs.GotoNext(x => x.MatchLdloc(out _));
            curs.GotoNext(x => x.MatchLdloc(out num));
            //move back to our saved location
            curs.Index = index;
            //emit the 'this' variable as an argument
            curs.Emit(OpCodes.Ldarg_0);
            //emit the second local argument (a DamageInfo struct) in TakeDamage as an argument
            curs.Emit(OpCodes.Ldarg_1);
            //emit our saved local variable as an argument
            curs.Emit(OpCodes.Ldloc, num);
            //consume our three arguments in a function
            curs.EmitDelegate<Func<HealthComponent, DamageInfo, float, float>>((self, info, amount) =>
            {
                //get an instance of our behaviour
                var initialised = GetInstance<GolemsDeadeye>();
                //get the CharacterBody component of whoever dealt this damage
                CharacterBody attacker = info.attacker.GetComponent<CharacterBody>();
                //if we can't find an attacker CharacterBody, don't modify damage
                if (!attacker) return amount;
                //find out how many Golem Deadeyes the attacker has, to calculate damage bonus
                int itemStack = attacker.inventory.GetItemCount(initialised!.itemDef);
                if(itemStack > 0)
                {
                    //calculate a damage boost for how many Deadeye Marks this target has
                    int buffStack = self.body.GetBuffCount(DeadeyeEffect);
                    float percentBoost = initialised.baseDamage!.Value + (initialised.damagePerStack!.Value * itemStack - 1);
                    float finalBoost = percentBoost * buffStack;
                    //emit our Deadeye sound event
                    //RoR2.Audio.EntitySoundManager.EmitSoundServer(DeadeyeEffectSound!.index, self.body.gameObject);
                    amount *= 1 + (finalBoost / 100);
                }
                return amount;
            });
            //return the saved version of our modified damage amount
            curs.Emit(OpCodes.Stloc, num);
        }

        #region Config Entries
        public ConfigEntry<float>? baseDamage;
        public ConfigEntry<float>? damagePerStack;
        public ConfigEntry<int>? buffCap;
        public ConfigEntry<int>? buffCapPerStack;
        //Initialise Config Entries for this behaviour
        protected override bool AddConfig()
        {

            baseDamage = Config.configFile!.Bind(configName, "Base Damage %", 5f, "How much damage is increased by the Deadeye debuff, with  1 stack.");
            damagePerStack = Config.configFile!.Bind(configName, "Damage % Per Stack", 2.5f, "How much damage is increased by the Deadeye debuff, for each additional stack.");
            buffCap = Config.configFile!.Bind(configName, "Max debuffs", 3, "The max number of Deadeye debuffs an enemy can have, using  1 stack.");
            buffCapPerStack = Config.configFile!.Bind(configName, "Extra debuffs per stack", 2, "The number of extra Deadeye debuffs an enemy can have, for each additional stack.");

            return base.AddConfig();
        }
        #endregion
        /// <summary>
        /// The item behaviour placed on CharacterBodies holding Golem's Deadeye
        /// </summary>
        class GolemDeadeyeComponent : RoR2.Items.BaseItemBodyBehavior
        {
            //the itemDef this is associated with
            [ItemDefAssociation(useOnServer = true, useOnClient = false)]
            private static ItemDef? GetItemDef()
            {
                return GetInstance<GolemsDeadeye>()?.itemDef;
            }
            //the active target of the Deadeye Mark effect
            HealthComponent? lastTarget;
            //behaviour when this component is first enabled
            void OnEnable()
            {
                //add event listener for damage being dealt
                GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt;
            }
            //the behaviour for when this component is removed
            void OnDestroy()
            {
                //remove damage-dealt event listener
                GlobalEventManager.onServerDamageDealt -= GlobalEventManager_onServerDamageDealt;
                //remove any residual Deadeye Mark debuffs
                if (lastTarget && lastTarget!.body) lastTarget!.body.SetBuffCount(((BuffDef) DeadeyeEffect).buffIndex, 0);
            }
            //the event hook for whenever damage is dealt
            private void GlobalEventManager_onServerDamageDealt(DamageReport rep)
            {
                //return if we didn't cause this damage: i.e. the damageReport's attacker body does not equal the component's body
                if (rep.attackerBody != body) return;
                //remove any Deadeye Marks on old enemies
                if(lastTarget != rep.victim)
                {
                    if(lastTarget && lastTarget!.body) lastTarget!.body.SetBuffCount(((BuffDef) DeadeyeEffect).buffIndex, 0);
                    lastTarget = rep.victim;
                }
                //increment debuff, up to cap, if this is a consecutive attack
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
