using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    // Tốc độ xoay trên mỗi trục
    public Vector3 rotationSpeed = new Vector3(0f, 0f, 100f); // Xoay quanh trục Y

    void Update()
    {
        // Xoay theo thời gian thực để không phụ thuộc FPS
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
