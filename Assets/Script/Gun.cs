using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private ObjectPool bulletPool;

    [Header("Weapon")]
    [SerializeField] private float bulletSpeed = 45f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float fireRate = 0.08f;
    [SerializeField] private float bulletLifetime = 5f;
    [SerializeField] private int projectileCount = 1;
    [SerializeField] private float spreadAngle = 6f;
    [SerializeField, Range(0f, 1f)] private float criticalChance = 0f;
    [SerializeField] private float criticalDamageMultiplier = 2f;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1f, 0f);

    private Transform currentTarget;
    private bool isFiring;
    private float fireTimer;

    public float Damage => damage;
    public float FireRate => fireRate;
    public int ProjectileCount => projectileCount;
    public float CriticalChance => criticalChance;

    private void Awake()
    {
        if (muzzle == null)
        {
            muzzle = transform;
        }
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;
    }

    public void SetFiring(bool firing)
    {
        isFiring = firing;

        if (!firing)
        {
            fireTimer = 0f;
        }
    }

    public Vector3 GetAimPoint()
    {
        return GetTargetPoint();
    }

    private void Update()
    {
        if (!isFiring)
            return;

        if (currentTarget == null)
            return;

        if (bulletPrefab == null && bulletPool == null)
            return;

        if (muzzle == null)
            return;

        fireTimer -= Time.deltaTime;

        if (fireTimer <= 0f)
        {
            Fire();

            fireTimer = fireRate;
        }
    }

    private void Fire()
    {
        Vector3 targetPosition =
            GetTargetPoint();

        Vector3 direction =
            (targetPosition - muzzle.position).normalized;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        int count = Mathf.Max(1, projectileCount);
        float startAngle = count > 1
            ? -spreadAngle * (count - 1) * 0.5f
            : 0f;

        for (int i = 0; i < count; i++)
        {
            float yaw = startAngle + spreadAngle * i;
            Vector3 shotDirection =
                Quaternion.AngleAxis(yaw, Vector3.up) *
                direction;

            SpawnBullet(shotDirection.normalized);
        }
    }

    private Vector3 GetTargetPoint()
    {
        if (currentTarget == null)
        {
            return transform.position + transform.forward;
        }

        Collider targetCollider =
            currentTarget.GetComponentInChildren<Collider>();

        if (targetCollider != null)
        {
            return targetCollider.bounds.center;
        }

        return currentTarget.position + targetOffset;
    }

    private void SpawnBullet(Vector3 direction)
    {
        Quaternion rotation =
            Quaternion.LookRotation(direction);

        GameObject bulletObject = bulletPool != null
            ? bulletPool.Spawn(muzzle.position, rotation)
            : Instantiate(bulletPrefab, muzzle.position, rotation);

        if (bulletObject == null)
        {
            return;
        }

        Bullet bullet =
            bulletObject.GetComponent<Bullet>();

        if (bullet == null)
        {
            Debug.LogError(
                "Bullet prefab does not contain Bullet component."
            );

            return;
        }

        float finalDamage =
            Random.value <= criticalChance
                ? damage * criticalDamageMultiplier
                : damage;

        bullet.Initialize(
            transform.root.gameObject,
            direction,
            bulletSpeed,
            finalDamage,
            bulletLifetime
        );
    }

    public void AddDamageMultiplier(float multiplier)
    {
        damage *= Mathf.Max(0f, multiplier);
    }

    public void AddFireRateMultiplier(float multiplier)
    {
        fireRate /= Mathf.Max(0.01f, multiplier);
    }

    public void AddProjectileCount(int amount)
    {
        projectileCount = Mathf.Max(1, projectileCount + amount);
    }

    public void AddCriticalChance(float amount)
    {
        criticalChance = Mathf.Clamp01(criticalChance + amount);
    }
}
