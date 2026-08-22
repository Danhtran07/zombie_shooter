using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private Animator animator;
    [SerializeField] private Gun gun;

    private Transform currentEnemy;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        Transform nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != currentEnemy)
            currentEnemy = nearestEnemy;

        bool inRange = currentEnemy != null &&
                       Vector3.Distance(transform.position, currentEnemy.position) <= attackRange;

        if (animator != null)
            animator.SetBool("IsShooting", inRange);

        if (gun != null)
        {
            gun.SetTarget(currentEnemy);
            gun.SetFiring(inRange);
        }
    }

    private void LateUpdate()
    {
        if (currentEnemy != null &&
            Vector3.Distance(transform.position, currentEnemy.position) <= attackRange)
        {
            AimAtEnemy();
        }
    }

    private Transform FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;
        float nearestDistanceSqr = float.PositiveInfinity;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null || !enemy.activeInHierarchy)
                continue;

            float distanceSqr = (enemy.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr < nearestDistanceSqr)
            {
                nearest = enemy.transform;
                nearestDistanceSqr = distanceSqr;
            }
        }

        return nearest;
    }

    private void AimAtEnemy()
    {
        Vector3 direction =
            currentEnemy.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}