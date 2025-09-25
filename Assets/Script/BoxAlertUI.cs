using UnityEngine;
using TMPro;

public class BoxAlertUI : MonoBehaviour
{
    public GameObject alertPanel;
    public TextMeshProUGUI alertText;
    public GameObject closeButton,yes,no;
    public GameObject inventory;
    public GameObject bgr;
    public MouseClickDetector mouseClickDetector;
    private bool shouldRestoreInventory = false;
    private int mess =-1,type=-1;
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;

            bool clickedOnCloseBtn = closeButton.activeSelf &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    closeButton.GetComponent<RectTransform>(), mousePos, Camera.main);

            bool clickedOnNoBtn = no.activeSelf &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    no.GetComponent<RectTransform>(), mousePos, Camera.main);

            bool clickedOnYesBtn = yes.activeSelf &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    yes.GetComponent<RectTransform>(), mousePos, Camera.main);

            if (clickedOnCloseBtn || clickedOnNoBtn || clickedOnYesBtn)
            {
                Hide();
                if (shouldRestoreInventory && inventory != null)
                {
                    inventory.SetActive(true);
                    bgr.SetActive(true);
                    shouldRestoreInventory = false;
                }
                if (clickedOnYesBtn)
                {
                    if (type == 0)
                        mouseClickDetector.SendCommand("drop", mess);
                    else
                        mouseClickDetector.SendCommand("sell", mess);
                }
            }
        }
    }

    public void ShowAlert(string message)
    {
        closeButton.SetActive(true);
        yes.SetActive(false);
        no.SetActive(false);
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
    public void YesNo(string message,int msg, int type_action)
    {
        yes.SetActive(true);
        closeButton.SetActive(false);
        no.SetActive(true);
        mess = msg;
        type = type_action;
        if (inventory != null && inventory.activeSelf)
        {
            inventory.SetActive(false);
            bgr.SetActive(false);
            shouldRestoreInventory = true; // ✅ Nhớ bật lại sau này
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
