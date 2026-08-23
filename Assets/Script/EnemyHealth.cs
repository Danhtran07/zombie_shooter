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

    [Header("Events")]
    [SerializeField] private UnityEvent onDeath;

    private float currentHealth;
    private bool isDead;
    private Animator animator;
    private EnemyAI enemyAI;
    private PooledObject pooledObject;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public int XpReward => xpReward;
    public static IReadOnlyList<EnemyHealth> Enemies => ActiveEnemies;
    public static event Action<EnemyHealth> EnemyKilled;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        enemyAI = GetComponent<EnemyAI>();
        pooledObject = GetComponent<PooledObject>();

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

        if (enemyAI != null)
        {
            enemyAI.enabled = true;
        }
    }

    public void TakeDamage(
        float damage,
        Vector3 hitPoint,
        Vector3 hitDirection)
    {
        if (isDead)
            return;

        if (damage <= 0f)
            return;

        currentHealth -= damage;

        Debug.Log(
            $"{gameObject.name} took {damage} damage. HP: {currentHealth}"
        );

        if (currentHealth <= 0f)
        {
            Die(hitDirection);
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
