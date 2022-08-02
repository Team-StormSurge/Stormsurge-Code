using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace StormSurge.InitialisedObjects.CardBehaviour.Interactables
{
    public class StormItemsBehavior : NetworkBehaviour
    {
        void Start()
        {
            StormItemInventory = Instantiate(NetworkedInventoryPrefab).GetComponent<Inventory>();
            ExplicitPickupDropTable dropTable;
            dropTable = FamilyPools.TryGetValue(CurrentFamilyToken, out dropTable) ? dropTable : FamilyPools["default"];
            foreach(var entry in dropTable.pickupEntries)
            {
                if (entry.pickupDef is ItemDef)
                    StormItemInventory.GiveItem(entry.pickupDef as ItemDef);
            }
            foreach(var charMaster in FindObjectsOfType<CharacterMaster>())
            {
                if(charMaster.teamIndex == TeamIndex.Monster)
                {
                    charMaster.inventory.AddItemsFrom(StormItemInventory);
                }
            }
            SpawnCard.onSpawnedServerGlobal += SpawnHunterEnemy;
        }
        void Awake()
        {
            foreach (FamilyPool pool in familyPools)
            {
                FamilyPools!.Add(pool.key, pool.table);
            }
        }
        void OnDisable()
        {
            SpawnCard.onSpawnedServerGlobal -= SpawnHunterEnemy;
            Destroy(StormItemInventory.gameObject);
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
        public static string CurrentFamilyToken;
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
        private static int currentItemIterator = 0;
    }
}
