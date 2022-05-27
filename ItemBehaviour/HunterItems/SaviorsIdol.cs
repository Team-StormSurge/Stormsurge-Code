using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RoR2;
using RoR2.Items;
using static StormSurge.Utils.LanguageProvider;

namespace StormSurge.ItemBehaviour
{
    class SaviorsIdol : ItemBase
    {
        #region LoadedContent
        static BuffDef? _saviorEffect;
        static BuffDef SaviorEffect
        {
            get
            {
                _saviorEffect ??= Assets.ContentPack.buffDefs.Find("SaviorEffect");
                return _saviorEffect;
            }
        }

        static NetworkSoundEventDef? _saviorEffectSound;
        static NetworkSoundEventDef SaviorEffectSound
        {
            get
            {
                _saviorEffectSound ??= Assets.ContentPack.networkSoundEventDefs.Find("nseSaviorEffect");
                return _saviorEffectSound;
            }
        }
        #endregion
        static string prefix = "ITEM_HUNTER_" + "SAVIORIDOL";
        protected override ItemLanguage lang => new()
        {
            nameToken = new LanguagePair($"{prefix}_NAME", "Savior Idol"),
            pickupToken = new LanguagePair($"{prefix}_PICKUP", "In peril, find clarity."),
            descToken = new LanguagePair($"{prefix}_DESC", "placeholder"),
            loreToken = new LanguagePair($"{prefix}_LORE", "placeholder"),
        };

        protected override string itemDefName => "SaviorIdol";
        public override void AddItemBehaviour()
        {
            Inventory.onInventoryChangedGlobal += OnInvChanged;
        }

        private void OnInvChanged(Inventory inv)
        {
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
        public class SaviorIdolBehaviour : UnityEngine.MonoBehaviour
        {
            public int luckBonus = 0;
            public int stackCount;
            CharacterMaster? master;
            CharacterBody? body;
            HealthComponent? HP;

            void Start()
            {
                master = GetComponent<CharacterMaster>();
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
                master!.luck -= luckBonus;

                if (oldHealth == HP!.health) return;

                oldHealth = HP.health;
                float healthPercent = (25 - (HP.health * 100f / HP.fullHealth));

                int idolLuck = (int)UnityEngine.Mathf.Ceil(UnityEngine.Mathf.Max(healthPercent * stackCount / 25f, 0));

                int finalLuck = UnityEngine.Mathf.Min(stackCount, idolLuck);

                if (luckBonus == finalLuck) return;

                body?.SetBuffCount(SaviorEffect.buffIndex, finalLuck);

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
