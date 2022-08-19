using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Audio;
using StormSurge.Utils.ReferenceHelper;

namespace StormSurge.ItemBehaviour
{
    /// <summary>
    ///  Behaviours for the Wicked Rake, an item that gives you Nerd Rage when your shield is destroyed.
    /// </summary>
    public class WickedRake : ItemBase
    {
        protected override string itemDefName => "WickedRake";

        protected override string configName => "Wicked Rake";

        //yada-yada, no event listeners, no behaviours- for now
        public override void AddItemBehaviour()
        {}

        //the buff effect used by Wicked Rake
        #region Loaded Content
        static InstRef<BuffDef> WickedRageBuff = new(() => Assets.ContentPack.buffDefs.Find("bdWickedRage"));
        static InstRef<NetworkSoundEventDef> WickedRageSound = new(() => Assets.ContentPack.networkSoundEventDefs.Find("nseWickedRage"));
        #endregion Loaded Content

        //the Harmony Patch used in CharacterBody.Recalculate stats- this currently boosts Attack Speed if Wicked Rage is active.
        //TODO replace this to affect Critical Chance instead
        [HarmonyILManipulator, HarmonyPatch(typeof(CharacterBody),nameof(CharacterBody.RecalculateStats))]
        public static void ILRecalcStats(ILContext il)
        {
            //create a new cursor
            var c = new ILCursor(il);
            //move to relevant in this code- in this case, the definition of our base attack speed
            c.GotoNext(MoveType.Before, x => x.MatchLdfld(typeof(CharacterBody),nameof(CharacterBody.baseAttackSpeed)));
            var where = c.Index; //save this position
            int num = -1;

            //move to the next local variable (in this case, current Attack Speed)
            c.GotoNext(x => x.MatchLdloc(out num));
            c.Index = where;
            c.Emit(OpCodes.Ldarg_0); // emit the 'this' variable as an argument.
            c.Emit(OpCodes.Ldloc, num); // emit our local variable at position 'num' as an argument.
            c.EmitDelegate<Func<CharacterBody, float, float>>((self, amount) => //consume our arguments in this method
            {
                //if we have the WickedRage buff, boost our attack speed based on how many Wicked Rakes we have.
                if (!self.inventory) return amount;
                int itemCount = self.inventory.GetItemCount(GetInstance<WickedRake>()?.itemDef);
                if(self.HasBuff((BuffDef) WickedRageBuff))
                {
                    amount += 0.5f + (0.15f * (itemCount - 1));
                }

                //return our modified attack speed
                return amount;
            });
            c.Emit(OpCodes.Stloc, num); //save our modified attack speed value
        }

        //Harmony patch for HealthComponent.TakeDamage; very simple, just gives us WickedRage if our shield breaks
        [HarmonyPostfix,HarmonyPatch(typeof(HealthComponent),nameof(HealthComponent.TakeDamage))]
        public static void PatchTakeDamage(HealthComponent __instance) //runs after TakeDamage
        {
            if(__instance.shield <= 0) //only runs if our shield has run out; TODO only run if this happened on this tick!! 
            {
                var body = __instance.body;
                var itemCount = body.inventory.GetItemCount(GetInstance<WickedRake>()?.itemDef); //get our Wicked Rake stack size
                if (itemCount > 0)
                {
                    body.AddTimedBuff(WickedRageBuff, 4 + (1 * (itemCount - 1)), 1); //adds Wicked Rage by its effect def
                    EntitySoundManager.EmitSoundServer(WickedRageSound.Reference.index, __instance.gameObject);
                }
            }
        }
    }
}
