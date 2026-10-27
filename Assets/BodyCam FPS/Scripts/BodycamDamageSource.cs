using UnityEngine;

namespace RealisticBodycam
{
    [AddComponentMenu("Realistic Bodycam/Damage Source")]
    public class BodycamDamageSource : MonoBehaviour
    {
        [Min(0f)] public float damage = 25f;
        public bool destroyAfterHit;

        void OnTriggerEnter(Collider other)
        {
            ApplyDamage(other);
        }

        void OnCollisionEnter(Collision collision)
        {
            ApplyDamage(collision.collider);
        }

        void ApplyDamage(Collider other)
        {
            BodycamPlayerHealth health = other.GetComponentInParent<BodycamPlayerHealth>();
            if (health == null)
                return;

            health.TakeDamage(damage);

            if (destroyAfterHit)
                Destroy(gameObject);
        }
    }
}
