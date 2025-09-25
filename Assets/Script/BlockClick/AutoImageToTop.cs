using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class AutoImageToTop : MonoBehaviour
{
    [Header("Image Settings")]
    public bool blockRaycast = true; // Có chặn click xuyên qua không

    void Awake()
    {
        // Thêm Image nếu chưa có
        Image img = gameObject.GetComponent<Image>();
        if (img == null)
            img = gameObject.AddComponent<Image>();

        // Set màu trong suốt nhưng vẫn có thể chặn raycast
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = blockRaycast;

        // Đưa object lên trên cùng
        transform.SetAsLastSibling();
    }

    // Cho phép gọi lại bằng code nếu cần
    public void BringToFront()
    {
        transform.SetAsLastSibling();
    }
}
