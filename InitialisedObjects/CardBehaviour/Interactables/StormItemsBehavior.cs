using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static StormSurge.InitialisedObjects.ItemBehaviour.Equipment.Elites.AffixStorm;

namespace StormSurge.InitialisedObjects.CardBehaviour.Interactables
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
            Destroy(StormItemInventory!.gameObject);
        }

        static Dictionary<string, string> subtitles = new()
        {
            ["FAMILY_GOLEM"]        =      "SS_SUBTITLE_GOLEMFAMILY",
            ["FAMILY_JELLYFISH"]    =   "SS_SUBTITLE_JELLYFAMILY",
            ["FAMILY_WISP"]         =       "SS_SUBTITLE_WISPFAMILY",
            ["FAMILY_BEETLE"]       =     "SS_SUBTITLE_BEETLEFAMILY",
            ["FAMILY_IMP"]          =        "SS_SUBTITLE_IMPFAMILY",
            ["FAMILY_LEMURIAN"]     =   "SS_SUBTITLE_LEMFAMILY",
            ["FAMILY_PARENT"]       =     "SS_SUBTITLE_PARENTFAMILY",
            ["FAMILY_MUSHRUM"]      =    "SS_SUBTITLE_MUSHRUMFAMILY",
            ["FAMILY_LUNAR"]        =      "SS_SUBTITLE_LUNARFAMILY",
            ["FAMILY_ACIDLARVA"]    =  "SS_SUBTITLE_LARVAFAMILY",
            ["FAMILY_GUP"]          =        "SS_SUBTITLE_GUPFAMILY",
            ["FAMILY_CONSTRUCT"]    =  "SS_SUBTITLE_CONSTRUCTFAMILY",
            ["FAMILY_VOID"]         =       "SS_SUBTITLE_VOIDFAMILY",

        };
        public static string FindSubtitle()
        {
            string result;
            var success = subtitles.TryGetValue(CurrentFamilyToken, out result);
            return Language.GetString(success ? result : "NULL_SUBTITLE");
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
