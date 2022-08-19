using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering.PostProcessing;
using static StormSurge.Interactables.StormShrineBase;
using static RoR2.ClassicStageInfo;
using StormSurge.Utils.ReferenceHelper;
using RoR2.Audio;

namespace StormSurge.Interactables
{
    /// <summary>
    /// The behaviour which controls the activation and spawning of Storm events.
    /// </summary>
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

        #region Unity Methods
        void Awake()
        {
            familyChanceBase = monsterFamilyChance;
        }
        private void Start()
        {
            if (!NetworkServer.active) return; //skip this code if we aren't Server Authority!
            ///Debug.LogWarning($"STORMSURGE : Shrine {gameObject.name} loaded!!");
            // initialise script values
            ppVolume = FindObjectsOfType<PostProcessVolume>().Where(x => x.isGlobal == true).ToArray().First();
            combatDirector = GetComponent<CombatDirector>();
            SpawnCard.onSpawnedServerGlobal += StormShrineBase.OnSpawnedServerGlobal; // subscribe to server monster spawns
            ///GetComponent<CombatSquad>().onMemberAddedServer += (CharacterMaster master) => {Debug.LogWarning($"COMBAT SQUAD ADDED {master.GetBody().name}");
        }
        void OnDisable()
        {
            stormActive = false; //disable our storm event and behaviours
            activeEvent = null; //reset the active storm event
            AkSoundEngine.PostEvent("stop_ShrineThunderLoop", gameObject);
        }
        #endregion

        #region Storm Inst
        [Server]
        public void AddShrineStack(Interactor interactor) //used when a Shrine is successfully interacted
        {
            stormActive = true; //enables storm behaviours, etc
            activeEvent = stormEvent; //sets the active storm event
            PointSoundManager.EmitSoundServer(shrineStartSound.Reference.index, transform.position);
            AkSoundEngine.PostEvent("start_ShrineThunderLoop", gameObject);
            //FORCE FAMILY EVENT TO START
            ForceFamilyEvent(); //forces the stage to generate a family event from MonsterdccsPool
            if (ActiveFamily == null) //only run if ForceFamilyEvent() has failed
            {
                Debug.LogError($"No Family event found! This is either a hidden realm, or ya fucked up big-time, cuz!");
                return;
            }

            //broadcast our storm warning in chat
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = stormEvent?.startMessageToken
            });

            //Add PP (fog, blindness, etc), and wind
            AddStormPostProcessing();
            AddStormWind();
            AddVisionLimit();

            AddStormWetGround();
            AddStormParticles();

            //Spawn storming elite squad
            AddStormElites(interactor);

        }
        /// <summary>
        /// lowers the base vision distance of characters to create fog
        /// </summary>
        void AddVisionLimit()
        {

            var bodies = FindObjectsOfType<CharacterBody>();
            foreach (CharacterBody body in bodies)
            {
                body.baseVisionDistance = stormEvent.fogDistance;
            }
        }
        /// <summary>
        /// generates WeatherParticles effects to simulate rain, etc.
        /// </summary>
        void AddStormParticles()
        {
            if (stormEvent?.stormEffect)
            {
                EffectManager.SpawnEffect(stormEvent?.stormEffect, new()
                {
                    origin = transform.position
                }, NetworkServer.active);
            }
        }
        /// <summary>
        /// Adds wet ground material to the stage, to simulate runoff
        /// </summary>
        void AddStormWetGround()
        {
            Debug.LogWarning($"STORMSURGE [WET GROUND] not been implemented!");
        }
        /// <summary>
        /// Modifies stage's wind values to simulate strong wind
        /// </summary>
        void AddStormWind()
        {
            var wind = FindObjectsOfType<WindZone>().FirstOrDefault();
            if (!wind) return;
            wind.windMain *= 4;
            wind.windPulseMagnitude *= 2;
            wind.windPulseFrequency *= 1.5f;
            wind.windTurbulence *= 1.5f;
        }
        /// <summary>
        /// generates a post-processing volume to override global default for this stage
        /// </summary>
        void AddStormPostProcessing()
        {
            if (stormEvent == null) return;
            var rainPP = Instantiate(stormEvent?.postProcessingVolume);
            rainPP.GetComponent<PostProcessVolume>().priority = ppVolume.priority + 1;
            NetworkServer.Spawn(rainPP);
            //Debug.LogWarning($"STORMSURGE: post process has original priority of {ppVolume.priority}");
        }
        /// <summary>
        /// Sets ClassicStageInfo's monster family chance to 100% (OBSOLETE) and rebuilds the combat director to generate a new family event
        /// </summary>
        void ForceFamilyEvent()
        {

            monsterFamilyChance = 100f;
            foreach (CombatDirector director in CombatDirector.instancesList)
            {
                director.monsterCardsSelection = null;
            }
            instance.RebuildCards();
        }
        /// <summary>
        /// Enables our combat squad director and builds a squad of Storming elites
        /// </summary>
        /// <param name="interactor"></param>
        void AddStormElites(Interactor interactor)
        {
            combatDirector!.enabled = true;
            combatDirector.monsterCredit += monsterCredit;
            var eDef = Assets.ContentPack.eliteDefs.Find("edStorm");
            combatDirector.currentActiveEliteDef = eDef;
            combatDirector.monsterSpawnTimer = 0f;
        }
        #endregion Storm Inst



        //random script values
        static InstRef<NetworkSoundEventDef> shrineStartSound = new(() => Assets.ContentPack.networkSoundEventDefs.Find("nseShrineStormActivation"));
        private CombatDirector? combatDirector;
        PostProcessVolume ppVolume;

        float familyChanceBase;

        public static bool stormActive = false;
        public static StormEvent activeEvent;

        [Tooltip("The credits spent on spawning a Storming Wave.")]
        public float baseMonsterCredit;

        [Tooltip("The StormEvent object activated when this shrine is used.")]
        public StormEvent? stormEvent;
    }
}
