using UnityEngine;
using TMPro;

public class BoxAlertUI : MonoBehaviour
{
    public GameObject alertPanel;
    public TextMeshProUGUI alertText;
    public GameObject closeButton;
    public GameObject inventory;
    public GameObject bgr;

    private bool shouldRestoreInventory = false; // ✅ Ghi nhớ trạng thái inventory trước khi ẩn

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;

            bool clickedOnCloseBtn = RectTransformUtility.RectangleContainsScreenPoint(
                closeButton.GetComponent<RectTransform>(), mousePos, Camera.main);

            if (clickedOnCloseBtn)
            {
                Hide();

                // ✅ Bật lại inventory nếu cần
                if (shouldRestoreInventory && inventory != null)
                {
                    inventory.SetActive(true);
                    bgr.SetActive(true);
                    Debug.Log("📦 Túi đồ đã được bật lại sau khi đóng alert.");
                    shouldRestoreInventory = false; // Reset flag
                }

                return;
            }
        }
    }

    public void ShowAlert(string message)
    {
        if (inventory != null && inventory.activeSelf)
        {
            inventory.SetActive(false);
            bgr.SetActive(false);
            shouldRestoreInventory = true; // ✅ Nhớ bật lại sau này
            Debug.Log("📦 Túi đồ đã được ẩn trước khi hiển thị cảnh báo.");
        }
        else
        {
            shouldRestoreInventory = false;
        }

        if (alertPanel != null && alertText != null)
        {
            alertPanel.SetActive(true);
            alertText.text = message;
        }
    }

    public void Hide()
    {
        if (alertPanel != null)
            alertPanel.SetActive(false);
    }
}
