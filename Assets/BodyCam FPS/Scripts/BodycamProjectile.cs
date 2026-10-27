using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Projectile")]
    public class BodycamProjectile : MonoBehaviour
    {
        [Header("IMPACT")]
        public GameObject impactPrefab;

        [Min(0f)]
        public float impactLifetime = 3f;

        Rigidbody projectileRigidbody;
        float damage;
        LayerMask collisionMask;
        bool hasHit;

        public void Initialize(
            Vector3 direction,
            float speed,
            float lifetime,
            float projectileDamage,
            bool useGravity,
            LayerMask hitMask,
            Transform ownerRoot)
        {
            projectileRigidbody = GetComponent<Rigidbody>();

            if (projectileRigidbody == null)
                projectileRigidbody = gameObject.AddComponent<Rigidbody>();

            if (GetComponent<Collider>() == null)
            {
                SphereCollider bulletCollider = gameObject.AddComponent<SphereCollider>();
                bulletCollider.radius = 0.02f;
            }

            damage = projectileDamage;
            collisionMask = hitMask;

            projectileRigidbody.useGravity = useGravity;
            projectileRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            projectileRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            projectileRigidbody.linearVelocity = direction.normalized * speed;

            IgnoreOwnerCollisions(ownerRoot);
            Destroy(gameObject, lifetime);
        }

        void IgnoreOwnerCollisions(Transform ownerRoot)
        {
            if (ownerRoot == null)
                return;

            Collider bulletCollider = GetComponent<Collider>();
            Collider[] ownerColliders = ownerRoot.GetComponentsInChildren<Collider>();

            foreach (Collider ownerCollider in ownerColliders)
            {
                if (ownerCollider != null && ownerCollider != bulletCollider)
                    Physics.IgnoreCollision(bulletCollider, ownerCollider, true);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (hasHit)
                return;

            int collidedLayer = collision.gameObject.layer;

            if ((collisionMask.value & (1 << collidedLayer)) == 0)
                return;

            hasHit = true;

            BodycamRespawningTarget respawningTarget =
                collision.transform.GetComponentInParent<BodycamRespawningTarget>();

            if (respawningTarget != null)
            {
                respawningTarget.TakeDamage(damage);
            }
            else
            {
                BodycamShootableTarget shootableTarget =
                    collision.transform.GetComponentInParent<BodycamShootableTarget>();

                if (shootableTarget != null)
                {
                    shootableTarget.TakeDamage(damage);
                }
                else
                {
                    collision.transform.SendMessageUpwards(
                        "TakeDamage",
                        damage,
                        SendMessageOptions.DontRequireReceiver
                    );
                }
            }

            if (impactPrefab != null && collision.contactCount > 0)
            {
                ContactPoint contact = collision.GetContact(0);
                GameObject impact = Instantiate(
                    impactPrefab,
                    contact.point,
                    Quaternion.LookRotation(contact.normal)
                );

                Destroy(impact, impactLifetime);
            }

            Destroy(gameObject);
        }
    }
}
