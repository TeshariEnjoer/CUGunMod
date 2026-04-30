using System;
using System.Collections.Generic;
using UnityEngine;

namespace CasGunMod {

    public enum PiercingType {
        None,
        Flat,
        Sharpnel,
        Piercing
    }


    public class CasinngComponent : MonoBehaviour {

        public string Caliber;

        public float FlatDamage = 10f;

        public float ArmorPenetration = 0f;

        public float SharpnelChance = 0f;

        public float Speed = 100f;

        public float MaximumRange = 500f;

        public float StructureDamageMultiplier = 1f;

        public PiercingType PiercingFactor = PiercingType.Flat;

    }

    public class ProjectileComponent : MonoBehaviour {

        public enum HitResult {
            None,
            Pierce,
            ForcePierce,
            Block
        }

        private CasinngComponent _casing = null;
        private Vector3 _velocity = Vector3.zero;
        private Vector3 _target = Vector3.zero;
        private Vector3 _shootDirectionn = Vector3.zero;
        private float _age = 0f;
        private bool _fired = false;


        private void Start() {
            _casing = GetComponent<CasinngComponent>();
            if (_casing == null) {
                Debug.LogError("ProjectileComponent requires a CasinngComponent on the same GameObject.");
                Destroy(this);
            }
            _velocity = Vector3.zero;
            _target = Vector3.zero;
            _age = 0f;
        }

        public void Shoot(Vector3 direction) {
            if (_casing == null) {
                Debug.LogError("Cannot shoot without a CasinngComponent.");
                return;
            }
            _velocity = direction.normalized * _casing.Speed;
            _target = direction;
            _fired = true;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update() {
            if (!_fired) return;

            float dt = Time.deltaTime;

            Vector3 start = transform.position;
            Vector3 step = _velocity * dt;
            float dist = step.magnitude;

            if (dist > 0f) {

                var hits = FindPotentialTargets(start, _velocity.normalized, dist);

                foreach (var hit in hits) {

                    HitResult result = CanHitTarget(hit);

                    if (result == HitResult.None)
                        continue;

                    HandleImpact(hit, result);

                    if (result == HitResult.Pierce)
                        Destroy(gameObject);

                    if (result == HitResult.Block)
                        Destroy(gameObject);

                    return;
                }
            }

            transform.position += step;

            _age += dt;
            if (_age >= _casing.MaximumRange / _casing.Speed) {
                Destroy(gameObject);
            }
        }

        private List<RaycastHit2D> FindPotentialTargets(Vector3 origin, Vector3 dir, float range) {

            var results = new List<RaycastHit2D>();

            RaycastHit2D[] hits = Physics2D.CircleCastAll(
                origin,
                0.2f,
                dir,
                range,
                LayerMask.GetMask("Body", "Limb", "Blocking")
            );

            foreach (var hit in hits) {
                if (hit.collider == null) continue;

                results.Add(hit);
            }

            return results;
        }


        /// <summary>
        /// Determines whether a shot from origin in direction dir for given range would hit a valid target.
        /// This mirrors TurretScript raycast logic (checks for Body/Limb and blocking layers).
        /// </summary>
        private HitResult CanHitTarget(RaycastHit2D hit) {

            if (hit.collider == null)
                return HitResult.None;

            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Blocking"))
                return HitResult.Block;

            if (hit.collider.TryGetComponent(out BuildingEntity building)) {
                return building.cantHit ? HitResult.None : HitResult.Pierce;
            }

            if (hit.collider.TryGetComponent(out Limb limb))
                return limb.body != null ? HitResult.Pierce : HitResult.None;

            if (hit.collider.TryGetComponent(out Body body))
                return HitResult.Pierce;

            return HitResult.None;
        }


        private void HandleImpact(RaycastHit2D hit, HitResult result) {

            Vector2 point = hit.point;

            if (hit.rigidbody != null) {
                hit.rigidbody.AddForceAtPosition(_velocity, point, ForceMode2D.Impulse);
            }

            float damage = _casing.FlatDamage;
            float structureDamage = _casing.FlatDamage * _casing.StructureDamageMultiplier;

            if (result == HitResult.Block) {

                WorldGeneration.world.DamageBlock(point, structureDamage);

                WorldGeneration.CreateDamageNumber(point, (int)structureDamage);

                return;
            }

            if (hit.collider.TryGetComponent(out Limb limb)) {

                var body = limb.body;
                if (body == null) return;

                ApplyBodyDamage(limb, body, damage, point);
                return;
            }

            if (hit.collider.TryGetComponent(out Body bodyOnly)) {

                ApplyBodyHit(bodyOnly, damage);
                return;
            }

            if (hit.collider.TryGetComponent(out BuildingEntity building)) {

                if (building.cantHit)
                    return;

                building.health -= damage;

                WorldGeneration.CreateDamageNumber(point, (int)damage);
            }
        }


        private void ApplyBodyHit(Body body, float damage) {

            //body.rb.velocity += _velocity.normalized * 10f;
            body.Ragdoll();
        }

        private void ApplyBodyDamage(Limb limb, Body body, float damage, Vector2 point) {

            float armor = Mathf.Max(1f, limb.GetArmorReduction());

            float final = damage / armor;

            limb.skinHealth -= final;
            limb.muscleHealth -= final;
            limb.bleedAmount += 30f;

            //body.rb.velocity += _velocity.normalized * 10f;
            body.Ragdoll();

            limb.pain += UnityEngine.Random.Range(80f, 100f) / armor;
            body.adrenaline = 100f;

            if (UnityEngine.Random.value < 0.3f) {
                limb.BreakBone();
            }

            if (limb.isHead) {
                body.consciousness = 0f;
            }
        }
    }

    public class GunComponent : MonoBehaviour {
 
    }
}
