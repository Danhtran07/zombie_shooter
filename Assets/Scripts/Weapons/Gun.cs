using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private ObjectPool bulletPool;
    [SerializeField] private ThirdPersonCamera cameraController;
    [SerializeField] private PlayerWeaponAim weaponAim;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireSound;
    [SerializeField, Range(0f, 1f)] private float fireVolume = 1f;

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
    [SerializeField] private float cameraRecoilPitch = 1.15f;
    [SerializeField] private float cameraRecoilYaw = 0.45f;
    [SerializeField] private float cameraRecoilRoll = 0.55f;
    [SerializeField] private Vector3 weaponRecoilPosition =
        new Vector3(0f, -0.015f, -0.085f);
    [SerializeField] private Vector3 weaponRecoilEuler =
        new Vector3(-4.5f, 0f, 1.25f);

    private Transform currentTarget;
    private GunEffects effects;
    private GunProjectileSpawner projectileSpawner;
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

        if (cameraController == null)
        {
            cameraController =
                FindObjectOfType<ThirdPersonCamera>();
        }

        if (weaponAim == null)
        {
            weaponAim = GetComponentInParent<PlayerWeaponAim>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        effects = new GunEffects(
            muzzle,
            muzzleFlashPrefab,
            audioSource,
            fireSound,
            fireVolume
        );

        projectileSpawner = new GunProjectileSpawner(
            muzzle,
            bulletPrefab,
            bulletPool,
            transform.root.gameObject,
            bulletSpeed,
            damage,
            bulletLifetime,
            criticalChance,
            criticalDamageMultiplier
        );

        if (fireSound == null)
        {
            Debug.LogWarning(
                "[Gun] Fire Sound chưa được gán.",
                this
            );
        }
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;
    }

    public void SetFiring(bool firing)
    {
        bool wasFiring = isFiring;
        isFiring = firing;

        if (!firing)
        {
            fireTimer = 0f;
            return;
        }

        if (!wasFiring)
        {
            fireTimer = Mathf.Min(fireTimer, 0f);
        }
    }

    public Vector3 GetAimPoint()
    {
        return GetTargetPoint();
    }

    private void Update()
    {
        effects?.Tick(Time.deltaTime);

        if (!isFiring)
            return;

        if (currentTarget == null)
            return;

        EnemyHealth targetHealth =
            currentTarget.GetComponentInParent<EnemyHealth>();

        if (targetHealth == null || targetHealth.IsDead)
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

            projectileSpawner?.Spawn(shotDirection.normalized);
        }

        if (cameraController != null)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            cameraController.AddRecoil(
                cameraRecoilPitch,
                cameraRecoilYaw * side,
                cameraRecoilRoll * -side
            );
        }

        if (weaponAim != null)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            weaponAim.AddWeaponRecoil(
                weaponRecoilPosition,
                new Vector3(
                    weaponRecoilEuler.x,
                    weaponRecoilEuler.y * side,
                    weaponRecoilEuler.z * side
                )
            );
        }

        effects?.Play();
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
