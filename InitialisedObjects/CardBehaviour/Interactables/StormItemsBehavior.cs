using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using StormSurge.Utils.ReferenceHelper;
using System;
using System.Collections.Generic;
using System.Linq;
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
        static InstReference<Xoroshiro128Plus> ItemSeed = new
            (() => new(Run.instance.seed));
        void Start()
        {
            var gObj = Instantiate(NetworkedInventoryPrefab);
            gObj.GetComponent<EnemyInfoPanelInventoryProvider>().enabled = true;
            StormItemInventory = gObj.GetComponent<Inventory>();

            if (CurrentFamily.familySelectionChatString == null) return;

            dropTable = FamilyPools.TryGetValue(CurrentFamily.familySelectionChatString, out dropTable) ? dropTable : FamilyPools["default"];
            var teleporterBoss = FindObjectsOfType<BossGroup>().Where((BossGroup group) => group.GetComponent<TeleporterInteraction>()).First();
            teleporterBoss.combatSquad.onMemberDefeatedServer += (CharacterMaster master, DamageReport report) =>
            {
                teleporterBoss.bossDropTables.Add(dropTable);
            };
            foreach (var entry in dropTable.pickupEntries)
            {
                if (entry.pickupDef is ItemDef)
                {
                    ItemDef ID = entry.pickupDef as ItemDef;
                    StormItemInventory.GiveItem(ID);
                    var pickupIndex = PickupCatalog.FindPickupIndex(ID.itemIndex);
                    teleporterBoss.bossDrops.Add(pickupIndex);
                    //Debug.LogWarning($"Added {((ItemDef) entry.pickupDef).name} ({(int)ID.itemIndex}) to teleporter drop pool");
                }
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

        private static Dictionary<string, ExplicitPickupDropTable> FamilyPools = new();

        [Tooltip("The family event => drop table list for storm events.")]
        public List<FamilyPool> familyPools;

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
        public static Inventory? StormItemInventory;
    }
}
