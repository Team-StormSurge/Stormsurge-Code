using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static RoR2.ClassicStageInfo;
using static StormSurge.Equipment.Elites.AffixStorm;

namespace StormSurge.Interactables
{
    public class StormItemsBehavior : NetworkBehaviour
    {
        static ExplicitPickupDropTable dropTable;
        static Xoroshiro128Plus _stormItemSeed;
        static Xoroshiro128Plus ItemSeed
        {
            get
            {
                _stormItemSeed ??= new(Run.instance.seed);
                return _stormItemSeed;
            }
        }
        void Start()
        {
            var gObj = Instantiate(NetworkedInventoryPrefab);
            gObj.GetComponent<EnemyInfoPanelInventoryProvider>().enabled = true;
            StormItemInventory = gObj.GetComponent<Inventory>();
            dropTable = FamilyPools.TryGetValue(CurrentFamily.familySelectionChatString, out dropTable) ? dropTable : FamilyPools["default"];
            foreach(var entry in dropTable.pickupEntries)
            {
                if (entry.pickupDef is ItemDef)
                    StormItemInventory.GiveItem(entry.pickupDef as ItemDef);
            }
            foreach(var charMaster in FindObjectsOfType<CharacterMaster>())
            {
                if(charMaster.teamIndex == TeamIndex.Monster)
                {
                    if(charMaster.GetBody() && charMaster.GetComponent<HealthComponent>())
                    {
                        charMaster.inventory.AddItemsFrom(StormItemInventory);
                    }
                }
            }


            SpawnCard.onSpawnedServerGlobal += SpawnHunterEnemy;
            GlobalEventManager.onCharacterDeathGlobal += CheckStormingItem;
        }
        void Awake()
        {
            if(FamilyPools.Count == 0)
            {
                foreach (FamilyPool pool in familyPools)
                {
                    FamilyPools!.Add(pool.key, pool.table);
                }
            }
        }
        void OnDisable()
        {
            SpawnCard.onSpawnedServerGlobal -= SpawnHunterEnemy;
            GlobalEventManager.onCharacterDeathGlobal -= CheckStormingItem;
            CurrentFamily = default;
            Destroy(StormItemInventory!.gameObject);
        }

        private static void CheckStormingItem(DamageReport damageReport)
        {
            if (!damageReport.victimMaster) return;
            if (damageReport.attackerTeamIndex == damageReport.victimTeamIndex && damageReport.victimMaster.minionOwnership.ownerMaster) return;
            if(damageReport.victimBody.GetComponent<StormAffixComponent>())
            {
                PickupIndex pickupIndex = dropTable.GenerateDrop(ItemSeed);
                if (pickupIndex != PickupIndex.none)
                {   
                    PickupDropletController.CreatePickupDroplet(pickupIndex, damageReport.victimBody.corePosition, Vector3.up * 20f);
                }
            }
        }
        private static void SpawnHunterEnemy(SpawnCard.SpawnResult result)
        {
            var charMaster = result.spawnedInstance?.GetComponent<CharacterMaster>();
            if (charMaster && charMaster!.teamIndex == TeamIndex.Monster)
            {
                charMaster.inventory.AddItemsFrom(StormItemInventory);
            }
        }

        private static Dictionary<string, ExplicitPickupDropTable>? FamilyPools = new();
        public List<FamilyPool> familyPools = new();
        [System.Serializable]
        public struct FamilyPool
        {
            public string key;
            public ExplicitPickupDropTable table;
        }
        public static MonsterFamily CurrentFamily;
        private static GameObject? _networkedInventoryPrefab;
        private static GameObject NetworkedInventoryPrefab
        {
            get
            {
                _networkedInventoryPrefab ??= LegacyResourcesAPI.Load<GameObject>
                    ("Prefabs/NetworkedObjects/MonsterTeamGainsItemsArtifactInventory");
                return _networkedInventoryPrefab;
            }
        }
        private static Inventory? StormItemInventory;
    }
}
