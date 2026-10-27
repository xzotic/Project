using System.Collections.Generic;
using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class WeaponEffectScript : MonoBehaviour
    {
        public List<GameObject> list = new List<GameObject>();
        public static WeaponEffectScript Instance;

        private void Awake()
        {
            Instance = this;    
        }

        public void Attack()
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                {
                    list.RemoveAt(i);
                    i--;
                }
            }
            // The hit is fired by an animation event, so the weapon can already have been
            // unequipped (from the inventory) by the time the swing lands.
            WeaponScript weapon = HeroController.instance != null && HeroController.instance.WeaponAttack != null
                ? HeroController.instance.WeaponAttack.GetComponent<WeaponScript>()
                : null;
            if (weapon == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                weapon.MakeDamage(list[i]);
            }
            if(list.Count > 0)
            {
                GameManager.Instance.ShakeTheCamera(0.5f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Soldier"))
            {
                if(!list.Contains(other.gameObject))
                {
                    list.Add(other.gameObject);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Soldier"))
            {
                if (list.Contains(other.gameObject))
                {
                    list.Remove(other.gameObject);
                }
            }
        }
    }
}