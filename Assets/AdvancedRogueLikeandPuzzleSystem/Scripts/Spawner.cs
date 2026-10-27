using UnityEngine;
using System.Collections.Generic;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class Spawner : MonoBehaviour
    {
        [Header("Spawner Configs")]
        public float spawnInterval = 1f;
        public GameObject[] prefabTypes;
        public int TargetSpawnCount = 10;
        public int maxAlivePrefabs = 5;
        public bool canSpawn = true;
        public Transform[] spawnPoints;

        private int currentSpawned = 0;
        private List<GameObject> alivePrefabs = new List<GameObject>();
        private float spawnTimer = 0f;
        private float cleanupTimer = 0f;
        private float cleanupInterval = 1f;
        private int lastSpawnPointIndex = -1;

        void Update()
        {
            cleanupTimer += Time.deltaTime;
            if (cleanupTimer >= cleanupInterval)
            {
                alivePrefabs.RemoveAll(item => item == null);
                cleanupTimer = 0f;
            }

            if (!canSpawn) return;
            if (currentSpawned >= TargetSpawnCount) return;
            if (alivePrefabs.Count >= maxAlivePrefabs) return;

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                SpawnPrefab();
            }
        }

        void SpawnPrefab()
        {
            if (prefabTypes.Length == 0 || spawnPoints.Length == 0) return;

            GameObject prefabToSpawn = prefabTypes[Random.Range(0, prefabTypes.Length)];
            int spawnIndex = GetRandomSpawnPointIndex();
            Transform spawnPoint = spawnPoints[spawnIndex];

            GameObject spawned = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
            alivePrefabs.Add(spawned);
            currentSpawned++;
            lastSpawnPointIndex = spawnIndex;
        }

        int GetRandomSpawnPointIndex()
        {
            if (spawnPoints.Length <= 1)
                return 0;

            int index;
            do
            {
                index = Random.Range(0, spawnPoints.Length);
            } while (index == lastSpawnPointIndex);

            return index;
        }
    }
}
