using HarmonyLib;
using MonoMod.Cil;
using RoR2;
using StormSurge.Utils.ReferenceHelper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection.Emit;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace StormSurge.ItemBehaviour
{
	/// <summary>
	/// The patched class that we use to modify HealingWards
	//	NOT CURRENT WORKING!!
	/// </summary>
	public class patch_HealingWard : HealingWard
    {
		public CorruptHealComponent malignComponent;
		public extern void orig_Start();
		public void Start()
        {
			orig_Start();
			malignComponent = GetComponent<CorruptHealComponent>();
        }
	}
	/// <summary>
	///  Behaviours for the Malign Mushroom, an item that corrupts touched Healing AoEs to deal damage.
	/// </summary>
	class EvilShroom : ItemBase
    {
        protected override string itemDefName => "MalignMushroom";

        protected override string configName => "Malign Mushrum";

		//you know the drill by now, I hope- no subscribed events means empty method body, for now
        public override void AddItemBehaviour() { }

		/// <summary>
		/// method shortcut, deals direct damage to a HealthComponent target.
		/// </summary>
		/// <param name="toHurt">The character we will hurt</param>
		/// <param name="attacker">The attacking object responsible for hurting this character</param>
		/// <param name="inflictor">The owner of our Attacker object</param>
		/// <param name="damage">The amount of damage that this attack will deal</param>
		/// <param name="mask">The proc mask that may be used to prevent some items from Proc'ing off of this</param>
		/// <param name="pos">the position of this damage instance.</param>
		public static void DealDirectDamage(HealthComponent toHurt, GameObject attacker, GameObject inflictor, float damage, ProcChainMask mask, Vector3 pos)
        {
			//creates a DamageInfo struct for our attack
			DamageInfo damageInfo = new DamageInfo()
			{
				attacker = attacker,
				inflictor = inflictor,
				damage = damage,
				procChainMask = mask,
				damageType = DamageType.Generic,
				crit = false,
				force = Vector3.zero,
				//TODO CHANGE THIS TO A CONFIG VALUE
				procCoefficient = 0,
				damageColorIndex = DamageColorIndex.DeathMark,
				position = pos
			};
			//causes our target to take damage, according to our DamageInfo struct.
			toHurt.TakeDamage(damageInfo);

			//publishes Global Events to announce that something has been hit
			GlobalEventManager.instance.OnHitEnemy(damageInfo, toHurt.gameObject);
			GlobalEventManager.instance.OnHitAll(damageInfo, toHurt.gameObject);
		}
		/*[HarmonyILManipulator, HarmonyPatch(typeof(HealingWard),nameof(HealingWard.HealOccupants))]
		public static void ILHealOccupants(ILContext il)
        {
			var c = new ILCursor(il);
			//c.GotoNext(x => x.MatchCallvirt<ReadOnlyCollection<TeamComponent>>(AccessTools.DeclaredPropertyGetter()));

        }*/

		//Harmony patch for healing wards- runs after the original code finishes.
        [HarmonyPostfix, HarmonyPatch(typeof(HealingWard), nameof(HealingWard.HealOccupants))]
        public static void PatchHealOccupants(HealingWard __instance)
        {
			//this will work
			var mComp = __instance.GetComponent<CorruptHealComponent>(); //checks to find a Malign Mushroom component in our healing body.
			//this, however, will not. Monomod is no longer packaged with BepInEx, apparently??
			///var mComp = ((patch_HealingWard) __instance).malignComponent;
			float num = __instance.radius * __instance.radius; //the maximum square radius of our heal AoE.
			Vector3 position = __instance.transform.position; //the position of our heal AoE.
			for (TeamIndex teamIndex = TeamIndex.Neutral; teamIndex < TeamIndex.Count; teamIndex += 1) //loops for all team types
			{
				ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(teamIndex); //gets all members of the current team
				foreach (TeamComponent teamMember in teamMembers) //loops for each member in the team list
				{
					if ((teamMember.transform.position - position).sqrMagnitude <= num) //only runs if the team member is in our AoE range
					{
						HealthComponent hComp = teamMember.GetComponent<HealthComponent>(); //tries for a Health Component in this team member
						if (hComp)	//only runs if the Health Component exists
						{
							///Debug.LogWarning($"{teamIndex} vs {__instance.teamFilter.teamIndex}");
							if (teamIndex != __instance.teamFilter.teamIndex && mComp) //Only runs if this Health Component isn't on the HealZone team!
							{
								DealDirectDamage(hComp, mComp.owner.gameObject, __instance.gameObject, //deals direct damage to our target
								//TODO MAKE THIS CONTROLLED BY CONFIG - values are hard-coded right now
								(10 + (4 * mComp.stackSize)) * __instance.interval, default, hComp.transform.position);
							}
							else if(!mComp) //only runs if we don't have a Malign Mushroom component.
                            {
								CharacterBody charBod = hComp.body; //tries to find a CharacterBody
								///Debug.Log(charBod);
								if (charBod) //Only runs if we found a CharacterBody
                                {
									var evilShroomCount = charBod.inventory.GetItemCount(GetInstance<EvilShroom>().itemDef);
									if(evilShroomCount > 0) //only runs if we found Malign Mushrooms in the inventory of this character
                                    {
										//adds and instantiates a HealingDoesDamage component to the AoE zone.
										var corruptHeal = __instance.gameObject.AddComponent<CorruptHealComponent>();
										corruptHeal.stackSize = evilShroomCount;
										corruptHeal.owner = charBod;
									}
                                }
                            }
							
						}
					}
				}
			}
		}
		/// <summary>
		/// The ItemBodyBehaviour given to characters who have Malign Mushroom.
		/// </summary>
		class EvilShroomComponent : RoR2.Items.BaseItemBodyBehavior
		{
			//the healing ward created by Malign Mushrooms
			static InstRef<GameObject> wardEffect = new(() => Assets.AssetBundle.LoadAsset<GameObject>("EvilShroomWard.prefab"));
			[ItemDefAssociation(useOnServer = true, useOnClient = false)]
			private static ItemDef? GetItemDef()
			{
				return GetInstance<EvilShroom>()?.itemDef;
			}

			//the instance of this object's healing ward
			GameObject wardInstance;
			void OnEnable()
			{
				//instantiate and initialise our Mushrum ward.
				wardInstance ??= Instantiate((GameObject)wardEffect, transform);
				wardInstance.GetComponent<TeamFilter>().teamIndex = GetComponent<TeamComponent>().teamIndex;
				wardInstance.GetComponent<HealingWard>().radius = 4 + (1 * stack);
				AkSoundEngine.PostEvent("Play_item_proc_mushroom_start", gameObject);
				StartCoroutine(stopSound());
			}
			//destroy the healing ward if we lose Malign Mushroom.
			void OnDisable()
			{
				Destroy(wardInstance);
			}
			IEnumerator stopSound()
            {
				yield return new WaitForSeconds(1f);
				AkSoundEngine.PostEvent("Stop_item_proc_mushroom_loop", gameObject);

			}
		}
	}

	/// <summary>
	/// The component in charge of determining whether a Healing Ward will deal damage to enemies.
	/// </summary>
	[RequireComponent(typeof(TeamFilter))]
	public class CorruptHealComponent : MonoBehaviour
    {
		public float lerpTime = 3f;

		//the effects and materials related to corrupted healing zones.
		static InstRef<Material> evilShroomMat = new(() => Assets.AssetBundle.LoadAsset<Material>("matEvilShroomIndicator.mat"));
		static InstRef<GameObject> spawnEffect = new(() => LegacyResourcesAPI.Load<GameObject>("Prefabs/TemporaryVisualEffects/DeathMarkEffect"));
		///TeamFilter filter; do we need this??
		public CharacterBody owner; //the owner of this component
		public int stackSize; // the stack size of our Malign Mushroom at the time that we corrupted this field
		void Start()
		{
			//'corrupt' the material of this healing zone
			GetComponentInChildren<Renderer>().SetMaterial(evilShroomMat);
		}
    }
}
