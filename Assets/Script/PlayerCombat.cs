using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private float attackRange = 14f;
    [SerializeField] private float targetRefreshRate = 0.08f;

    [Header("References")]
    [SerializeField] private Gun gun;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform weaponPivot;

    private Transform currentEnemy;
    private float targetTimer;

    private void Awake()
    {
        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }

        if (gun == null)
        {
            gun = GetComponentInChildren<Gun>();
        }

        if (weaponPivot == null && gun != null)
        {
            weaponPivot = gun.transform;
        }
    }

    private void Update()
    {
        if (!IsValidTarget(currentEnemy))
        {
            currentEnemy = null;
            targetTimer = 0f;
        }

        targetTimer -= Time.deltaTime;

        if (targetTimer <= 0f)
        {
            FindNearestEnemy();

            targetTimer = targetRefreshRate;
        }

        bool hasTarget =
            currentEnemy != null;

        bool inRange =
            hasTarget &&
            IsTargetInRange();

        if (animator != null)
        {
            animator.SetBool(
                "IsShooting",
                inRange
            );
        }

        if (gun != null)
        {
            gun.SetTarget(currentEnemy);
            gun.SetFiring(inRange);
        }
    }

    private bool IsTargetInRange()
    {
        if (currentEnemy == null)
            return false;

        float distanceSqr =
            (currentEnemy.position - transform.position)
            .sqrMagnitude;

        return distanceSqr <=
               attackRange * attackRange;
    }

    private void FindNearestEnemy()
    {
        Transform nearest = null;

        float nearestDistanceSqr =
            attackRange * attackRange;

        IReadOnlyList<EnemyHealth> enemies =
            EnemyHealth.Enemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealth health = enemies[i];

            if (health == null)
                continue;

            if (health.IsDead)
                continue;

            Transform enemyTransform = health.transform;

            if (!enemyTransform.gameObject.activeInHierarchy)
                continue;

            float distanceSqr =
                (enemyTransform.position -
                 transform.position)
                .sqrMagnitude;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearest = enemyTransform;
                nearestDistanceSqr = distanceSqr;
            }
        }

        currentEnemy = nearest;
    }

    private bool IsValidTarget(Transform target)
    {
        if (target == null)
            return false;

        if (!target.gameObject.activeInHierarchy)
            return false;

        EnemyHealth health =
            target.GetComponent<EnemyHealth>();

        if (health != null && health.IsDead)
            return false;

        return IsTargetInRange();
    }

    private void LateUpdate()
    {
        if (currentEnemy == null)
            return;

        if (weaponPivot == null)
            return;

        if (!IsTargetInRange())
            return;

        Vector3 direction =
            currentEnemy.position -
            weaponPivot.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        weaponPivot.rotation =
            Quaternion.Slerp(
                weaponPivot.rotation,
                targetRotation,
                18f * Time.deltaTime
            );
    }
}
