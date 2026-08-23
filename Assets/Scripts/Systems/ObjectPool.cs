using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int preloadCount = 16;
    [SerializeField] private int maxSize = 128;

    private readonly Queue<GameObject> inactive =
        new Queue<GameObject>();

    private void Awake()
    {
        if (prefab == null)
        {
            Debug.LogWarning("[ObjectPool] Prefab is not assigned.");
            return;
        }

        for (int i = 0; i < preloadCount; i++)
        {
            GameObject instance = CreateInstance();

            if (instance == null)
            {
                return;
            }

            instance.SetActive(false);
            inactive.Enqueue(instance);
        }
    }

    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[ObjectPool] Prefab is not assigned.");
            return null;
        }

        GameObject instance =
            inactive.Count > 0 ? inactive.Dequeue() : CreateInstance();

        if (instance == null)
        {
            return null;
        }

        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);

        return instance;
    }

    public void Despawn(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (inactive.Count >= maxSize)
        {
            Destroy(instance);
            return;
        }

        instance.SetActive(false);
        instance.transform.SetParent(transform);
        inactive.Enqueue(instance);
    }

    private GameObject CreateInstance()
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject instance =
            Instantiate(prefab, transform);

        PooledObject pooledObject =
            instance.GetComponent<PooledObject>();

        if (pooledObject == null)
        {
            pooledObject = instance.AddComponent<PooledObject>();
        }

        pooledObject.SetPool(this);

        return instance;
    }
}
