using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class ZombieSpawner : MonoBehaviour
{
    private static readonly List<ZombieSpawner> ActiveSpawners =
        new List<ZombieSpawner>();

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject[] zombiePrefabs;
    [SerializeField] private ObjectPool[] zombiePools;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Spawn")]
    [FormerlySerializedAs("maxAliveZombies")]
    [SerializeField] private int maxZombies = 60;
    [SerializeField] private int spawnBurstCount = 3;

    [FormerlySerializedAs("spawnDistanceMin")]
    [SerializeField] private float spawnDistanceMin = 14f;

    [FormerlySerializedAs("spawnDistanceMax")]
    [SerializeField] private float spawnDistanceMax = 32f;
    [SerializeField] private float navMeshSampleDistance = 2f;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private float spawnRate = 1.25f;

    [SerializeField] private float maxSpawnRate = 6.5f;

    [SerializeField] private float spawnRateIncreasePerMinute = 0.8f;

    [SerializeField] private int maxAliveIncreasePerMinute = 20;

    [Header("Difficulty")]
    [SerializeField] private float healthIncreasePerMinute = 0.2f;
    [SerializeField] private float speedIncreasePerMinute = 0.05f;
    [SerializeField] private float damageIncreasePerMinute = 0.15f;

    private int aliveCount;
    private readonly HashSet<EnemyHealth> spawnedEnemies =
        new HashSet<EnemyHealth>();
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
            spawnedEnemies.Add(enemyHealth);
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
        if (TryGetSpawnPointPosition(out spawnPosition))
        {
            return true;
        }

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

            if (TryGetGroundedNavMeshPosition(candidate, out spawnPosition))
            {
                return true;
            }
        }

        spawnPosition = Vector3.zero;
        return false;
    }

    private bool TryGetSpawnPointPosition(out Vector3 spawnPosition)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            spawnPosition = Vector3.zero;
            return false;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform spawnPoint =
                spawnPoints[Random.Range(0, spawnPoints.Length)];

            if (spawnPoint == null)
            {
                continue;
            }

            float distanceSqr =
                (spawnPoint.position - player.position).sqrMagnitude;

            if (distanceSqr <
                spawnDistanceMin * spawnDistanceMin)
            {
                continue;
            }

                    if (TryGetGroundedNavMeshPosition(
                        spawnPoint.position,
                        out spawnPosition))
            {
                return true;
            }
        }

        spawnPosition = Vector3.zero;
        return false;
    }

    private bool TryGetGroundedNavMeshPosition(
        Vector3 origin,
        out Vector3 spawnPosition)
    {
        int layerMask = GetGroundLayerMask();
        Vector3 rayOrigin = origin + Vector3.up * 50f;

        if (!Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit groundHit,
                100f,
                layerMask,
                QueryTriggerInteraction.Ignore))
        {
            spawnPosition = Vector3.zero;
            return false;
        }

        spawnPosition = Vector3.zero;

        if (!NavMesh.SamplePosition(
            groundHit.point,
            out NavMeshHit navMeshHit,
            navMeshSampleDistance,
            NavMesh.AllAreas))
        {
            return false;
        }

        return AssignSpawnPosition(
            navMeshHit.position,
            groundHit.point,
            out spawnPosition
        );
    }

    private bool AssignSpawnPosition(
        Vector3 navMeshPosition,
        Vector3 groundPosition,
        out Vector3 spawnPosition)
    {
        if (Mathf.Abs(navMeshPosition.y - groundPosition.y) > navMeshSampleDistance)
        {
            spawnPosition = Vector3.zero;
            return false;
        }

        spawnPosition = navMeshPosition;
        return true;
    }

    private int GetGroundLayerMask()
    {
        if (groundLayer.value != 0)
        {
            return groundLayer.value;
        }

        int groundLayerIndex = LayerMask.NameToLayer("Ground");
        return groundLayerIndex >= 0
            ? 1 << groundLayerIndex
            : Physics.DefaultRaycastLayers;
    }

    private int GetCurrentMaxAlive()
    {
        int bonus =
            Mathf.FloorToInt(
                elapsed / 60f *
                maxAliveIncreasePerMinute
            );

        return maxZombies + bonus;
    }

    private float GetCurrentSpawnInterval()
    {
        float currentSpawnRate =
            Mathf.Min(
                maxSpawnRate,
                spawnRate +
                elapsed / 60f *
                spawnRateIncreasePerMinute
            );

        if (currentSpawnRate <= 0f)
        {
            return float.MaxValue;
        }

        return 1f / currentSpawnRate;
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

            if (spawner != null && spawner.spawnedEnemies.Remove(enemy))
            {
                spawner.aliveCount = Mathf.Max(0, spawner.aliveCount - 1);
                return;
            }
        }
    }
}
