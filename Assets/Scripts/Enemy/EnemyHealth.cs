using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    private static readonly List<EnemyHealth> ActiveEnemies =
        new List<EnemyHealth>();

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Death")]
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 2f;
    [SerializeField] private int xpReward = 1;

    [Header("Hit Feedback")]
    [SerializeField] private float headshotHeightRatio = 0.72f;
    [SerializeField] private float hitReactionDuration = 0.09f;
    [SerializeField] private float killReactionScale = 0.16f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.16f, 0.08f, 1f);
    [SerializeField] private Color headshotFlashColor = new Color(1f, 0.92f, 0.2f, 1f);

    [Header("Events")]
    [SerializeField] private UnityEvent onDeath;

    private float currentHealth;
    private bool isDead;
    private Animator animator;
    private EnemyAI enemyAI;
    private PooledObject pooledObject;
    private Collider[] colliders;
    private Renderer[] renderers;
    private MaterialPropertyBlock propertyBlock;
    private Vector3 baseLocalScale;
    private float hitReactionTimer;
    private bool lastHitWasHeadshot;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public int XpReward => xpReward;
    public static IReadOnlyList<EnemyHealth> Enemies => ActiveEnemies;
    public static event Action<EnemyHealth, float, bool, bool> EnemyDamaged;
    public static event Action<EnemyHealth> EnemyKilled;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        enemyAI = GetComponent<EnemyAI>();
        pooledObject = GetComponent<PooledObject>();
        colliders = GetComponentsInChildren<Collider>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
        propertyBlock = new MaterialPropertyBlock();
        baseLocalScale = transform.localScale;

        ResetHealth();
    }

    private void OnEnable()
    {
        if (!ActiveEnemies.Contains(this))
        {
            ActiveEnemies.Add(this);
        }

        ResetHealth();
    }

    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
        transform.localScale = baseLocalScale;
        ClearHitFlash();
    }

    public void SetMaxHealth(float value, bool refill = true)
    {
        maxHealth = Mathf.Max(1f, value);

        if (refill)
        {
            ResetHealth();
        }
    }

    public void SetXpReward(int value)
    {
        xpReward = Mathf.Max(0, value);
    }

    public void ResetHealth()
    {
        CancelInvoke();

        currentHealth = maxHealth;
        isDead = false;
        hitReactionTimer = 0f;
        transform.localScale = baseLocalScale;
        ClearHitFlash();

        if (enemyAI != null)
        {
            enemyAI.enabled = true;
        }
    }

    private void Update()
    {
        if (hitReactionTimer <= 0f)
        {
            return;
        }

        hitReactionTimer -= Time.deltaTime;
        float t = Mathf.Clamp01(hitReactionTimer / hitReactionDuration);
        float punch = (lastHitWasHeadshot ? killReactionScale : killReactionScale * 0.55f) * t;
        transform.localScale = baseLocalScale * (1f + punch);

        if (hitReactionTimer <= 0f)
        {
            transform.localScale = baseLocalScale;
            ClearHitFlash();
        }
    }

    public void TakeDamage(
        float damage,
        Vector3 hitPoint,
        Vector3 hitDirection)
    {
        TakeDamage(damage, hitPoint, hitDirection, false);
    }

    public void TakeDamage(
        float damage,
        Vector3 hitPoint,
        Vector3 hitDirection,
        bool headshot)
    {
        if (isDead)
            return;

        if (damage <= 0f)
            return;

        currentHealth -= damage;
        bool killed = currentHealth <= 0f;

        PlayHitReaction(headshot, killed);
        EnemyDamaged?.Invoke(this, damage, headshot, killed);

        Debug.Log(
            $"{gameObject.name} took {damage} damage. HP: {currentHealth}"
        );

        if (killed)
        {
            Die(hitDirection);
        }
    }

    public bool IsHeadshotPoint(Collider hitCollider, Vector3 hitPoint)
    {
        if (hitCollider != null &&
            hitCollider.name.ToLowerInvariant().Contains("head"))
        {
            return true;
        }

        Bounds bounds = GetBounds();
        if (bounds.size.y <= 0.001f)
        {
            return false;
        }

        float normalizedHeight =
            Mathf.InverseLerp(bounds.min.y, bounds.max.y, hitPoint.y);

        return normalizedHeight >= headshotHeightRatio;
    }

    private Bounds GetBounds()
    {
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider targetCollider = colliders[i];
            if (targetCollider == null || !targetCollider.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = targetCollider.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(targetCollider.bounds);
        }

        return bounds;
    }

    private void PlayHitReaction(bool headshot, bool killed)
    {
        lastHitWasHeadshot = headshot || killed;
        hitReactionTimer = hitReactionDuration;

        Color color = headshot || killed ? headshotFlashColor : hitFlashColor;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void ClearHitFlash()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i]?.SetPropertyBlock(null);
        }
    }

    private void Die(Vector3 hitDirection)
    {
        if (isDead)
            return;

        isDead = true;
        ActiveEnemies.Remove(this);

        Debug.Log($"{gameObject.name} died.");

        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("Death");
        }

        onDeath.Invoke();
        EnemyKilled?.Invoke(this);

        ZombieSpawner.NotifyZombieKilled(this);

        if (destroyOnDeath)
        {
            Invoke(nameof(Despawn), destroyDelay);
        }
    }

    private void Despawn()
    {
        if (pooledObject != null && pooledObject.HasPool)
        {
            pooledObject.Release();
            return;
        }

        Destroy(gameObject);
    }
}
