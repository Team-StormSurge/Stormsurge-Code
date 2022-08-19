using StormSurge.ItemBehaviour;
using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RoR2;
using UnityEngine;
using System.Collections;
using System.Linq;
using RoR2.Orbs;
using UnityEngine.Networking;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using StormSurge.Utils.ReferenceHelper;

namespace StormSurge.Equipment.Elites
{
    /// <summary>
    /// Behaviours for the Overlord's Descent, the Aspect of Storming Elites.
    /// </summary>
    [HarmonyPatch]
    public class AffixStorm : EquipBase
    {
        protected override string equipDefName => "edAffixStorming";
        protected override string configName => "Storming Aspect";

        //don't need to subscribe to any events, so this is empty for now
        public override void AddEquipBehavior(){ }

        //TODO REPLACE THIS!! This doesn't work with headhunter effects, so we need to check for the buff instead
        #region Add Affix Behaviours
        //Harmony postfix to run after OnEquipmentGained- adds our Storm Affix Component if they picked up Overlord's Descent.
        [HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.OnEquipmentGained))]
        public static void EquipmentBehaviorPatch(CharacterBody __instance, EquipmentDef equipmentDef)
        {
            if (equipmentDef != GetInstance<AffixStorm>()!.equipDef) return; //skips this code if they did not pick up Overlord's Descent
            __instance.gameObject.AddComponent<StormAffixComponent>(); //add our Storm Affix monobehaviour

            //spawns a lightning strike VFX to signify Aspect pickup
            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/LightningStrikeImpact"), new EffectData
            {
                origin = __instance.gameObject.transform.position
            }, true);

        }

        // Harmony postfix to run after OnEquipmentLost - removes our Storm Affix Component if they dropped Overlord's Descent.
        [HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.OnEquipmentLost))]
        public static void EquipmentLostPatch(CharacterBody __instance, EquipmentDef equipmentDef)
        {
            if (equipmentDef != GetInstance<AffixStorm>()!.equipDef) return; //skips this code if we did not drop Overlord's Descent
            UnityEngine.Object.Destroy(__instance.gameObject.GetComponent<StormAffixComponent>()); //destroys our Storm Affix monobehaviour

        }
        #endregion Add Affix Behaviours

        static InstRef<BuffDef> StuporBuff = new(() => Assets.ContentPack.buffDefs.Find("bdStupor"));

        // Harmony postfix for RecalculateStats- drains our stats if we're affected by the Stupor / Drowned debuff
        [HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
        public static void StuporBuffPatch(CharacterBody __instance)
        {
            if(__instance.HasBuff(StuporBuff))
            {
                // reduce damage dealt by debuff stack
                __instance.damage *= Mathf.Min((1 - (0.05f * __instance.GetBuffCount(StuporBuff))),0.7f); 
                // reduce sprinting speed by debuff stack
                __instance.sprintingSpeedMultiplier *= Mathf.Min((1 - (0.05f * __instance.GetBuffCount(StuporBuff))),0.7f);
            }
        }

        /// <summary>
        /// The item behaviour placed on CharacterBodies using Overlord's Descent
        /// </summary>
        public class StormAffixComponent : MonoBehaviour
        {
            //the component used to search for missile targets
            private readonly BullseyeSearch search = new BullseyeSearch();

            //the Buff Ward that inflicts Stupor / Drowning on nearby combatants
            public static InstRef<GameObject> stuporWard = new(() => Assets.AssetBundle.LoadAsset<GameObject>("StuporField.prefab"));

            public GameObject? stuporInstance;
            //the timed method responsible for generating Storm missiles
            Coroutine? lightningRoutine;
            //the refresh rate between Storm Missile firing
            float duration = 3;
            CharacterBody body;
            void Start() //instantiate our stored values
            {
                body = GetComponent<CharacterBody>();
                stuporInstance = Instantiate((GameObject) stuporWard, gameObject.transform);
                stuporInstance!.GetComponent<TeamFilter>().teamIndex = body.teamComponent.teamIndex;
                //Debug.LogWarning($"STORMSURGE :: instantiated {stuporInstance}");
                lightningRoutine = StartCoroutine(TickLightning(duration));
            }
            void OnDestroy() //remove this component and its Stupor Field once we're done with it
            {
                Destroy(stuporInstance);
            }
            void OnDisable() //stop our lightning missiles if this component is disabled
            {
                StopCoroutine(lightningRoutine);
            }
            void FixedUpdate()
            {
                if(canFire && GetComponent<HealthComponent>().alive)
                {
                    canFire = false;
                    GenerateSearch();
                    if (target)
                    {
                        FireBolt();
                    }
                }
            }

            void FireBolt() // fire a missile if we have a target
            {
                if(!target) return;

                float num = 3f;
                bool isCrit = Util.CheckRoll(body.crit, body.master);
                MissileUtils.FireMissile(body.corePosition, body, 
                    default(ProcChainMask), null, body.damage * num, isCrit, projectilePrefab, 
                    DamageColorIndex.Item, false);
            }
            HurtBox? target;
            static GameObject _projectilePrefab;
            static InstRef<GameObject> projectilePrefab = new(() => Assets.AssetBundle.LoadAsset<GameObject>("StormMissile.prefab"));

            void GenerateSearch() //finds the next target for our Storming Missiles
            {
                search.teamMaskFilter = TeamMask.GetUnprotectedTeams(body.teamComponent.teamIndex);
                search.filterByLoS = true;
                search.teamMaskFilter = TeamMask.allButNeutral;
                search.teamMaskFilter.RemoveTeam(body.teamComponent.teamIndex);
                search.filterByLoS = true;
                search.searchOrigin = transform.position;
                search.sortMode = BullseyeSearch.SortMode.Distance;
                search.RefreshCandidates();
                search.FilterOutGameObject(gameObject);
                var targets = (from v in search.GetResults()
                                 where v.healthComponent
                                 select v);
                target = targets.FirstOrDefault();
            }

            bool canFire = true;
            // looping timer to determine when we can fire missiles
            // this only runs if CanFire is false, so the timing never gets weird if we can't find a target (and end up waiting to reset our values)
            IEnumerator TickLightning(float duration) 
            {
                while(true)
                {
                    yield return new WaitUntil(() => !canFire);
                    yield return new WaitForSeconds(duration);
                    canFire = true;
                }
            }
        }

    }

    /// <summary>
    /// The component that controls our Storm Missile movement
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(QuaternionPID), typeof(TeamFilter))]
    [RequireComponent(typeof(ProjectileNetworkTransform), typeof(ProjectileImpactExplosion), typeof(ProjectileController))]
    public class StormMissile : MonoBehaviour
    {
        static InstRef<GameObject> ImpactEffect = new(() => LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/MissileExplosionVFX"));
        private void Awake() //only runs when this script is first loaded, for efficiency- not sure if hotloads to this prefab would work?
            {
                
                if (!NetworkServer.active) //this script can only run on Server Authorative objects! clients use Ghosts instead
                {
                    enabled = false;
                    return;
                }
                //initialises our missile values
                transform = base.transform;
                rigidbody = GetComponent<Rigidbody>();
                torquePID = GetComponent<QuaternionPID>();
                teamFilter = GetComponent<TeamFilter>();
                projectileExplosion = GetComponent<ProjectileImpactExplosion>();

            projectileExplosion.explosionEffect = ImpactEffect; //sets the explosion effect our current loaded Impact prefab
            ((GameObject) ImpactEffect).transform.localScale = projectileExplosion.blastRadius * Vector3.one; //scales the effect with our Blast Radius
            }
        void Start()
        {
            targetPos = FindTarget(); //gets the target of our missile
        }
        private void FixedUpdate()
        {
            timer += Time.fixedDeltaTime;
            /*
             * currently these missiles always move at max velocity; we can add acceleration to them later, though, pretty easily
             */
            rigidbody.velocity = transform.forward * maxVelocity; 

            if (Vector3.Distance(targetPos, Vector3.zero) > float.Epsilon && timer >= delayTimer) //runs if our delay has run out, and our target isn't 0.0.0
            {
                //runs if our target is within the blast radius, with some margin for error
                if (Vector3.Distance(transform.position, targetPos) < projectileExplosion.blastRadius * 0.9f)
                {
                    projectileExplosion.Detonate(); //detonates our missile
                }
                //... accelerates our missile past max once it finds a target? not sure what that's about...
                rigidbody.velocity = transform.forward * (maxVelocity + timer * acceleration); 

                //randomises our missile target; it jiggles around for some extra pizzazz
                Vector3 vector = (targetPos + (UnityEngine.Random.insideUnitSphere * turbulence)) - transform.position;
                if (vector != Vector3.zero) //only runs if we need to rotate
                {
                    Quaternion rotation = transform.rotation;
                    Quaternion targetQuat = Util.QuaternionSafeLookRotation(vector);
                    torquePID.inputQuat = rotation;
                    torquePID.targetQuat = targetQuat;
                    rigidbody.angularVelocity = torquePID.UpdatePID();
                }
            }
            if (timer > deathTimer) //detonate if we run out of life!
            {
                projectileExplosion.Detonate();
            }
        }
        private Vector3 FindTarget() //finds our current target
        {
            search.searchOrigin = transform.position;
            search.searchDirection = transform.forward;
            search.teamMaskFilter.RemoveTeam(teamFilter.teamIndex);
            search.RefreshCandidates();
            HurtBox hurtBox = search.GetResults().FirstOrDefault<HurtBox>();
            if (hurtBox == null)
            {
                return Vector3.zero;
            }
            return hurtBox.transform.position;
        }




            //all of our script's assorted values
            private Vector3 targetPos;
            private new Transform transform;
            private Rigidbody rigidbody;
            private TeamFilter teamFilter;
            private ProjectileImpactExplosion projectileExplosion;
            public float maxVelocity;
            public float rollVelocity;
            public float acceleration;
            public float delayTimer;
            public float deathTimer = 10f;
            private float timer;
            private QuaternionPID torquePID;
            public float turbulence;
            public float maxSeekDistance = 40f;
            private BullseyeSearch search = new BullseyeSearch();
        }
}
