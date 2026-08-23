using UnityEngine;
using UnityEngine.Animations.Rigging;

[DefaultExecutionOrder(50)]
public class PlayerWeaponAim : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private Gun gun;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Transform rightHandTarget;
    [SerializeField] private Transform leftHandTarget;

    [Header("Gun Aim")]
    [SerializeField] private float aimTurnSpeed = 12f;
    [SerializeField] private float restReturnSpeed = 8f;
    [SerializeField] private float maxYawFromBody = 80f;

    [Header("Hand Grip")]
    [SerializeField] private Vector3 rightHandLocalPosition = new Vector3(0.06f, -0.05f, 0.04f);
    [SerializeField] private Vector3 rightHandLocalEuler = new Vector3(0f, 0f, -90f);
    [SerializeField] private Vector3 leftHandLocalPosition = new Vector3(0.05f, -0.03f, 0.32f);
    [SerializeField] private Vector3 leftHandLocalEuler = new Vector3(0f, 0f, -90f);
    [SerializeField] private bool applyGripOffsets = true;

    private Quaternion restLocalRotation;
    private Vector3 restLocalPosition;
    private bool hasRestPose;
    private Transform rightHint;
    private Transform leftHint;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureWeaponTargets();
        CaptureRestPose();
        SetupTwoBoneIK();
    }

    private void Update()
    {
        ResolveReferences();
        EnsureWeaponTargets();
        UpdateGripTargets();
        UpdateGunAim();
        UpdateElbowHints();
    }

    private void ResolveReferences()
    {
        if (combat == null)
        {
            combat = GetComponent<PlayerCombat>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (gun == null)
        {
            gun = GetComponentInChildren<Gun>();
        }

        if (weaponPivot == null && gun != null)
        {
            weaponPivot = gun.transform;
        }

        if (weaponPivot == null && combat != null)
        {
            weaponPivot = combat.WeaponPivot;
        }
    }

    private void CaptureRestPose()
    {
        if (weaponPivot == null || hasRestPose)
        {
            return;
        }

        restLocalRotation = weaponPivot.localRotation;
        restLocalPosition = weaponPivot.localPosition;
        hasRestPose = true;
    }

    private void EnsureWeaponTargets()
    {
        if (weaponPivot == null)
        {
            return;
        }

        if (rightHandTarget == null)
        {
            rightHandTarget = FindOrCreateChild(weaponPivot, "RightHandTarget");
            rightHandTarget.localPosition = rightHandLocalPosition;
            rightHandTarget.localRotation = Quaternion.Euler(rightHandLocalEuler);
        }

        if (leftHandTarget == null)
        {
            leftHandTarget = FindOrCreateChild(weaponPivot, "LeftHandTarget");
            leftHandTarget.localPosition = leftHandLocalPosition;
            leftHandTarget.localRotation = Quaternion.Euler(leftHandLocalEuler);
        }
    }

    private void UpdateGripTargets()
    {
        if (!applyGripOffsets)
        {
            return;
        }

        if (rightHandTarget != null)
        {
            rightHandTarget.localPosition = rightHandLocalPosition;
            rightHandTarget.localRotation = Quaternion.Euler(rightHandLocalEuler);
        }

        if (leftHandTarget != null)
        {
            leftHandTarget.localPosition = leftHandLocalPosition;
            leftHandTarget.localRotation = Quaternion.Euler(leftHandLocalEuler);
        }
    }

    private void UpdateGunAim()
    {
        CaptureRestPose();

        if (weaponPivot == null)
        {
            return;
        }

        bool hasTarget = combat != null && combat.HasTargetInRange && combat.CurrentTarget != null;

        if (!hasTarget)
        {
            weaponPivot.localRotation = Quaternion.Slerp(
                weaponPivot.localRotation,
                restLocalRotation,
                restReturnSpeed * Time.deltaTime
            );
            weaponPivot.localPosition = restLocalPosition;
            return;
        }

        Vector3 aimPoint = gun != null
            ? gun.GetAimPoint()
            : combat.CurrentTarget.position + Vector3.up;

        Vector3 worldDirection = aimPoint - weaponPivot.position;
        if (worldDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = GetClampedAimRotation(worldDirection.normalized);
        weaponPivot.rotation = Quaternion.Slerp(
            weaponPivot.rotation,
            targetRotation,
            aimTurnSpeed * Time.deltaTime
        );
    }

    private Quaternion GetClampedAimRotation(Vector3 worldDirection)
    {
        Vector3 localDirection = transform.InverseTransformDirection(worldDirection);
        float yaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Asin(Mathf.Clamp(localDirection.y, -1f, 1f)) * Mathf.Rad2Deg;

        yaw = Mathf.Clamp(yaw, -maxYawFromBody, maxYawFromBody);
        pitch = Mathf.Clamp(pitch, -35f, 45f);

        Vector3 clampedLocal = Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
        return Quaternion.LookRotation(transform.TransformDirection(clampedLocal), Vector3.up);
    }

    private void SetupTwoBoneIK()
    {
        if (animator == null || weaponPivot == null)
        {
            return;
        }

        Transform rightArm = FindBone("RightArm");
        Transform rightForeArm = FindBone("RightForeArm");
        Transform rightHand = FindBone("RightHand");
        Transform leftArm = FindBone("LeftArm");
        Transform leftForeArm = FindBone("LeftForeArm");
        Transform leftHand = FindBone("LeftHand");

        if (rightArm == null || rightForeArm == null || rightHand == null ||
            leftArm == null || leftForeArm == null || leftHand == null)
        {
            Debug.LogWarning(
                "[PlayerWeaponAim] Mixamo arm bones were not found. Two Bone IK was not created."
            );
            return;
        }

        RigBuilder rigBuilder = animator.GetComponent<RigBuilder>();
        if (rigBuilder == null)
        {
            rigBuilder = animator.gameObject.AddComponent<RigBuilder>();
        }

        Transform rigTransform = animator.transform.Find("WeaponIK");
        if (rigTransform == null)
        {
            GameObject rigObject = new GameObject("WeaponIK");
            rigObject.transform.SetParent(animator.transform, false);
            rigTransform = rigObject.transform;
        }

        Rig rig = rigTransform.GetComponent<Rig>();
        if (rig == null)
        {
            rig = rigTransform.gameObject.AddComponent<Rig>();
        }

        rightHint = FindOrCreateChild(rigTransform, "RightElbowHint");
        leftHint = FindOrCreateChild(rigTransform, "LeftElbowHint");

        EnsureTwoBoneIK(
            rigTransform,
            "RightHandIK",
            rightArm,
            rightForeArm,
            rightHand,
            rightHandTarget,
            rightHint
        );

        EnsureTwoBoneIK(
            rigTransform,
            "LeftHandIK",
            leftArm,
            leftForeArm,
            leftHand,
            leftHandTarget,
            leftHint
        );

        bool alreadyAdded = false;
        for (int i = 0; i < rigBuilder.layers.Count; i++)
        {
            if (rigBuilder.layers[i].rig == rig)
            {
                alreadyAdded = true;
                break;
            }
        }

        if (!alreadyAdded)
        {
            rigBuilder.layers.Add(new RigLayer(rig, true));
        }

        rigBuilder.Build();
    }

    private void EnsureTwoBoneIK(
        Transform rigTransform,
        string name,
        Transform root,
        Transform mid,
        Transform tip,
        Transform target,
        Transform hint)
    {
        Transform constraintTransform = rigTransform.Find(name);
        if (constraintTransform == null)
        {
            GameObject constraintObject = new GameObject(name);
            constraintObject.transform.SetParent(rigTransform, false);
            constraintTransform = constraintObject.transform;
        }

        TwoBoneIKConstraint constraint = constraintTransform.GetComponent<TwoBoneIKConstraint>();
        if (constraint == null)
        {
            constraint = constraintTransform.gameObject.AddComponent<TwoBoneIKConstraint>();
        }

        TwoBoneIKConstraintData data = constraint.data;
        data.root = root;
        data.mid = mid;
        data.tip = tip;
        data.target = target;
        data.hint = hint;
        data.targetPositionWeight = 1f;
        data.targetRotationWeight = 1f;
        data.hintWeight = 1f;
        constraint.data = data;
        constraint.weight = 1f;
    }

    private void UpdateElbowHints()
    {
        if (rightHint != null)
        {
            rightHint.position =
                transform.position +
                Vector3.up * 1.15f +
                transform.right * 0.28f -
                transform.forward * 0.18f;
        }

        if (leftHint != null)
        {
            leftHint.position =
                transform.position +
                Vector3.up * 1.15f -
                transform.right * 0.28f -
                transform.forward * 0.18f;
        }
    }

    private Transform FindBone(string boneName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            string name = children[i].name;
            if (name == boneName ||
                name == "mixamorig:" + boneName ||
                name.EndsWith(boneName))
            {
                return children[i];
            }
        }

        return null;
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
        {
            return existing;
        }

        Transform renamed = parent.Find(childName.Replace("Target", "IKTarget"));
        if (renamed != null)
        {
            renamed.name = childName;
            return renamed;
        }

        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        return child.transform;
    }
}
