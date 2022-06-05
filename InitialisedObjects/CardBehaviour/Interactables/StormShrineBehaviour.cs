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
using static StormSurge.Utils.LanguageProvider;
using HarmonyLib;

namespace StormSurge.InitialisedObjects.CardBehaviour.Interactables
{
    public class StormShrineBehaviour : CardBase, ISceneVariant
    {
        #region Shrine Cards
        public static InteractableSpawnCard ShrineCard_Rain => Assets.AssetBundle.LoadAsset<InteractableSpawnCard>("iscShrineStorm.asset");
        public static InteractableSpawnCard ShrineCard_Ash => Assets.AssetBundle.LoadAsset<InteractableSpawnCard>("iscShrineStorm.asset");
        public static InteractableSpawnCard ShrineCard_Snow => Assets.AssetBundle.LoadAsset<InteractableSpawnCard>("iscShrineStorm.asset");
        public static InteractableSpawnCard ShrineCard_Tar => Assets.AssetBundle.LoadAsset<InteractableSpawnCard>("iscShrineStorm.asset");
        public static InteractableSpawnCard ShrineCard_Stars => Assets.AssetBundle.LoadAsset<InteractableSpawnCard>("iscShrineStorm.asset");
        #endregion Shrine Cards
        protected override string configName => "Shrine of Storms";

        public string cardCategory => "Shrines";
        public Dictionary<string[], DirectorCard> SceneVariants => new Dictionary<string[], DirectorCard>()
        {
            [new string[]
            {
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

        const string PREFIX = "SHRINE_STORM";
        protected override void AddCard()
        {
            AddLanguage();
            SceneDirector.onGenerateInteractableCardSelection += (this as ISceneVariant).AddVariantToDirector;
        }
        void AddLanguage()
        {
            new LanguagePair($"{PREFIX}_NAME",$"Shrine of Storms");
            new LanguagePair($"{PREFIX}_CONTEXT", $"Desecrate the Shrine of Storms");
            new LanguagePair($"{PREFIX}_STARTRAIN", $"<style=cShrine>Wicked rain sweeps in...</style>");
            new LanguagePair($"{PREFIX}_STARTTAR", $"<style=cShrine>Vile tar condenses...</style>");
            new LanguagePair($"{PREFIX}_STARTASH", $"<style=cShrine>Volcanic ash spews from below...</style>");
            new LanguagePair($"{PREFIX}_STARTSNOW", $"<style=cShrine>Freezing snow fills the air...</style>");
            new LanguagePair($"{PREFIX}_STARTSTARS", $"<style=cShrine>Stars whistle through the sky...</style>");
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
        //not sure if we'll need this
        public GameObject DecalObject;
    }
    public class ShrineStormBehavior : NetworkBehaviour
    {
        PostProcessVolume ppVolume;

        public StormEvent? stormEvent;
        float familyChanceBase;

        void Awake()
        {
            familyChanceBase = ClassicStageInfo.monsterFamilyChance;
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

            //TODO
            AddStormWind();

            //TODO
            AddStormWetGround();

            //TODO
            AddStormParticles();

            //TODO
            AddStormElites();

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
        void AddStormElites()
        {
            Debug.LogWarning($"STORMSURGE [STORM ELITES] not yet been implemented!");
        }
        void AddStormWetGround()
        {
            Debug.LogWarning($"STORMSURGE [WET GROUND] not yet been implemented!");
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
            Debug.LogWarning($"STORMSURGE: post process has original priority of {ppVolume.priority}");
        }
        void ForceFamilyEvent()
        {

            ClassicStageInfo.monsterFamilyChance = 1f;
            ClassicStageInfo.instance.monsterDccsPool = null;
            foreach (CombatDirector director in CombatDirector.instancesList)
            {
                director.monsterCardsSelection = null;
            }
            ClassicStageInfo.instance.RebuildCards();
        }
        #endregion Storm Inst
        private void Start()
        {
            if (!NetworkServer.active) return;
            ppVolume = FindObjectsOfType<PostProcessVolume>().Where(x => x.isGlobal == true).ToArray().First();

        }
        void OnDestroy()
        {
            ClassicStageInfo.monsterFamilyChance = familyChanceBase;
        }
    }
}
