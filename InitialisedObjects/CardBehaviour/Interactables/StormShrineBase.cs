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
using StormSurge.Utils;
using static RoR2.DirectorCardCategorySelection;
using static RoR2.BossGroup;
using StormSurge.Utils.ReferenceHelper;

namespace StormSurge.Interactables
{
    public class StormShrineBase : CardBase, ISceneVariant
    {
        protected override string configName => "Shrine of Storms";
        public string cardCategory => "Shrines";

        public SceneVariantList SceneVariants => Assets.AssetBundle.LoadAsset<SceneVariantList>("ShrineStormVariants.asset");
        protected override void AddCard()
        {
            SceneDirector.onGenerateInteractableCardSelection += (this as ISceneVariant).AddVariantToDirector;
        }
        static Dictionary<string, string> subtitles = new()
        {
            ["FAMILY_GOLEM"] = "SS_SUBTITLE_GOLEMFAMILY",
            ["FAMILY_JELLYFISH"] = "SS_SUBTITLE_JELLYFAMILY",
            ["FAMILY_WISP"] = "SS_SUBTITLE_WISPFAMILY",
            ["FAMILY_BEETLE"] = "SS_SUBTITLE_BEETLEFAMILY",
            ["FAMILY_IMP"] = "SS_SUBTITLE_IMPFAMILY",
            ["FAMILY_LEMURIAN"] = "SS_SUBTITLE_LEMFAMILY",
            ["FAMILY_PARENT"] = "SS_SUBTITLE_PARENTFAMILY",
            ["FAMILY_MUSHRUM"] = "SS_SUBTITLE_MUSHRUMFAMILY",
            ["FAMILY_LUNAR"] = "SS_SUBTITLE_LUNARFAMILY",
            ["FAMILY_ACIDLARVA"] = "SS_SUBTITLE_LARVAFAMILY",
            ["FAMILY_GUP"] = "SS_SUBTITLE_GUPFAMILY",
            ["FAMILY_CONSTRUCT"] = "SS_SUBTITLE_CONSTRUCTFAMILY",
            ["FAMILY_VOID"] = "SS_SUBTITLE_VOIDFAMILY",

        };
        public static bool FindSubtitle(out string result)
        {
            result = "";
            var success = subtitles.TryGetValue(StormItemsBehavior.CurrentFamily.familySelectionChatString, out result);
            //Debug.LogWarning($"Current family token is {CurrentFamilyToken}; Success = {success}; result is {result}; final value is {final}");
            if (success)
            {
                result = Language.GetString(result);
            }
            return success;
        }
        static bool FamilyHasBody(CharacterBody body)
        {
            var selection = StormItemsBehavior.CurrentFamily.monsterFamilyCategories;
            foreach(Category category in selection.categories)
            {
                foreach(DirectorCard card in category.cards)
                {
                    var comparisonMaster = card.spawnCard.prefab.GetComponentInChildren<CharacterMaster>();
                    var master = body?.master;
                    if (!comparisonMaster || !master) continue;
                    //Debug.LogWarning($"Comparing master indices {(int)comparisonMaster.masterIndex} ({comparisonMaster.name}) & {(int)master!.masterIndex} ({master.name})");
                    if ((int) comparisonMaster.masterIndex == (int) master.masterIndex)
                    {
                        //Debug.LogWarning($"{master.name} will be a Storming Elite!");
                        return true;
                    }
                }
            }
            return false;
        }

