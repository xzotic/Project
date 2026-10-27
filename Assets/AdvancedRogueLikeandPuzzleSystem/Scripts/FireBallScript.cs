using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class FireBallScript : MonoBehaviour
    {
        private static readonly Collider[] overlapBuffer = new Collider[64];

        public GameObject explosionPrefab;
        public int damage;
        private bool crashed = false;
        public Rigidbody rigidBody;
        public Collider cold;
        public float damageRange;
        public LayerMask collisions;
        public LayerMask damageLayerMask;
        public ParticleSystem trailerInside;

        void Start()
        {
            Destroy(gameObject, 10);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (Contains(collision.gameObject.layer))
            {
                if (!crashed)
                {
                    if (explosionPrefab != null)
                    {
                        Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                    }
                    Destroy(gameObject, 5f);
                    crashed = true;

                    int hitCount = Physics.OverlapSphereNonAlloc(transform.position, damageRange, overlapBuffer, damageLayerMask);
                    for (int i = 0; i < hitCount; i++)
                    {
                        if (overlapBuffer[i].TryGetComponent(out SoldierController soldier))
                        {
                            soldier.GetDamage(damage);
                        }
                        else if (overlapBuffer[i].TryGetComponent(out ArcherController archer))
                        {
                            archer.GetDamage(damage);
                        }
                        else if (overlapBuffer[i].CompareTag("Player") && HeroController.instance != null)
                        {
                            HeroController.instance.GetDamagePlayer(damage);
                        }
                    }

                    Vector3 explosionOrigin = new Vector3(transform.position.x, transform.position.y - 3, transform.position.z);
                    hitCount = Physics.OverlapSphereNonAlloc(transform.position, 5, overlapBuffer, collisions);
                    for (int i = 0; i < hitCount; i++)
                    {
                        if (!overlapBuffer[i].CompareTag("Collapsable")) continue;
                        if (overlapBuffer[i].TryGetComponent(out Rigidbody debris))
                        {
                            debris.isKinematic = false;
                            debris.AddExplosionForce(500, explosionOrigin, 50);
                        }
                    }

                    Destroy(rigidBody);
                    if (TryGetComponent(out MeshRenderer meshRenderer))
                    {
                        meshRenderer.enabled = false;
                    }
                    Destroy(cold);
                    if (trailerInside != null) trailerInside.Stop();
                }
            }
        }

        public bool Contains(int layer)
        {
            return collisions == (collisions | (1 << layer));
        }
    }
}