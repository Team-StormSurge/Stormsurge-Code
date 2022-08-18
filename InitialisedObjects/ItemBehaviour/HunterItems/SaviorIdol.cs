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
        public override void AddItemBehaviour()
        {
            Inventory.onInventoryChangedGlobal += OnInvChanged;
        }

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
        public class SaviorIdolBehaviour : UnityEngine.MonoBehaviour
        {
            public int luckBonus = 0;
            public int stackCount;
            CharacterMaster? master;
            CharacterBody? body;
            HealthComponent? HP;

            void Start()
            {
                master = GetComponentInChildren<CharacterMaster>();
                //UnityEngine.Debug.LogWarning($"STORMSURGE :: {name} HAS COMPONENT {master}");
                body = master.GetBody();
                HP = body.healthComponent;
            }

            public void Destruct()
            {
                master!.luck -= luckBonus;
                Destroy(this);
            }

            float oldHealth;
            void FixedUpdate()
            {

                if (oldHealth == HP!.health) return;

                master!.luck -= luckBonus;

                oldHealth = HP.health;
                float healthPercent = (healthThreshold!.Value - (HP.health * 100f / HP.fullHealth));

                int idolLuck = (int)UnityEngine.Mathf.Ceil(UnityEngine.Mathf.Max(healthPercent * stackCount / healthThreshold.Value, 0));

                int finalLuck = UnityEngine.Mathf.Min(stackCount, idolLuck);

                if (luckBonus == finalLuck) return;

                body?.SetBuffCount(SaviorEffect.Reference.buffIndex, finalLuck);

                /*if(itemComponent.luckBonus < finalLuck)
                {
                    RoR2.Audio.EntitySoundManager.EmitSoundServer(SaviorEffectSound.index, __instance.gameObject);
                }*/


                luckBonus = finalLuck;
                master.luck += luckBonus;
            }
        }

    }
}
