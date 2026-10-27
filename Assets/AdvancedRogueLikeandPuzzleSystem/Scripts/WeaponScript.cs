using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class WeaponScript : MonoBehaviour
    {
        public int ID;
        public Collider colliderGrab;
        public string Name;
        public Collider colliderHit;
        public WeaponType weaponType;
        public bool isTaken = false;
        public TrailRenderer trailRenderer;
        public int DamagePower = 20;

        public Vector3 direction;
        public GameObject[] bloodParticles;

        public void MakeDamage(GameObject obje)
        {
            if (obje == null || !obje.CompareTag("Soldier")) return;

            bool damaged = false;
            if (obje.TryGetComponent(out SoldierController soldier) && soldier.health > 0)
            {
                soldier.GetDamage(DamagePower);
                damaged = true;
            }
            else if (obje.TryGetComponent(out ArcherController archer) && archer.health > 0)
            {
                archer.GetDamage(DamagePower);
                damaged = true;
            }

            if (damaged)
            {
                SpawnBlood(obje.transform);
                obje.transform.position = obje.transform.position - obje.transform.forward / 6f;
            }

            if (AudioManager.instance != null)
            {
                AudioManager.instance.Play_WeaponHit();
            }
        }

        private void SpawnBlood(Transform victim)
        {
            if (bloodParticles == null || bloodParticles.Length == 0) return;
            GameObject prefab = bloodParticles[Random.Range(0, bloodParticles.Length)];
            if (prefab == null) return;
            Instantiate(prefab, new Vector3(victim.position.x, victim.position.y + 1, victim.position.z), Quaternion.identity);
        }
    }

    public enum WeaponType
    {
        Shield,
        Sword,
        Tourch,
        Axe
    }
}