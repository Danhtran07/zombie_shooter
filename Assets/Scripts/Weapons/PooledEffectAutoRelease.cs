using UnityEngine;

public class PooledEffectAutoRelease : MonoBehaviour
{
    [SerializeField] private float fallbackLifetime = 1.25f;

    private ParticleSystem[] particles;
    private PooledObject pooledObject;
    private float lifetime;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
        particles = GetComponentsInChildren<ParticleSystem>(true);
        lifetime = fallbackLifetime;

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];
            if (particle == null)
            {
                continue;
            }

            ParticleSystem.MainModule main = particle.main;
            lifetime = Mathf.Max(
                lifetime,
                main.duration + main.startLifetime.constantMax
            );
        }
    }

    private void OnEnable()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i]?.Play(true);
        }

        CancelInvoke();
        Invoke(nameof(Release), lifetime);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void Release()
    {
        if (pooledObject != null && pooledObject.HasPool)
        {
            pooledObject.Release();
            return;
        }

        gameObject.SetActive(false);
    }
}
