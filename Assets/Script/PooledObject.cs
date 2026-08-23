using UnityEngine;

public class PooledObject : MonoBehaviour
{
    private ObjectPool pool;

    public bool HasPool => pool != null;

    public void SetPool(ObjectPool ownerPool)
    {
        pool = ownerPool;
    }

    public void Release()
    {
        if (pool != null)
        {
            pool.Despawn(gameObject);
            return;
        }

        gameObject.SetActive(false);
    }
}
