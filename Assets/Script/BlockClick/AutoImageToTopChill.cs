using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class AutoImageToTopChill : MonoBehaviour
{
    private void Awake()
    {
        // Nếu chưa có Image thì thêm vào
        Image img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
        }

        // Làm nền trong suốt nhưng vẫn chặn click
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = true;
    }

    private void OnEnable()
    {
        // 🔥 Thay vì SetAsLastSibling() chính nó
        // mình đẩy cả "cha" (Inventory) lên trên cùng
        Transform rootUI = transform.parent != null ? transform.parent : transform;
        rootUI.SetAsLastSibling();
    }
}
