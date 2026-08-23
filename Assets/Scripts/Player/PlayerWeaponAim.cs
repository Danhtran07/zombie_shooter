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
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;
    [SerializeField] private Transform aimTarget;

    [Header("Gun Aim")]
    [SerializeField] private Vector3 leftHandLocalPosition = new Vector3(0.05f, -0.03f, 0.32f);
    [SerializeField] private Vector3 leftHandLocalEuler = new Vector3(0f, 0f, -90f);
    [SerializeField] private Vector3 rightHandLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 rightHandLocalEuler = Vector3.zero;
    [SerializeField] private Vector3 stableWeaponLocalPosition;
    [SerializeField] private Vector3 stableWeaponLocalEuler;
    [SerializeField] private bool stabilizeWeapon = true;
    [SerializeField] private bool applyGripOffsets = true;
    [SerializeField] private float recoilReturnSpeed = 18f;

    private Transform leftHint;
    private Transform rightHint;
    private ThirdPersonController playerController;
    private Transform rightHandBone;
    private bool capturedStablePose;
    private Vector3 recoilLocalPosition;
    private Vector3 recoilLocalEuler;

    private void Reset()
    {
        ResolveReferences();
    }

    public void ConfigureWeaponAimNow()
    {
        ResolveReferences();
        AttachGunToHand();
        EnsureWeaponTargets();
        CaptureStableWeaponPoseIfNeeded();
        CaptureRightHandGrip();
        UpdateGripTargets();
        SetupTwoBoneIK();
        StabilizeWeaponPose();
    }

    private void Awake()
    {
        ResolveReferences();
        AttachGunToHand();
        EnsureWeaponTargets();
        CaptureStableWeaponPoseIfNeeded();
        SetupTwoBoneIK();
    }

    private void Update()
    {
        ResolveReferences();
        AttachGunToHand();
        EnsureWeaponTargets();
        UpdateGripTargets();
        UpdateAim();
        UpdateElbowHints();
    }

    private void LateUpdate()
    {
        UpdateWeaponRecoil();
        StabilizeWeaponPose();
    }

    public void AddWeaponRecoil(
        Vector3 positionKick,
        Vector3 eulerKick,
        float maxPositionKick = 0.18f,
        float maxRotationKick = 9f)
    {
        recoilLocalPosition += positionKick;
        recoilLocalEuler += eulerKick;

        recoilLocalPosition = Vector3.ClampMagnitude(
            recoilLocalPosition,
            maxPositionKick
        );

        recoilLocalEuler.x = Mathf.Clamp(
            recoilLocalEuler.x,
            -maxRotationKick,
            maxRotationKick
        );
        recoilLocalEuler.y = Mathf.Clamp(
            recoilLocalEuler.y,
            -maxRotationKick,
            maxRotationKick
        );
        recoilLocalEuler.z = Mathf.Clamp(
            recoilLocalEuler.z,
            -maxRotationKick,
            maxRotationKick
        );
    }

    public void AttachGunToHand()
    {
        if (gun == null)
        {
            return;
        }

        rightHandBone = FindRightHand();
        if (rightHandBone == null)
        {
            return;
        }

        if (gun.transform.parent != rightHandBone)
        {
            gun.transform.SetParent(rightHandBone, true);
        }

        weaponPivot = gun.transform;
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

        if (playerController == null)
        {
            playerController = GetComponent<ThirdPersonController>();
        }

        if (aimTarget == null)
        {
            aimTarget = FindOrCreateChild(transform, "AimTarget");
        }
    }

    private void EnsureWeaponTargets()
    {
        if (weaponPivot == null)
        {
            return;
        }

        if (leftHandTarget == null)
        {
            leftHandTarget = FindOrCreateChild(weaponPivot, "LeftHandTarget");
            leftHandTarget.localPosition = leftHandLocalPosition;
            leftHandTarget.localRotation = Quaternion.Euler(leftHandLocalEuler);
        }

        if (rightHandTarget == null)
        {
            rightHandTarget = FindOrCreateChild(weaponPivot, "RightHandTarget");
            CaptureRightHandGrip();
        }
    }

    private void UpdateGripTargets()
    {
        if (!applyGripOffsets)
        {
            return;
        }

        if (leftHandTarget != null)
        {
            leftHandTarget.localPosition = leftHandLocalPosition;
            leftHandTarget.localRotation = Quaternion.Euler(leftHandLocalEuler);
        }

        if (rightHandTarget != null)
        {
            rightHandTarget.localPosition = rightHandLocalPosition;
            rightHandTarget.localRotation = Quaternion.Euler(rightHandLocalEuler);
        }
    }

    private void UpdateAim()
    {
        Transform target = combat != null ? combat.CurrentTarget : null;
        bool hasTarget = combat != null && combat.HasTargetInRange && target != null;
        if (!hasTarget)
        {
            playerController?.ClearAimDirection();
            return;
        }

        Vector3 aimPoint = gun != null
            ? gun.GetAimPoint()
            : target.position + Vector3.up;

        if (aimTarget != null)
        {
            aimTarget.position = aimPoint;
        }

        Vector3 bodyDirection = aimPoint - transform.position;
        bodyDirection.y = 0f;
        playerController?.SetAimDirection(bodyDirection);
    }

    private void CaptureStableWeaponPoseIfNeeded()
    {
        if (capturedStablePose || weaponPivot == null)
        {
            return;
        }

        if (stableWeaponLocalPosition == Vector3.zero &&
            stableWeaponLocalEuler == Vector3.zero)
        {
            stableWeaponLocalPosition =
                transform.InverseTransformPoint(weaponPivot.position);

            stableWeaponLocalEuler =
                (Quaternion.Inverse(transform.rotation) * weaponPivot.rotation)
                .eulerAngles;
        }

        capturedStablePose = true;
    }

    private void CaptureRightHandGrip()
    {
        if (rightHandTarget == null ||
            weaponPivot == null ||
            rightHandBone == null)
        {
            return;
        }

        rightHandLocalPosition =
            weaponPivot.InverseTransformPoint(rightHandBone.position);

        rightHandLocalEuler =
            (Quaternion.Inverse(weaponPivot.rotation) * rightHandBone.rotation)
            .eulerAngles;

        rightHandTarget.localPosition = rightHandLocalPosition;
        rightHandTarget.localRotation = Quaternion.Euler(rightHandLocalEuler);
    }

    private void StabilizeWeaponPose()
    {
        if (!stabilizeWeapon || weaponPivot == null)
        {
            return;
        }

        CaptureStableWeaponPoseIfNeeded();

        Vector3 stablePosition =
            transform.TransformPoint(stableWeaponLocalPosition);

        Quaternion stableRotation =
            transform.rotation *
            Quaternion.Euler(stableWeaponLocalEuler);

        Transform target = combat != null ? combat.CurrentTarget : null;
        bool hasTarget = combat != null && combat.HasTargetInRange && target != null;

        if (hasTarget)
        {
            Vector3 aimPoint = gun != null
                ? gun.GetAimPoint()
                : target.position + Vector3.up;

            Vector3 aimDirection = aimPoint - stablePosition;
            if (aimDirection.sqrMagnitude > 0.001f)
            {
                stableRotation =
                    Quaternion.LookRotation(aimDirection.normalized, Vector3.up);
            }
        }

        stablePosition += stableRotation * recoilLocalPosition;
        stableRotation *= Quaternion.Euler(recoilLocalEuler);

        weaponPivot.SetPositionAndRotation(stablePosition, stableRotation);
    }

    private void UpdateWeaponRecoil()
    {
        float step = recoilReturnSpeed * Time.deltaTime;
        recoilLocalPosition = Vector3.MoveTowards(
            recoilLocalPosition,
            Vector3.zero,
            step * 0.025f
        );

        recoilLocalEuler = Vector3.MoveTowards(
            recoilLocalEuler,
            Vector3.zero,
            step
        );
    }

    private void SetupTwoBoneIK()
    {
        if (animator == null || weaponPivot == null)
        {
            return;
        }

        Transform leftArm = FindBone("LeftArm");
        Transform leftForeArm = FindBone("LeftForeArm");
        Transform leftHand = FindBone("LeftHand");
        Transform rightArm = FindBone("RightArm");
        Transform rightForeArm = FindBone("RightForeArm");
        Transform rightHand = FindBone("RightHand");

        if (leftArm == null || leftForeArm == null || leftHand == null ||
            rightArm == null || rightForeArm == null || rightHand == null)
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

        leftHint = FindOrCreateChild(rigTransform, "LeftElbowHint");
        rightHint = FindOrCreateChild(rigTransform, "RightElbowHint");

        EnsureTwoBoneIK(
            rigTransform,
            "LeftHandIK",
            leftArm,
            leftForeArm,
            leftHand,
            leftHandTarget,
            leftHint
        );

        EnsureTwoBoneIK(
            rigTransform,
            "RightHandIK",
            rightArm,
            rightForeArm,
            rightHand,
            rightHandTarget,
            rightHint
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
        if (leftHint != null)
        {
            leftHint.position =
                transform.position +
                Vector3.up * 1.15f -
                transform.right * 0.28f -
                transform.forward * 0.18f;
        }

        if (rightHint != null)
        {
            rightHint.position =
                transform.position +
                Vector3.up * 1.15f +
                transform.right * 0.28f -
                transform.forward * 0.18f;
        }
    }

    private Transform FindBone(string boneName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        string normalizedBoneName = boneName
            .Replace("_", string.Empty)
            .ToLowerInvariant();

        for (int i = 0; i < children.Length; i++)
        {
            string name = children[i].name
                .Replace("_", string.Empty)
                .Replace(".", string.Empty)
                .Replace(":", string.Empty)
                .ToLowerInvariant();

            if (name == normalizedBoneName ||
                name.EndsWith(normalizedBoneName))
            {
                return children[i];
            }

            if (normalizedBoneName == "leftarm" &&
                (name == "arml" || name == "upperarml"))
            {
                return children[i];
            }

            if (normalizedBoneName == "leftforearm" &&
                (name == "forearml" || name == "lowerarml"))
            {
                return children[i];
            }

            if (normalizedBoneName == "lefthand" && name == "handl")
            {
                return children[i];
            }

            if (normalizedBoneName == "rightarm" &&
                (name == "armr" || name == "upperarmr"))
            {
                return children[i];
            }

            if (normalizedBoneName == "rightforearm" &&
                (name == "forearmr" || name == "lowerarmr"))
            {
                return children[i];
            }

            if (normalizedBoneName == "righthand" && name == "handr")
            {
                return children[i];
            }
        }

        return null;
    }

    private Transform FindRightHand()
    {
        Transform rightHand = FindBone("RightHand");
        if (rightHand != null)
        {
            return rightHand;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            string normalizedName = children[i].name
                .Replace("_", string.Empty)
                .Replace(".", string.Empty)
                .Replace(":", string.Empty)
                .ToLowerInvariant();

            if (normalizedName == "righthand" ||
                normalizedName == "handr" ||
                normalizedName.EndsWith("righthand"))
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
