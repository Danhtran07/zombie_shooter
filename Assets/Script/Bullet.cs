using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float damage = 10f;

    private Transform target;
    private float lifetime;

    public void Initialize(GameObject owner, Transform targetTransform, float lifetimeSeconds)
    {
        target = targetTransform;
        lifetime = Mathf.Max(0.01f, lifetimeSeconds);
        CancelInvoke(nameof(DestroyBullet));
        Invoke(nameof(DestroyBullet), lifetime);

        Collider bulletCollider = GetComponent<Collider>();
        if (owner != null && bulletCollider != null)
        {
            foreach (Collider ownerCollider in owner.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(bulletCollider, ownerCollider);
        }
    }

    private void Awake()
    {
        lifetime = 5f;
        Invoke(nameof(DestroyBullet), lifetime);
    }

    private void DestroyBullet()
    {
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void HandleHit(Collider hitCollider)
    {
        if (hitCollider.transform == transform || hitCollider.CompareTag("Player"))
            return;

        if (target != null && hitCollider.transform.IsChildOf(target) ||
            target != null && hitCollider.transform == target)
        {
            hitCollider.SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            hitCollider.SendMessageUpwards("Damage", damage, SendMessageOptions.DontRequireReceiver);
            Destroy(gameObject);
            return;
        }

        if (hitCollider.CompareTag("Enemy"))
        {
            hitCollider.SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            hitCollider.SendMessageUpwards("Damage", damage, SendMessageOptions.DontRequireReceiver);
            Destroy(gameObject);
        }
    }
}