        public delegate string RebuildSubtitle(string str, BossGroup group, ref BossMemory memory);
        [HarmonyILManipulator, HarmonyPatch(typeof(BossGroup), nameof(BossGroup.UpdateObservations))]
        public static void ILOverrideSubtitle(ILContext il)
        {
            var curs = new ILCursor(il);

            curs.GotoNext(MoveType.Before, x => x.MatchCallvirt(AccessTools.DeclaredPropertyGetter(typeof(CharacterBody), 
                nameof(CharacterBody.healthComponent))));

            curs.GotoPrev(MoveType.After, x => x.MatchCall(AccessTools.DeclaredPropertyGetter(typeof(BossGroup), 
                nameof(BossGroup.bestObservedSubtitle))));

            curs.Emit(OpCodes.Ldarg_0);
            curs.Emit(OpCodes.Ldarg_1);

            curs.EmitDelegate<RebuildSubtitle>((string str, BossGroup group, ref BossMemory memory) =>
            {
                Debug.LogWarning(str);
                if (!ShrineStormBehavior.stormActive || !FamilyHasBody(memory.cachedBody)) return str;
                string result = "";
                var success = FindSubtitle(out result);
                if (!success) return str;

                return result + " ";
            }); // was curs.Next.Operand = mRef;

            //Debug.LogWarning($"\n{il}");



        }
        [HarmonyILManipulator, HarmonyPatch(typeof(VisionLimitEffect), nameof(VisionLimitEffect.UpdateCommandBuffer))]
        public static void ChangeBlindColor(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(x => x.MatchCallOrCallvirt<Material>(nameof(Material.SetColor)));
            c.EmitDelegate<Func<Color, Color>>(color =>
            {
                if (ShrineStormBehavior.stormActive) // storm enabled;
                    return ShrineStormBehavior.activeEvent.fogColour;
                return color;
            });
        }
        [HarmonyILManipulator, HarmonyPatch(typeof(ClassicStageInfo), nameof(ClassicStageInfo.RebuildCards))]
        public static void ILRebuildCards(ILContext il)
        {
            var curs = new ILCursor(il);
            curs.GotoNext(MoveType.After, x => x.MatchLdfld<MonsterFamily>(nameof(MonsterFamily.minimumStageCompletion)));
            curs.EmitDelegate<Action<int>>(_ => { });
            curs.Emit(OpCodes.Ldc_I4_0);

            curs.GotoNext(MoveType.Before, x => x.MatchCallvirt<DirectorCardCategorySelection>(nameof(DirectorCardCategorySelection.CopyFrom)));
            int i = -1;

            curs.GotoPrev(MoveType.After, x => x.MatchStloc(out i));

            curs.Emit(OpCodes.Ldloc, i);
            curs.EmitDelegate((MonsterFamily family) =>
            {
                StormItemsBehavior.CurrentFamily = family;
                //UnityEngine.Debug.LogWarning($"STORMSURGE :: Family selection token is {StormItemsBehavior.CurrentFamilyToken}");
            });
            //Debug.LogWarning(il);
        }
        private void SceneDirector_onGenerateInteractableCardSelection(SceneDirector dir, DirectorCardCategorySelection selection)
        {
            DirectorCard dCard = new();
            int ind = Array.FindIndex(selection.categories, (item) => item.name.Equals("Shrines", StringComparison.OrdinalIgnoreCase));
            selection.AddCard(ind, dCard);
        }
        static InstReference<EquipmentDef> StormingDef = new
            (() => Assets.ContentPack.equipmentDefs.Find("edAffixStorming"));
        public static void OnSpawnedServerGlobal(SpawnCard.SpawnResult result)
        {
            //Debug.LogWarning($"Spawned instance is {result.spawnedInstance}");
            if (!result.success || !ShrineStormBehavior.stormActive) return;

            var master = result.spawnedInstance.GetComponent<CharacterMaster>();
            if (!master) return;
            //Debug.LogWarning($"Spawned instance master is {master}");
            var body = master.GetBody();
            //Debug.LogWarning($"Spawned instance body is {body}");

            if (master && body.isBoss && FamilyHasBody(body))
            {
                //Debug.LogWarning($"Setting {body.GetDisplayName()} to storming elite");
                //var ed = Assets.ContentPack.equipmentDefs.Find("edAffixStorming");
                //Debug.LogWarning($"Equipment index for {ed.name} is {ed.equipmentIndex}");
                body.inventory.SetEquipmentIndex(((EquipmentDef)StormingDef).equipmentIndex) ;
            }
        }
    }
    [CreateAssetMenu(menuName = "Stormsurge/Storm Event")]
    public class StormEvent : UnityEngine.ScriptableObject
    {
        [Header("Main Properties")]

        [Tooltip("The biome-specific token for the storm event.")]
        public string? startMessageToken;

        [Tooltip("The Particle Systems created.")]
        public GameObject? stormEffect;

        [Tooltip("The PP Volume added.")]
        public GameObject? postProcessingVolume;

        [Tooltip("Defines the storm's wet-ground material.")][Obsolete]
        public Material? StormMaterial;

        [Space(height: 2)]
        [Header("Fog Properties")]

        [Tooltip("Controls the colour of the blindness effect.")]
        public Color fogColour;

        [Tooltip("The distance at which blindness fog renders.")]
        public int fogDistance;
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

        #region Unity Methods
        void Awake()
        {
            familyChanceBase = monsterFamilyChance;
        }
        private void Start()
        {
            if (!NetworkServer.active) return;
            //Debug.LogWarning($"STORMSURGE : Shrine {gameObject.name} loaded!!");
            ppVolume = FindObjectsOfType<PostProcessVolume>().Where(x => x.isGlobal == true).ToArray().First();
            combatDirector = GetComponent<CombatDirector>();
            SpawnCard.onSpawnedServerGlobal += StormShrineBase.OnSpawnedServerGlobal;
            /*GetComponent<CombatSquad>().onMemberAddedServer += (CharacterMaster master) =>
            {
                Debug.LogWarning($"COMBAT SQUAD ADDED {master.GetBody().name}");
            };*/
        }
        void OnDestroy()
        {
            monsterFamilyChance = familyChanceBase;
            stormActive = false;
            activeEvent = null;
        }
        #endregion

        #region Storm Inst
        [Server]
        public void AddShrineStack(Interactor interactor)
        {
            stormActive = true;
            activeEvent = stormEvent;

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
            AddVisionLimit();

        }
        void AddVisionLimit()
        {   if (stormEvent == null) return;
            var bodies = FindObjectsOfType<CharacterBody>();
            foreach(CharacterBody body in bodies)
            {
                body.baseVisionDistance = stormEvent.fogDistance;
            }
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

            monsterFamilyChance = 100f;
            instance.monsterDccsPool = null;
            foreach (CombatDirector director in CombatDirector.instancesList)
            {
                director.monsterCardsSelection = null;
            }
            instance.RebuildCards();
        }
        void AddStormElites(Interactor interactor)
        {
            combatDirector!.enabled = true;
            combatDirector.monsterCredit += monsterCredit;
            /*var combatCard = combatDirector.SelectMonsterCardForCombatShrine(monsterCredit);
            Debug.LogWarning($"STORMSURGE:: combat card is {combatCard}, monster is {combatCard.spawnCard.prefab.name}");
            combatDirector.OverrideCurrentMonsterCard(combatCard);*/
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
        #endregion Storm Inst




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
