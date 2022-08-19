using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using HarmonyLib;
using RoR2;
using RoR2.Items;
using StormSurge.Utils.ReferenceHelper;

namespace StormSurge.ItemBehaviour
{
    /// <summary>
    /// Behaviours for the Savior's Idol, an item that grants luck to players with low HP.
    /// </summary>
    class SaviorIdol : ItemBase
    {
        #region LoadedContent
        static InstRef<BuffDef> SaviorEffect = new
            (() => Assets.ContentPack.buffDefs.Find("SaviorEffect"));
        static InstRef<NetworkSoundEventDef> SaviorEffectSound = new
            (() => Assets.ContentPack.networkSoundEventDefs.Find("nseSaviorEffect"));
        #endregion

        protected override string itemDefName => "SaviorIdol";
        protected override string configName => "Savior Idol";
        
        //We use this method to subscribe to the global Inventory Change event.
        public override void AddItemBehaviour()
        {
            Inventory.onInventoryChangedGlobal += OnInvChanged;
        }

        //this code is run any time an inventory is updated; we check to see if this character has Savior's Idol, and
        //add this component if they do- otherwise, if they already have this component, then we remove it
        private void OnInvChanged(Inventory inv)
        {
            if (!inv.GetComponent<CharacterMaster>()) return;
            var itemComponent = inv.GetComponent<SaviorIdolBehaviour>();
            if (inv.GetItemCount(itemDef) > 0)
            {
                if(!itemComponent) itemComponent = inv.gameObject.AddComponent<SaviorIdolBehaviour>();
                itemComponent.stackCount = inv.GetItemCount(itemDef);
            }
            else if (itemComponent)
            {
                itemComponent.Destruct();
            }
        }
        #region Config Entries
        public static ConfigEntry<int>? healthThreshold;
        protected override bool AddConfig()
        {
            healthThreshold = Config.configFile.Bind(configName, "Health Threshold", 25, "the HP% below which Savior Idol can begin to activate.");
            return base.AddConfig();
        }
        #endregion
        /// <summary>
        /// The CharacterMaster behaviour that calculates luck for Savior's Idols
        /// </summary>
        public class SaviorIdolBehaviour : UnityEngine.MonoBehaviour
        {
            public int luckBonus = 0;
            public int stackCount;
            CharacterMaster? master;
            CharacterBody? body;
            HealthComponent? HP;

            void Start()
            {
                //initialise any values we use in our behaviour
                master = GetComponentInChildren<CharacterMaster>();
                ///UnityEngine.Debug.LogWarning($"STORMSURGE :: {name} HAS COMPONENT {master}");
                body = master.GetBody();
                HP = body.healthComponent;
            }

            //we use this function instead of just Destroy() to get rid of our component- otherwise, we might leave behind a 
            ////residual change to the player's luck.
            public void Destruct()
            {
                master!.luck -= luckBonus;
                Destroy(this);
            }

            float oldHealth;
            void FixedUpdate()
            {
                //check to see if our character's health has updated this frame; if not, we don't need to calculate on this frame
                if (oldHealth == HP!.health) return;

                //remove our current luck bonus, to be recalculated
                master!.luck -= luckBonus;

                oldHealth = HP.health;
                //calculate our character's current %Health
                float healthPercent = (healthThreshold!.Value - (HP.health * 100f / HP.fullHealth));

                //calculate the max luck that our Idol stack can give, then round it
                int idolLuck = (int)UnityEngine.Mathf.Ceil(UnityEngine.Mathf.Max(healthPercent * stackCount / healthThreshold.Value, 0));

                //cap our luck boost to be no more than our current item stack count
                int finalLuck = UnityEngine.Mathf.Min(stackCount, idolLuck);

                //don't bother updating buffs if we have the same luck value; this is just a small optimisation
                if (luckBonus == finalLuck) return;

                body?.SetBuffCount(((BuffDef) SaviorEffect).buffIndex, finalLuck);

                //unused as of now; play the Savior Idol sound event if our luck has increased on this update (HP has decreased)
                /*if(itemComponent.luckBonus < finalLuck)
                {
                    RoR2.Audio.EntitySoundManager.EmitSoundServer(SaviorEffectSound.index, __instance.gameObject);
                }*/

                //finalise our luck boost
                luckBonus = finalLuck;
                master.luck += luckBonus;
            }
        }

    }
}
