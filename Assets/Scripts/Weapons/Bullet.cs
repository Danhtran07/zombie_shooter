using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float headshotDamageMultiplier = 1.8f;

    private Rigidbody rb;
    private PooledObject pooledObject;
    private Collider[] bulletColliders;
    private GameObject owner;
    private Vector3 travelDirection;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pooledObject = GetComponent<PooledObject>();

        rb.useGravity = false;
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        bulletColliders = GetComponentsInChildren<Collider>();

        foreach (Collider bulletCollider in bulletColliders)
        {
            if (bulletCollider != null)
                bulletCollider.isTrigger = true;
        }
    }

    public void Initialize(
        GameObject bulletOwner,
        Vector3 direction,
        float speed,
        float damageAmount,
        float lifeTime)
    {
        direction = direction.normalized;
        owner = bulletOwner;
        damage = damageAmount;
        lifetime = lifeTime;
        travelDirection = direction;
        hasHit = false;

        transform.SetPositionAndRotation(
            transform.position,
            Quaternion.LookRotation(direction)
        );

        rb.position = transform.position;
        rb.rotation = transform.rotation;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        IgnoreOwnerCollision();
        IgnorePlayerCollision();

        rb.velocity = direction * speed;

        CancelInvoke();
        Invoke(nameof(Expire), lifetime);
    }

    private void IgnoreOwnerCollision()
    {
        if (owner == null)
            return;

        Collider[] ownerColliders =
            owner.GetComponentsInChildren<Collider>();

        foreach (Collider bulletCollider in bulletColliders)
        {
            if (bulletCollider == null)
                continue;

            foreach (Collider ownerCollider in ownerColliders)
            {
                if (ownerCollider == null)
                    continue;

                Physics.IgnoreCollision(
                    bulletCollider,
                    ownerCollider,
                    true
                );
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit)
            return;

        ProcessHit(
            collision.collider,
            collision.GetContact(0).point,
            collision.GetContact(0).normal
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit)
            return;

        Vector3 hitPoint =
            other.ClosestPoint(transform.position);

        Vector3 direction =
            (other.transform.position - transform.position)
            .normalized;

        ProcessHit(
            other,
            hitPoint,
            direction.sqrMagnitude > 0.001f ? -direction : -travelDirection
        );
    }

    private void ProcessHit(
        Collider hitCollider,
        Vector3 hitPoint,
        Vector3 hitDirection)
    {
        if (hitCollider == null)
            return;

        if (owner != null &&
            hitCollider.transform.root.gameObject == owner)
        {
            return;
        }

        if (hitCollider.CompareTag("Player") ||
            hitCollider.GetComponentInParent<PlayerHealth>() != null)
        {
            IgnoreCollider(hitCollider);
            return;
        }

        EnemyHealth enemy =
            hitCollider.GetComponentInParent<EnemyHealth>();

        if (enemy == null || enemy.IsDead)
        {
            hasHit = true;
            BulletImpactFeedback.Instance?.Play(
                hitPoint,
                hitDirection,
                false,
                false,
                false
            );
            Expire();
            return;
        }

        hasHit = true;
        bool headshot = enemy.IsHeadshotPoint(hitCollider, hitPoint);
        float finalDamage = headshot
            ? damage * headshotDamageMultiplier
            : damage;

        enemy.TakeDamage(
            finalDamage,
            hitPoint,
            travelDirection,
            headshot
        );

        BulletImpactFeedback.Instance?.Play(
            hitPoint,
            -travelDirection,
            true,
            headshot,
            enemy.IsDead
        );

        Expire();
    }

    private void IgnorePlayerCollision()
    {
        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            return;

        Collider[] playerColliders =
            player.GetComponentsInChildren<Collider>();

        foreach (Collider playerCollider in playerColliders)
        {
            IgnoreCollider(playerCollider);
        }
    }

    private void IgnoreCollider(Collider other)
    {
        if (other == null)
            return;

        foreach (Collider bulletCollider in bulletColliders)
        {
            if (bulletCollider == null)
                continue;

            Physics.IgnoreCollision(
                bulletCollider,
                other,
                true
            );
        }
    }

    private void Expire()
    {
        CancelInvoke();

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (pooledObject != null && pooledObject.HasPool)
        {
            pooledObject.Release();
            return;
        }

        Destroy(gameObject);
    }
}
