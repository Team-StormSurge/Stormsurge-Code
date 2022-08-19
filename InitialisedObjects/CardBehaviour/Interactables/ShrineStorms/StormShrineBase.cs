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
using static StormSurge.Interactables.StormShrineBase;

namespace StormSurge.Interactables
{
    /// <summary>
    /// the InteractableBehaviour that controls the Shrine of Storms, a rare shrine that activates unique stage-specific events.
    /// </summary>
    public class StormShrineBase : CardBase, ISceneVariant
    {
        protected override string configName => "Shrine of Storms";
        public string[] cardCategories => new string[]{"Shrines", "VoidStuff"};

        public SceneVariantList SceneVariants => Assets.AssetBundle.LoadAsset<SceneVariantList>("ShrineStormVariants.asset");
        protected override void AddCard()
        {
            //add listener to Scene Director to pass our card selection
            SceneDirector.onGenerateInteractableCardSelection += (this as ISceneVariant).AddVariantToDirector;
        }
        //list of subtitles for Storming Elite boss bars--
        //TODO make this into a separate file/object for code brevity! 
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
        /// <summary>
        /// Tries to find a subtitle replacement based on the active Family Event
        /// </summary>
        /// <param name="result">the string modified to match our Subtitle result, if successful</param>
        /// <returns>whether or not a subtitle was successfully found</returns>
        public static bool FindSubtitle(out string result)
        {
            result = "";
            var success = subtitles.TryGetValue(StormShrineBase.ActiveFamily.selectionChatString, out result);
            ///Debug.LogWarning($"Current family token is {CurrentFamilyToken}; Success = {success}; result is {result}; final value is {final}");
            if (success)
            {
                result = Language.GetString(result);
            }
            return success;
        }
        /// <summary>
        /// Finds whether a given CharacterBody is included in our family event.
        /// </summary>
        /// <param name="body">The body we are checking for a match.</param>
        /// <returns>whether the active Family Event contains the given body.</returns>
        static bool FamilyHasBody(CharacterBody body)
        {
            var selection = ActiveFamily;
            if (selection?.categories == default) return false; //skips this code if no family is active, or if it has no selection categories
            foreach(Category category in selection.categories)
            {
                foreach(DirectorCard card in category.cards) //loops for all monster cards in our family event
                {
                    var comparisonMaster = card.spawnCard.prefab.GetComponentInChildren<CharacterMaster>();
                    var master = body?.master;
                    if (!comparisonMaster || !master) continue;
                    ///Debug.LogWarning($"Comparing master indices {(int)comparisonMaster.masterIndex} ({comparisonMaster.name}) & {(int)master!.masterIndex} ({master.name})");
                    if ((int) comparisonMaster.masterIndex == (int) master.masterIndex)
                    {
                        ///Debug.LogWarning($"{master.name} will be a Storming Elite!");
                        return true; //returns true if we find a match
                    }
                }
            }
            return false; //returns false as last resort
        }

        public delegate string RebuildSubtitle(string str, BossGroup group, ref BossMemory memory);

        /* our Harmony IL patch for BossGroup.UpdateObservations, used to replace the subtitles on boss bars
        *  I'm lazy, so I'm not doing a walkthrough of this code- if you need to learn more about IL hooking, 
        *  check our other shit, or just ask me*/
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
                ///Debug.LogWarning(str);
                if (!ShrineStormBehavior.stormActive || !FamilyHasBody(memory.cachedBody)) return str;
                string result = "";
                var success = FindSubtitle(out result);
                if (!success) return str;

