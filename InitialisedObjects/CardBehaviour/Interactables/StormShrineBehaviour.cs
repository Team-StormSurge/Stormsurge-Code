using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RoR2.PostProcessing;
using RoR2.PostProcess;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Networking;
using HarmonyLib;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using static RoR2.ClassicStageInfo;
using Mono.Cecil;

namespace StormSurge.InitialisedObjects.CardBehaviour.Interactables
{
    public class StormShrineBehaviour : CardBase, ISceneVariant
    {
        #region Shrine Cards
        public static InteractableSpawnCard ShrineCard_Rain => Assets.AssetBundle.LoadAsset<InteractableSpawnCard>("iscShrineStorm.asset");
        public static InteractableSpawnCard ShrineCard_Ash => Assets.AssetBundle.LoadAsset<InteractableSpawnCard>("iscShrineStormFire.asset");
        public static InteractableSpawnCard ShrineCard_Snow => Assets.AssetBundle.LoadAsset<InteractableSpawnCard>("iscShrineStormSnow.asset");
        public static InteractableSpawnCard ShrineCard_Tar => Assets.AssetBundle.LoadAsset<InteractableSpawnCard>("iscShrineStormTar.asset");
        public static InteractableSpawnCard ShrineCard_Stars => Assets.AssetBundle.LoadAsset<InteractableSpawnCard>("iscShrineStormStars.asset");
        #endregion Shrine Cards
        protected override string configName => "Shrine of Storms";

