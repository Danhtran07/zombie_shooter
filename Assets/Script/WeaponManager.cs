using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Holder")]
    [SerializeField] private Transform weaponHolder;

    [Header("Starting Weapon")]
    [SerializeField] private GameObject startingWeapon;

    private GameObject currentWeapon;

    private void Start()
    {
        if (startingWeapon != null)
        {
            EquipWeapon(startingWeapon);
        }
    }

    public void EquipWeapon(GameObject weaponPrefab)
    {
        if (weaponPrefab == null)
            return;

        // Xóa súng cũ
        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
        }

        // Spawn súng mới vào tay
        currentWeapon = Instantiate(
            weaponPrefab,
            weaponHolder
        );

        // Reset local transform
        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;
        currentWeapon.transform.localScale = Vector3.one;
    }

    public GameObject GetCurrentWeapon()
    {
        return currentWeapon;
    }
}