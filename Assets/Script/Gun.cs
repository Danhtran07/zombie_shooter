using UnityEngine;

public class Gun : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float fireRate = 0.1f;
    [SerializeField] private float bulletLifetime = 5f;

    private Transform currentEnemy;
    private bool isFiring;
    private float timer;

    public void SetTarget(Transform target)
    {
        currentEnemy = target;
    }

    public void SetFiring(bool firing)
    {
        isFiring = firing;
        if (!firing)
            timer = 0f;
    }

    private void Update()
    {
        if (!isFiring || currentEnemy == null || bulletPrefab == null)
            return;

        AimAtTarget();
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            Fire();
            timer = Mathf.Max(0.01f, fireRate);
        }
    }

    private void AimAtTarget()
    {
        Vector3 direction = currentEnemy.position - transform.position;
        if (direction.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private void Fire()
    {
        if (currentEnemy == null)
            return;

        Vector3 direction = (currentEnemy.position - transform.position).normalized;

        GameObject bullet = Instantiate(
            bulletPrefab,
            transform.position,
            Quaternion.LookRotation(direction)
        );

        Rigidbody rb = bullet.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.velocity = direction * bulletSpeed;
        }

        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null)
            bulletComponent.Initialize(transform.root.gameObject, currentEnemy, bulletLifetime);
    }
}