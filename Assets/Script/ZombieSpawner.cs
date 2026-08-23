using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieSpawner : MonoBehaviour
{
    private static readonly List<ZombieSpawner> ActiveSpawners =
        new List<ZombieSpawner>();

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject[] zombiePrefabs;
    [SerializeField] private ObjectPool[] zombiePools;

    [Header("Spawn")]
    [SerializeField] private int maxAliveZombies = 60;
    [SerializeField] private int spawnBurstCount = 3;
    [SerializeField] private float spawnDistanceMin = 14f;
    [SerializeField] private float spawnDistanceMax = 32f;
    [SerializeField] private float spawnInterval = 0.8f;
    [SerializeField] private float spawnIntervalMin = 0.15f;
    [SerializeField] private float spawnIntervalRampPerMinute = 0.5f;
    [SerializeField] private int maxAliveIncreasePerMinute = 20;

    [Header("Difficulty")]
    [SerializeField] private float healthIncreasePerMinute = 0.2f;
    [SerializeField] private float speedIncreasePerMinute = 0.05f;
    [SerializeField] private float damageIncreasePerMinute = 0.15f;

    private int aliveCount;
    private float timer;
    private float elapsed;

    public int AliveCount => aliveCount;
    public float Elapsed => elapsed;

    private void OnEnable()
    {
        if (!ActiveSpawners.Contains(this))
        {
            ActiveSpawners.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveSpawners.Remove(this);
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        elapsed += Time.deltaTime;
        timer -= Time.deltaTime;

        if (timer > 0f)
        {
            return;
        }

        timer = GetCurrentSpawnInterval();

        if (aliveCount >= GetCurrentMaxAlive())
        {
            return;
        }

        int spawnCount =
            Mathf.Min(
                spawnBurstCount,
                GetCurrentMaxAlive() - aliveCount
            );

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnZombie();
        }
    }

    private void SpawnZombie()
    {
        if (!TryGetSpawnPosition(out Vector3 spawnPosition))
        {
            return;
        }

        GameObject zombie = CreateZombie(spawnPosition);

        if (zombie == null)
        {
            return;
        }

        aliveCount++;

        EnemyAI enemyAI = zombie.GetComponent<EnemyAI>();

        if (enemyAI != null)
        {
            enemyAI.enabled = true;
            enemyAI.SetTarget(player);
            enemyAI.SetStats(
                GetHealthMultiplier(),
                GetSpeedMultiplier(),
                GetDamageMultiplier()
            );
        }

        EnemyHealth enemyHealth = zombie.GetComponent<EnemyHealth>();

        if (enemyHealth != null)
        {
            enemyHealth.ResetHealth();
        }
    }

    private GameObject CreateZombie(Vector3 spawnPosition)
    {
        Quaternion rotation =
            Quaternion.LookRotation(
                (player.position - spawnPosition).normalized,
                Vector3.up
            );

        if (zombiePools != null && zombiePools.Length > 0)
        {
            ObjectPool pool =
                zombiePools[Random.Range(0, zombiePools.Length)];

            if (pool != null)
            {
                return pool.Spawn(spawnPosition, rotation);
            }
        }

        if (zombiePrefabs == null || zombiePrefabs.Length == 0)
        {
            Debug.LogWarning("[ZombieSpawner] No zombie prefab assigned.");
            return null;
        }

        GameObject prefab =
            zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];

        return Instantiate(prefab, spawnPosition, rotation);
    }

    private bool TryGetSpawnPosition(out Vector3 spawnPosition)
    {
        Vector3 fallbackPosition = player.position;

        for (int i = 0; i < 16; i++)
        {
            Vector2 circle = Random.insideUnitCircle.normalized;

            if (circle.sqrMagnitude <= 0.001f)
            {
                circle = Vector2.right;
            }

            float distance =
                Random.Range(spawnDistanceMin, spawnDistanceMax);

            Vector3 candidate =
                player.position +
                new Vector3(circle.x, 0f, circle.y) * distance;

            fallbackPosition = candidate;

            if (NavMesh.SamplePosition(
                    candidate,
                    out NavMeshHit hit,
                    4f,
                    NavMesh.AllAreas))
            {
                spawnPosition = hit.position;
                return true;
            }
        }

        spawnPosition = fallbackPosition;
        return true;
    }

    private int GetCurrentMaxAlive()
    {
        int bonus =
            Mathf.FloorToInt(
                elapsed / 60f *
                maxAliveIncreasePerMinute
            );

        return maxAliveZombies + bonus;
    }

    private float GetCurrentSpawnInterval()
    {
        float reduction =
            elapsed / 60f *
            spawnIntervalRampPerMinute;

        return Mathf.Max(
            spawnIntervalMin,
            spawnInterval - reduction
        );
    }

    private float GetHealthMultiplier()
    {
        return 1f + elapsed / 60f * healthIncreasePerMinute;
    }

    private float GetSpeedMultiplier()
    {
        return 1f + elapsed / 60f * speedIncreasePerMinute;
    }

    private float GetDamageMultiplier()
    {
        return 1f + elapsed / 60f * damageIncreasePerMinute;
    }

    public static void NotifyZombieKilled(EnemyHealth enemy)
    {
        for (int i = 0; i < ActiveSpawners.Count; i++)
        {
            ZombieSpawner spawner = ActiveSpawners[i];

            if (spawner != null && spawner.aliveCount > 0)
            {
                spawner.aliveCount--;
                return;
            }
        }
    }
}