                return result + " ";
            }); /// was curs.Next.Operand = mRef;

            ///Debug.LogWarning($"\n{il}");



        }

        /* Our Harmony IL Patch for VisionLimitEffect.UpdateCommandBuffer, used to replace the default black colour with our storm's custom
        *  colour */
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

        /// <summary>
        /// Replaces the given CategorySelection with a family Event version, if possible
        /// </summary>
        /// <param name="oldSelection">The default card selection originally used on this stage.</param>
        /// <param name="randomNext">the seed used for random number generation</param>
        /// <param name="info">the current ClassicStageInfo in this run</param>
        /// <returns>A new Family Event card selection if found, otherwise, oldSelection</returns>
        public static DirectorCardCategorySelection evalFamily(DirectorCardCategorySelection oldSelection, float randomNext, ClassicStageInfo info)
        {
            ///Debug.LogWarning($"Method has selection {oldSelection}, float {randomNext}, and info {info}");
            ///Debug.LogWarning("Rebuilding cards, checking if storm is active");
            if (ShrineStormBehavior.stormActive) //only run this code if a storm is active
            {
                ///Debug.LogWarning("Storm is active! Finding Family dccs category!"); 
                var selection = new WeightedSelection<DirectorCardCategorySelection>(8); //generate a new selection from our card selection
                //grabs any options that are considered family pools
                var options = info.monsterDccsPool.poolCategories.Where((category) => category.name.Equals("Family", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().includedIfConditionsMet;
                foreach (var option in options) //loops for all options in our Family Pool list
                {
                    selection.AddChoice(option.dccs, option.weight);
                }
                var eval = selection?.Evaluate(randomNext); //evaluates for a random choice of Family Event
                if (eval && eval is FamilyDirectorCardCategorySelection) //only runs if we were able to evaluate a Family Card Selection
                {
                    var final = eval as FamilyDirectorCardCategorySelection;
                    if (final != null)
                    {
                        ActiveFamily = final;
                        return final;
                    }
                }
                ///Debug.LogError($"Could not find family pool! Returning to non-storm behaviours!");
            }
            ///Debug.LogWarning("Not storming- returning default selection instead! :: " + oldSelection);
            return oldSelection; //return oldSelection if no family could be generated
        }

        //Harmony IL patch for ClassicStageInfo.RebuildCards, to force a family event if a storm is active
        [HarmonyILManipulator, HarmonyPatch(typeof(ClassicStageInfo), nameof(ClassicStageInfo.RebuildCards))]
        public static void ILRebuildCards(ILContext il)
        {
            var curs = new ILCursor(il);
            curs.GotoNext(x => x.MatchLdfld<ClassicStageInfo>(nameof(ClassicStageInfo.monsterDccsPool)));
            curs.GotoNext(x => x.MatchCallvirt(AccessTools.DeclaredPropertyGetter(typeof(Xoroshiro128Plus),
                nameof(Xoroshiro128Plus.nextNormalizedFloat))));
            var prev = curs.Prev;
            var next = curs.Next;
            var method = AccessTools.DeclaredMethod(typeof(WeightedSelection<DirectorCardCategorySelection>),
                nameof(WeightedSelection<DirectorCard>.Evaluate));
            curs.GotoNext(MoveType.After, x =>
                x.MatchCallvirt(method));
            curs.Emit(prev.OpCode, prev.Operand);
            curs.Emit(next.OpCode, next.Operand);
            curs.Emit(OpCodes.Ldarg_0);
            curs.Emit(OpCodes.Call, AccessTools.DeclaredMethod(typeof(StormShrineBase),nameof(StormShrineBase.evalFamily)));
            ///Debug.LogWarning(il + "\n\n");
        }

        //Harmony IL patch for CategorySelection.IsAvailable, to remove stage completion limits on a selected family event
        [HarmonyILManipulator, HarmonyPatch(typeof(FamilyDirectorCardCategorySelection),nameof(FamilyDirectorCardCategorySelection.IsAvailable))]
        public static void ILIsAvailable(ILContext il)
        {
            var curs = new ILCursor(il);

            curs.GotoNext(MoveType.After, x => x.MatchLdfld<FamilyDirectorCardCategorySelection>(nameof(FamilyDirectorCardCategorySelection.minimumStageCompletion)));
            curs.EmitDelegate<Func<int, int>>(val => 0);

            curs.GotoNext(MoveType.After, x => x.MatchLdfld<FamilyDirectorCardCategorySelection>(nameof(FamilyDirectorCardCategorySelection.maximumStageCompletion)));
            curs.EmitDelegate<Func<int, int>>(val => int.MaxValue);

        }
        //OLD FAMILY EVENT PATCH; OBSOLETE
        /*[HarmonyILManipulator, HarmonyPatch(typeof(ClassicStageInfo), nameof(ClassicStageInfo.RebuildCards))]
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
                StormShrineBase.ActiveFamily = family;
                //UnityEngine.Debug.LogWarning($"STORMSURGE :: Family selection token is {StormShrineBase.ActiveFamilyToken}");
            });
            //Debug.LogWarning(il);
        }*/
        
        //event listener for card collection on stage load, used to add Shrine of Storms to stage
        private void SceneDirector_onGenerateInteractableCardSelection(SceneDirector dir, DirectorCardCategorySelection selection)
        {
            DirectorCard dCard = new();
            int ind = Array.FindIndex(selection.categories, (item) => item.name.Equals("Shrines", StringComparison.OrdinalIgnoreCase));
            selection.AddCard(ind, dCard);
        }
        static InstRef<EquipmentDef> StormingDef = new
            (() => Assets.ContentPack.equipmentDefs.Find("edAffixStorming"));

        //event listener for enemy spawning ingame, used to force Storming Elites during storms
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

        public static FamilyDirectorCardCategorySelection ActiveFamily; //the current Active Family 
    }
}
