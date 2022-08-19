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
using static StormSurge.Interactables.StormShrineBase;

namespace StormSurge.Interactables
{
    /// <summary>
    /// The behaviour that determines how item pools are loaded for the Shrine of Storms
    /// </summary>
    public class StormItemsBehavior : NetworkBehaviour
    {
        static ExplicitPickupDropTable dropTable; //the active drop table for this storm
        static InstRef<Xoroshiro128Plus> ItemSeed = new
            (() => new(Run.instance.seed)); //the seed for all drop tables; when instantiated, will use the current run as a seed
        void Start()
        {
            GameObject gObj = Instantiate<GameObject>(NetworkedInventoryPrefab); //instantiates a networked inventory to grant Storm enemies.
            gObj.GetComponent<EnemyInfoPanelInventoryProvider>().enabled = true; //enables an inventory to display all monster items.
            StormItemInventory = gObj.GetComponent<Inventory>(); //loads the inventory of our networked inventory prefab.

            if (ActiveFamily == null) //stops this behaviour if no family event could be loaded previously
            {
                enabled = false;
                return;
            }

            // tries to find the active family's drop table; returns a default otherwise
            dropTable = FamilyPools.TryGetValue(ActiveFamily.selectionChatString, out dropTable) ? dropTable : FamilyPools["default"];
            // finds the teleporter's boss group to modify
            var teleporterBoss = FindObjectsOfType<BossGroup>().Where((BossGroup group) => group.GetComponent<TeleporterInteraction>()).First();
            
            //modified to add Hunter Items to the teleporter when boss dies.
            teleporterBoss.combatSquad.onMemberDefeatedServer += (CharacterMaster master, DamageReport report) =>
            {
                teleporterBoss.bossDropTables.Add(dropTable);
            };

            foreach (var entry in dropTable.pickupEntries) //get ItemDefs from our drop table, and add them to boss drops and our Storm inventory
            {
                if (entry.pickupDef is ItemDef)
                {
                    ItemDef ID = entry.pickupDef as ItemDef;
                    StormItemInventory.GiveItem(ID);
                    var pickupIndex = PickupCatalog.FindPickupIndex(ID.itemIndex);
                    teleporterBoss.bossDrops.Add(pickupIndex);
                    ///Debug.LogWarning($"Added {((ItemDef) entry.pickupDef).name} ({(int)ID.itemIndex}) to teleporter drop pool");
                }
            }
            
            foreach(var charMaster in FindObjectsOfType<CharacterMaster>()) //grants storm items to all living enemies
            {
                if(charMaster.teamIndex == TeamIndex.Monster)
                {
                    if(charMaster.GetBody() && charMaster.GetComponent<HealthComponent>())
                    {
                        charMaster.inventory.AddItemsFrom(StormItemInventory);
                    }
                }
            }

            //subscribes to events we need to modify
            SpawnCard.onSpawnedServerGlobal += SpawnHunterEnemy;
            GlobalEventManager.onCharacterDeathGlobal += CheckStormingItem;
        }
        void Awake()
        {
            if(FamilyPools.Count == 0) //converts our drop table list from a Unity Object to a useable dictionary
            {
                foreach (FamilyPool pool in familyPools)
                {
                    FamilyPools!.Add(pool.key, pool.table);
                }
            }
        }
        void OnDisable()
        {
            //unsubscribe from all events, and destroy our Storm inventory
            SpawnCard.onSpawnedServerGlobal -= SpawnHunterEnemy;
            GlobalEventManager.onCharacterDeathGlobal -= CheckStormingItem;
            Destroy(StormItemInventory!.gameObject);
        }

        private static void CheckStormingItem(DamageReport damageReport) //event listener, drops a Hunter Item if killed enemies were Storming Elites.
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
        private static void SpawnHunterEnemy(SpawnCard.SpawnResult result) //gives spawned enemy hunter items if they are a Monster, in a storm.
        {
            var charMaster = result.spawnedInstance?.GetComponent<CharacterMaster>();
            if (charMaster && charMaster!.teamIndex == TeamIndex.Monster)
            {
                charMaster.inventory.AddItemsFrom(StormItemInventory);
            }
        }

        //dictionary of family event => drop table list for storm events.
        private static Dictionary<string, ExplicitPickupDropTable> FamilyPools = new();

        [Tooltip("The family event => drop table list for storm events.")]
        public List<FamilyPool> familyPools;

        /// <summary>
        /// the Pool object used to correlate family event names with Drop Tables.
        /// </summary>
        [System.Serializable]
        public struct FamilyPool
        {
            public string key;
            public ExplicitPickupDropTable table;
        }

        //our network inventory prefab and instanced Inventory object.
        static InstRef<GameObject> NetworkedInventoryPrefab = new(() => LegacyResourcesAPI.Load<GameObject>
                    ("Prefabs/NetworkedObjects/MonsterTeamGainsItemsArtifactInventory"));
        public static Inventory? StormItemInventory;
    }
}
