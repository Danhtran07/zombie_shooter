using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 8f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float damage = 10f;

    private Animator animator;
    private NavMeshAgent agent;
    private EnemyHealth health;
    private float baseMoveSpeed;
    private float baseDamage;
    private float baseMaxHealth;
    private float attackTimer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();
        baseMoveSpeed = moveSpeed;
        baseDamage = damage;
        baseMaxHealth = health != null ? health.MaxHealth : 1f;

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackRange;
            agent.updateRotation = false;
        }
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }
    }

    private void Update()
    {
        if (player == null)
            return;

        attackTimer -= Time.deltaTime;

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance <= attackRange)
        {
            Attack();
        }
        else
        {
            ChasePlayer();
        }
    }

    private void ChasePlayer()
    {
        // QUAN TRỌNG:
        // Reset Attack Trigger khi Player chạy xa
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetFloat("Speed", 1f);
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = moveSpeed;
            agent.SetDestination(player.position);
        }

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                transform.position +=
                    transform.forward *
                    moveSpeed *
                    Time.deltaTime;
            }
        }
    }

    private void Attack()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }

        // Quay mặt về Player
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // Chỉ attack khi cooldown hết
        if (attackTimer <= 0f)
        {
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            PlayerHealth playerHealth =
                player.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }

            attackTimer = attackCooldown;
        }
    }

    public void SetTarget(Transform target)
    {
        player = target;
    }

    public void SetStats(
        float healthMultiplier,
        float speedMultiplier,
        float damageMultiplier)
    {
        moveSpeed =
            baseMoveSpeed *
            Mathf.Max(0.01f, speedMultiplier);

        damage =
            baseDamage *
            Mathf.Max(0f, damageMultiplier);

        if (agent != null)
        {
            agent.speed = moveSpeed;
        }

        if (health != null)
        {
            health.SetMaxHealth(
                baseMaxHealth *
                Mathf.Max(0.01f, healthMultiplier)
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );
    }
}
