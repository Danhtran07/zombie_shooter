using UnityEngine;

public sealed class GunProjectileSpawner
{
    private readonly Transform muzzle;
    private readonly GameObject bulletPrefab;
    private readonly ObjectPool bulletPool;
    private readonly GameObject owner;
    private readonly float speed;
    private readonly float damage;
    private readonly float lifetime;
    private readonly float criticalChance;
    private readonly float criticalMultiplier;

    public GunProjectileSpawner(
        Transform muzzle,
        GameObject bulletPrefab,
        ObjectPool bulletPool,
        GameObject owner,
        float speed,
        float damage,
        float lifetime,
        float criticalChance,
        float criticalMultiplier)
    {
        this.muzzle = muzzle;
        this.bulletPrefab = bulletPrefab;
        this.bulletPool = bulletPool;
        this.owner = owner;
        this.speed = speed;
        this.damage = damage;
        this.lifetime = lifetime;
        this.criticalChance = criticalChance;
        this.criticalMultiplier = criticalMultiplier;
    }

    public void Spawn(Vector3 direction)
    {
        Quaternion rotation = Quaternion.LookRotation(direction);
        GameObject bulletObject = bulletPool != null
            ? bulletPool.Spawn(muzzle.position, rotation)
            : Object.Instantiate(bulletPrefab, muzzle.position, rotation);

        if (bulletObject == null)
        {
            return;
        }

        Bullet bullet = bulletObject.GetComponent<Bullet>();
        if (bullet == null)
        {
            Debug.LogError("Bullet prefab does not contain Bullet component.");
            return;
        }

        float finalDamage = Random.value <= criticalChance
            ? damage * criticalMultiplier
            : damage;

        bullet.Initialize(owner, direction, speed, finalDamage, lifetime);
    }
}