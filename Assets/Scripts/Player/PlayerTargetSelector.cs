using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerTargetSelector
{
    private readonly Transform owner;
    private readonly float range;
    private readonly float rangeSqr;

    public PlayerTargetSelector(Transform owner, float range)
    {
        this.owner = owner;
        this.range = range;
        rangeSqr = range * range;
    }

    public Transform FindNearestEnemy()
    {
        Transform nearest = null;
        float nearestDistanceSqr = rangeSqr;
        IReadOnlyList<EnemyHealth> enemies = EnemyHealth.Enemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealth health = enemies[i];

            if (health == null || health.IsDead)
            {
                continue;
            }

            Transform enemy = health.transform;

            if (!enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            float distanceSqr =
                (enemy.position - owner.position).sqrMagnitude;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearest = enemy;
                nearestDistanceSqr = distanceSqr;
            }
        }

        return nearest;
    }

    public bool IsValid(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            return false;
        }

        EnemyHealth health = target.GetComponentInParent<EnemyHealth>();
        return health != null && !health.IsDead && IsInRange(target);
    }

    public bool IsInRange(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        return (target.position - owner.position).sqrMagnitude <= rangeSqr;
    }
}