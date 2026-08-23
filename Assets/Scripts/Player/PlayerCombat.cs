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
    private PlayerTargetSelector targetSelector;

    public Transform CurrentTarget => currentEnemy;
    public Transform WeaponPivot => weaponPivot;
    public bool HasTargetInRange =>
        targetSelector != null && targetSelector.IsInRange(currentEnemy);

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

        targetSelector = new PlayerTargetSelector(transform, attackRange);
    }

    private void Update()
    {
        if (!targetSelector.IsValid(currentEnemy))
        {
            currentEnemy = null;
            targetTimer = 0f;
        }

        targetTimer -= Time.deltaTime;

        if (targetTimer <= 0f)
        {
            currentEnemy = targetSelector.FindNearestEnemy();

            targetTimer = targetRefreshRate;
        }

        bool hasTarget =
            currentEnemy != null;

        bool inRange =
            hasTarget &&
            targetSelector.IsInRange(currentEnemy);

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
