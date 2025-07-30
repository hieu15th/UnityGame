using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    // Thời gian tồn tại của đối tượng trước khi bị hủy (1 giây)
    public float destroyDelay = 1f;

    void Start()
    {
        // Hủy đối tượng sau thời gian delay (1 giây)
        Destroy(gameObject, destroyDelay);
    }
}