        public string cardCategory => "Shrines";
        public Dictionary<string[], DirectorCard> SceneVariants => new Dictionary<string[], DirectorCard>()
        {
            [new string[]
            {
                "MAP_ANCIENTLOFT_TITLE",
                "MAP_BLACKBEACH_TITLE", 
                "MAP_GOLEMPLAINS_TITLE", 
                "MAP_FOGGYSWAMP_TITLE", 
                "MAP_SHIPGRAVEYARD_TITLE",
                "MAP_ROOTJUNGLE_TITLE",

            }] = new() 
            {
                spawnCard = ShrineCard_Rain,
                //DEBUG: SET COST 20,
                //DEBUG: SET WEIGHT ??
                //DEBUG: DEFAULT VALUE 1
                selectionWeight = 100,
                minimumStageCompletions = 1,
            },
            [new string[]
            {
                "MAP_DAMPCAVE_TITLE",
                "MAP_WISPGRAVEYARD_TITLE",
            }] = new()
            {
                spawnCard = ShrineCard_Ash,
                selectionWeight = 100,
                minimumStageCompletions = 1,
            },
            [new string[]
            {
                "MAP_GOOLAKE_TITLE",
            }] = new()
            {
                spawnCard = ShrineCard_Tar,
                selectionWeight = 100,
                minimumStageCompletions = 1,
            },
            [new string[]
            {
                "MAP_ARENA_TITLE",
                "MAP_SKYMEADOW_TITLE",
            }] = new()
            {
                spawnCard = ShrineCard_Stars,
                selectionWeight = 100,
                minimumStageCompletions = 1,
            },
            [new string[]
            {
                "MAP_FROZENWALL_TITLE",
                "MAP_SNOWYFOREST_TITLE",
            }] = new()
            {
                spawnCard = ShrineCard_Snow,
                selectionWeight = 100,
                minimumStageCompletions = 1,
            },
        };
        protected override void AddCard()
        {
            SceneDirector.onGenerateInteractableCardSelection += (this as ISceneVariant).AddVariantToDirector;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BossGroup),nameof(BossGroup.UpdateObservations))]
        public static void overrideSubtitle(BossGroup __instance, BossGroup.BossMemory memory)
        {
            string cloud = "< sprite name =\"CloudLeft\" tint=1>";
            __instance.bestObservedSubtitle = $"{cloud} {StormItemsBehavior.FindSubtitle()} {cloud}";
        }
        static TypeReference tRef;
        [HarmonyILManipulator, HarmonyPatch(typeof(ClassicStageInfo), nameof(ClassicStageInfo.RebuildCards))]
        public static void IlRebuildCards(ILContext il)
        {
            var curs = new ILCursor(il);
            curs.GotoNext(MoveType.Before, x => x.MatchCallvirt<DirectorCardCategorySelection>(nameof(DirectorCardCategorySelection.CopyFrom)));
            int i = -1;

            curs.GotoPrev(MoveType.After, x => x.MatchStloc(out i));

            curs.Emit(OpCodes.Ldloc, i);
            curs.EmitDelegate<Action<MonsterFamily>>(family =>
            {
                StormItemsBehavior.CurrentFamilyToken = family.familySelectionChatString;
                //UnityEngine.Debug.LogWarning($"STORMSURGE :: Family selection token is {StormItemsBehavior.CurrentFamilyToken}");
            });
            //Debug.LogWarning(il);
        }
        private void SceneDirector_onGenerateInteractableCardSelection(SceneDirector dir, DirectorCardCategorySelection selection)
        {
            DirectorCard dCard = new()
            {
            };
            int ind = Array.FindIndex(selection.categories, (item) => item.name.Equals("Shrines", StringComparison.OrdinalIgnoreCase));
            selection.AddCard(ind, dCard);
        }
    }
    [CreateAssetMenu(menuName = "Stormsurge/Storm Event")]
    public class StormEvent : UnityEngine.ScriptableObject
    {
        public string? startMessageToken;
        public GameObject? stormEffect;
        public GameObject? postProcessingVolume;
        public Material? StormMaterial;
    }

    [RequireComponent(typeof(CombatDirector), typeof(StormItemsBehavior))]
    public class ShrineStormBehavior : NetworkBehaviour
    {
        private float monsterCredit
        {
            get
            {
                return baseMonsterCredit * Stage.instance.entryDifficultyCoefficient;
            }
        }
        void Awake()
        {
            familyChanceBase = monsterFamilyChance;
        }
        #region Storm Inst
        [Server]
        public void AddShrineStack(Interactor interactor)
        {
            //BROADCAST STORM
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = stormEvent?.startMessageToken
            });
            ForceFamilyEvent();
            AddStormPostProcessing();
            AddStormWind();

            //TODO
            AddStormWetGround();
            AddStormParticles();

            //TODO
            AddStormElites(interactor);

        }
        void AddStormParticles()
        {
            if(stormEvent?.stormEffect)
            {
                EffectManager.SpawnEffect(stormEvent?.stormEffect, new()
                {
                    origin = transform.position
                }, NetworkServer.active);
            }
        }
        void AddStormWetGround()
        {
            Debug.LogWarning($"STORMSURGE [WET GROUND] not been implemented!");
        }
        void AddStormWind()
        {
            var wind = FindObjectsOfType<WindZone>().FirstOrDefault();
            if (!wind) return;
            wind.windMain *= 4;
            wind.windPulseMagnitude *= 2;
            wind.windPulseFrequency *= 1.5f;
            wind.windTurbulence *= 1.5f;
        }
        void AddStormPostProcessing()
        {
            var rainPP = Instantiate(stormEvent?.postProcessingVolume);
            rainPP.GetComponent<PostProcessVolume>().priority = ppVolume.priority + 1;
            NetworkServer.Spawn(rainPP);
            //Debug.LogWarning($"STORMSURGE: post process has original priority of {ppVolume.priority}");
        }
        void ForceFamilyEvent()
        {

            monsterFamilyChance = 1f;
            instance.monsterDccsPool = null;
            foreach (CombatDirector director in CombatDirector.instancesList)
            {
                director.monsterCardsSelection = null;
            }
            instance.RebuildCards();
        }
        #endregion Storm Inst
        private void Start()
        {
            if (!NetworkServer.active) return;
            Debug.LogWarning($"STORMSURGE : Shrine {gameObject.name} loaded!!");
            ppVolume = FindObjectsOfType<PostProcessVolume>().Where(x => x.isGlobal == true).ToArray().First();
            combatDirector = GetComponent<CombatDirector>();
        }
        void OnDestroy()
        {
            monsterFamilyChance = familyChanceBase;
        }
        void AddStormElites(Interactor interactor)
        {
            combatDirector!.enabled = true;
            combatDirector.monsterCredit += monsterCredit;
            var combatCard = combatDirector.SelectMonsterCardForCombatShrine(monsterCredit);
            //Debug.LogWarning($"STORMSURGE:: combat card is {combatCard}, monster is {combatCard.spawnCard.prefab.name}");
            combatDirector.OverrideCurrentMonsterCard(combatCard);
            var eDef = Assets.ContentPack.eliteDefs.Find("edStorm");
            //Debug.LogWarning($"Setting Storm elites to def {eDef}");
            combatDirector.currentActiveEliteDef = eDef;
            combatDirector.monsterSpawnTimer = 0f;
            //CharacterMaster component = chosenDirectorCard.spawnCard.prefab.GetComponent<CharacterMaster>();
            //if (component)
            //{
            //    CharacterBody component2 = component.bodyPrefab.GetComponent<CharacterBody>();
            //    if (component2)
            //    {
            //        Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
            //        {
            //            subjectAsCharacterBody = interactor.GetComponent<CharacterBody>(),
            //            baseToken = "SHRINE_STORM_COMBAT_MESSAGE",
            //            paramTokens = new string[]
            //            {
            //                component2.baseNameToken
            //            }
            //        });
            //    }
            //}
        }


        private CombatDirector? combatDirector;
        PostProcessVolume ppVolume;

        float familyChanceBase;

        public float baseMonsterCredit;
        public StormEvent? stormEvent;
    }
}
