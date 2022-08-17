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
    [HarmonyPatch]
    public class AffixStorm : EquipBase
    {
        protected override string equipDefName => "edAffixStorming";
        protected override string configName => "Storming Aspect";

        public override void AddEquipBehavior(){ }

        [HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.OnEquipmentGained))]
        public static void EquipmentBehaviorPatch(CharacterBody __instance, EquipmentDef equipmentDef)
        {
            if (equipmentDef != GetInstance<AffixStorm>()!.equipDef) return;
            __instance.gameObject.AddComponent<StormAffixComponent>();
            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/LightningStrikeImpact"), new EffectData
            {
                origin = __instance.gameObject.transform.position
            }, true);

        }


        [HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.OnEquipmentLost))]
        public static void EquipmentLostPatch(CharacterBody __instance, EquipmentDef equipmentDef)
        {
            if (equipmentDef != GetInstance<AffixStorm>()!.equipDef) return;
            UnityEngine.Object.Destroy(__instance.gameObject.GetComponent<StormAffixComponent>());

        }

        private static BuffDef _stuporBuff;
        static BuffDef StuporBuff
        {
            get
            {
                _stuporBuff ??= Assets.ContentPack.buffDefs.Find("stupor");
                return _stuporBuff;
            }
        }
        [HarmonyPostfix, HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats))]
        public static void StuporBuffPatch(CharacterBody __instance)
        {
            if(__instance.HasBuff(StuporBuff))
            {
                __instance.damage *= Mathf.Min((1 - (0.05f * __instance.GetBuffCount(StuporBuff))),0.7f);
                __instance.sprintingSpeedMultiplier *= Mathf.Min((1 - (0.05f * __instance.GetBuffCount(StuporBuff))),0.7f);
            }
        }
        public class StormAffixComponent : MonoBehaviour
        {
            private readonly BullseyeSearch search = new BullseyeSearch();
            public static InstRef<GameObject> stuporWard = new(() => Assets.AssetBundle.LoadAsset<GameObject>("StuporField.prefab"));

            public GameObject? stuporInstance;
            Coroutine? lightningRoutine;
            float duration = 3;
            CharacterBody body;
            void Start()
            {
                body = GetComponent<CharacterBody>();
                stuporInstance = Instantiate((GameObject) stuporWard, gameObject.transform);
                stuporInstance!.GetComponent<TeamFilter>().teamIndex = body.teamComponent.teamIndex;
                //Debug.LogWarning($"STORMSURGE :: instantiated {stuporInstance}");
                lightningRoutine = StartCoroutine(TickLightning(duration));
            }
            void OnDestroy()
            {
                Destroy(stuporInstance);
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

            void FireBolt()
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
            static GameObject projectilePrefab => _projectilePrefab ??= Assets.AssetBundle.LoadAsset<GameObject>("StormMissile.prefab");

            void GenerateSearch()
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

    [RequireComponent(typeof(Rigidbody), typeof(QuaternionPID), typeof(TeamFilter))]
    [RequireComponent(typeof(ProjectileNetworkTransform), typeof(ProjectileImpactExplosion), typeof(ProjectileController))]
    public class StormMissile : MonoBehaviour
        {
        static GameObject _impactEffect;
        static GameObject ImpactEffect => _impactEffect ??= LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/MissileExplosionVFX");
        private void Awake()
            {
                if (!NetworkServer.active)
                {
                    enabled = false;
                    return;
                }
                transform = base.transform;
                rigidbody = GetComponent<Rigidbody>();
                torquePID = GetComponent<QuaternionPID>();
                teamFilter = GetComponent<TeamFilter>();
                projectileExplosion = GetComponent<ProjectileImpactExplosion>();

            projectileExplosion.explosionEffect = ImpactEffect;
            ImpactEffect.transform.localScale = projectileExplosion.blastRadius * Vector3.one;
            }
        void Start()
        {
            targetPos = FindTarget();
        }
        private void FixedUpdate()
        {
            timer += Time.fixedDeltaTime;
            rigidbody.velocity = transform.forward * maxVelocity;
            if (Vector3.Distance(targetPos, Vector3.zero) > float.Epsilon && timer >= delayTimer)
            {
                if (Vector3.Distance(transform.position, targetPos) < projectileExplosion.blastRadius * 0.9f)
                {
                    projectileExplosion.Detonate();
                }
                rigidbody.velocity = transform.forward * (maxVelocity + timer * acceleration);
                Vector3 vector = (targetPos + UnityEngine.Random.insideUnitSphere * turbulence - transform.position);
                if (vector != Vector3.zero)
                {
                    Quaternion rotation = transform.rotation;
                    Quaternion targetQuat = Util.QuaternionSafeLookRotation(vector);
                    torquePID.inputQuat = rotation;
                    torquePID.targetQuat = targetQuat;
                    rigidbody.angularVelocity = torquePID.UpdatePID();
                }
            }
            if (timer > deathTimer)
            {
                projectileExplosion.Detonate();
            }
        }
        private Vector3 FindTarget()
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
