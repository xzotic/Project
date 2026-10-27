using System.Collections.Generic;
using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    /// <summary>
    /// Loot dropped when an enemy dies (called from SoldierController/ArcherController).
    /// Rolls 0, 1 or 2 drops with equal chance; two drops are always different prefabs.
    /// </summary>
    public class GainScript : MonoBehaviour
    {
        public List<GameObject> prefabs;

        public void Spread()
        {
            if (prefabs == null || prefabs.Count == 0) return;

            int a = Random.Range(0, 3);
            if (a == 1)
            {
                SpawnDrop(prefabs[Random.Range(0, prefabs.Count)]);
            }
            else if (a == 2)
            {
                int i = Random.Range(0, prefabs.Count);
                SpawnDrop(prefabs[i]);
                // Remove the first pick so the second drop is a different item.
                prefabs.RemoveAt(i);
                if (prefabs.Count > 0)
                {
                    SpawnDrop(prefabs[Random.Range(0, prefabs.Count)]);
                }
            }
        }

        private void SpawnDrop(GameObject prefab)
        {
            if (prefab == null) return;
            GameObject drop = Instantiate(prefab, transform.position, Quaternion.identity);
            if (drop.TryGetComponent(out Rigidbody body))
            {
                body.AddForce(new Vector3(Random.Range(-1, 1), Random.Range(1, 3), Random.Range(-1, 1)) * 4, ForceMode.Impulse);
            }
        }
    }
}
