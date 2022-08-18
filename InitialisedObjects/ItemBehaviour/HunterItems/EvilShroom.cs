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
    class EvilShroom : ItemBase
    {
        protected override string itemDefName => "EvilShroom";

        protected override string configName => "Malign Mushrum";

        public override void AddItemBehaviour() { }

		public static void DealDirectDamage(HealthComponent toHurt, GameObject attacker, GameObject inflictor, float damage, ProcChainMask mask, Vector3 pos)
        {
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
				damageColorIndex = DamageColorIndex.Bleed,
				position = pos
			};
			toHurt.TakeDamage(damageInfo);
			GlobalEventManager.instance.OnHitEnemy(damageInfo, toHurt.gameObject);
			GlobalEventManager.instance.OnHitAll(damageInfo, toHurt.gameObject);
		}
		[HarmonyILManipulator, HarmonyPatch(typeof(HealingWard),nameof(HealingWard.HealOccupants))]
		public static void ILHealOccupants(ILContext il)
        {
			var c = new ILCursor(il);
			//c.GotoNext(x => x.MatchCallvirt<ReadOnlyCollection<TeamComponent>>(AccessTools.DeclaredPropertyGetter()));

        }
        [HarmonyPostfix, HarmonyPatch(typeof(HealingWard), nameof(HealingWard.HealOccupants))]
        public static void PatchHealOccupants(HealingWard __instance)
        {
			//this will work
			var mComp = __instance.GetComponent<CorruptHealComponent>();
			//this, however, will not. How does one access a patch-time field?
			//var mComp = ((patch_HealingWard) __instance).malignComponent;
			float num = __instance.radius * __instance.radius;
			Vector3 position = __instance.transform.position; 
			for (TeamIndex teamIndex = TeamIndex.Neutral; teamIndex < TeamIndex.Count; teamIndex += 1)
			{
				ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(teamIndex);
				foreach (TeamComponent teamMember in teamMembers)
				{
					if ((teamMember.transform.position - position).sqrMagnitude <= num)
					{
						HealthComponent hComp = teamMember.GetComponent<HealthComponent>();
						if (hComp)	
						{
							//Debug.LogWarning($"{teamIndex} vs {__instance.teamFilter.teamIndex}");
							if (teamIndex != __instance.teamFilter.teamIndex && mComp)
							{
								DealDirectDamage(hComp, mComp.owner.gameObject, __instance.gameObject,
								//TODO MAKE THIS CONTROLLED BY CONFIG
								(10 + (4 * mComp.stackSize)) * __instance.interval, default, hComp.transform.position);
							}
							else if(!mComp)
                            {
								CharacterBody charBod = hComp.body;
								//Debug.Log(charBod);
								if (charBod)
                                {
									var evilShroomCount = charBod.inventory.GetItemCount(GetInstance<EvilShroom>().itemDef);
									if(evilShroomCount > 0)
                                    {
										var corruptHeal = __instance.gameObject.AddComponent<CorruptHealComponent>();
										corruptHeal.stackSize = evilShroomCount;
										corruptHeal.owner = charBod;
									}
                                }
                            }
							
						}
					}
				}
				//this won't work; not sure how to access a patch-time field
			}
		}
		class EvilShroomComponent : RoR2.Items.BaseItemBodyBehavior
		{
			static InstRef<GameObject> wardEffect = new(() => Assets.AssetBundle.LoadAsset<GameObject>("EvilShroomWard.prefab"));
			[ItemDefAssociation(useOnServer = true, useOnClient = false)]
			private static ItemDef? GetItemDef()
			{
				return GetInstance<EvilShroom>()?.itemDef;
			}

			GameObject wardInstance;
			void OnEnable()
			{
				wardInstance ??= Instantiate((GameObject)wardEffect, transform);
				wardInstance.GetComponent<TeamFilter>().teamIndex = GetComponent<TeamComponent>().teamIndex;
				wardInstance.GetComponent<HealingWard>().radius = 4 + (1 * stack);
			}
			void OnDestroy()
			{
				Destroy(wardInstance);
			}
		}
	}
	[RequireComponent(typeof(TeamFilter))]
	public class CorruptHealComponent : MonoBehaviour
    {
		public float lerpTime = 3f;

		InstRef<Material> evilShroomMat = new(() => Assets.AssetBundle.LoadAsset<Material>("matEvilShroomIndicator.mat"));
		InstRef<GameObject> spawnEffect = new(() => LegacyResourcesAPI.Load<GameObject>("Prefabs/TemporaryVisualEffects/DeathMarkEffect"));
		//TeamFilter filter; do we need this??
		public CharacterBody owner;
		public int stackSize;
		void Start()
		{
			GetComponentInChildren<Renderer>().SetMaterial(evilShroomMat);
		}
    }
}
