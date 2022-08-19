using System;
using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using StormSurge.Utils.ReferenceHelper;

namespace StormSurge.ItemBehaviour
{
    /// <summary>
    ///  Behaviours for the Pack Bond, an item that grants damage reduction to all nested allies.
    /// </summary>
    class PackBond : ItemBase
    {
        #region LoadedContent
        static InstRef<NetworkSoundEventDef> PackBondBlockSound = new
            (() => Assets.ContentPack.networkSoundEventDefs.Find("nsePackBondBlock"));
        #endregion
        protected override string itemDefName => "PackBond";
        protected override string configName => "Pack Bond";
        //not adding any Behaviour event listeners... hence this is empty (for now)
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

        //Our harmony patch for HealthComponent.TakeDamage, where we'll reduce damage if our boss/ally has any Pack Bonds
        [HarmonyILManipulator, HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.TakeDamage))]
        public static void IlTakeDamage(ILContext il)
        {
            //create our IL cursor
            var c = new ILCursor(il);
            //move to relevant code- in this, damage reduction done by Repulsion Armour Plate
            c.GotoNext(MoveType.Before, x => x.MatchLdfld<HealthComponent.ItemCounts>(nameof(HealthComponent.ItemCounts.armorPlate)));
            //save this behaviour's location.
            var where = c.Index;
            //move to the next local variable (current damage), and save its location.
            int num = -1;
            c.GotoNext(x => x.MatchLdloc(out num));
            //move back to our saved location.
            c.Index = where;
            c.Emit(OpCodes.Ldarg_0); // Emit the 'this' argument as our own argument.
            c.Emit(OpCodes.Ldloc, num); // Emit our saved local variable as an argument.
            //Consume our arguments in a new function, which modifies the damage value.
            c.EmitDelegate<Func<HealthComponent, float, float>>((self, amount) =>
            {
                //get the amount and instance of Pack Bonds in all allies (TODO, only nested allies)
                int itemCount = 0;
                PackBond? initialised;
                initialised = GetInstance<PackBond>();
                //tallies Pack Bond amounts for all allies
                foreach(TeamComponent tComp in TeamComponent.GetTeamMembers(self.body.teamComponent.teamIndex))
                {
                    var bod = tComp.body;
                    if (!bod.inventory) continue;
                    itemCount += bod.inventory.GetItemCount(initialised!.itemDef);
                }
                float damageReduc = itemCount * initialised!.armorPerAlly!.Value;
                amount = UnityEngine.Mathf.Max(1f, amount - damageReduc);
                //current unused; if damage has been reduced, play the Pack Bond sound event.
                //if(itemCount > 0) RoR2.Audio.EntitySoundManager.EmitSoundServer(PackBondBlockSound.Reference.index, self.body.gameObject);
                return amount; //returns the modified damage
            });
            c.Emit(OpCodes.Stloc, num); //saves damage dealt as our modified amount.
        }
    }
}
