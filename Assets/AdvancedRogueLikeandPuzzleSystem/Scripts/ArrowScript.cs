using System.Collections;
using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class ArrowScript : MonoBehaviour
    {
        private int Damage;
        private bool isHit = false;
        private bool Sent = false;
        public TrailRenderer trailRenderer;
        public Collider cold;

        public void Shooted(int DamageAmount)
        {
            Damage = DamageAmount;
            GetComponent<AudioSource>().Play();
            trailRenderer.enabled = true;
            Sent = true;
            StartCoroutine(ActivateCollider());
            Destroy(gameObject, 5);
        }

        IEnumerator ActivateCollider()
        {
            yield return new WaitForSeconds(0.1f);
            cold.isTrigger = false;
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (Sent && !isHit)
            {
                if (collision.collider.CompareTag("Player"))
                {
                    isHit = true;
                    trailRenderer.enabled = false;
                    HeroController.instance.GetDamagePlayer(Damage);
                    if (HeroController.instance.inDefendMode)
                    {
                        Rigidbody rb = GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            Vector3 reflectDirection = Vector3.Reflect(rb.linearVelocity.normalized, collision.contacts[0].normal);

                            float randomAngle = Random.Range(-30f, 30f);
                            float randomUpward = Random.Range(10f, 25f);

                            reflectDirection = Quaternion.Euler(0f, randomAngle, 0f) * reflectDirection;
                            Vector3 upwardAngle = Quaternion.AngleAxis(randomUpward, Vector3.Cross(reflectDirection, Vector3.up)) * reflectDirection;

                            rb.linearVelocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;

                            float randomForce = Random.Range(12f, 18f);
                            rb.AddForce(upwardAngle * randomForce, ForceMode.Impulse);
                            rb.AddTorque(Random.insideUnitSphere * Random.Range(3f, 7f), ForceMode.Impulse);

                            isHit = false;
                            trailRenderer.enabled = true;
                            Destroy(gameObject, 3);
                        }
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}