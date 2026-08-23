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
    private bool hasShootingParameter;

    public Transform CurrentTarget => currentEnemy;
    public Transform WeaponPivot => weaponPivot;
    public bool HasTargetInRange =>
        currentEnemy != null && IsTargetInRange();

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

        hasShootingParameter =
            HasAnimatorParameter(
                "IsShooting",
                AnimatorControllerParameterType.Bool
            );

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

        if (animator != null &&
            hasShootingParameter)
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
            target.GetComponentInParent<EnemyHealth>();

        if (health == null || health.IsDead)
            return false;

        return IsTargetInRange();
    }

    private bool HasAnimatorParameter(
        string parameterName,
        AnimatorControllerParameterType parameterType)
    {
        if (animator == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters =
            animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName &&
                parameters[i].type == parameterType)
            {
                return true;
            }
        }

        return false;
    }
}
