using System;
using System.Net;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] public InventoryToggle UI_Upgrade;
    public void HandleUI(byte[] data)
    {
        if (data.Length < 1)
        {
            Debug.LogWarning("❌ Không đủ dữ liệu để đọc UI");
            return;
        }

        int offset = 0;

        // Đọc byte đầu tiên là index UI
        byte index = data[offset++];
        //Debug.Log($"📩 Nhận UI index: {index}");

        switch (index)
        {
            case 1: // Mở UI nâng cấp
                //Debug.Log("📦 Mở giao diện nâng cấp");

                if (UI_Upgrade != null)
                    UI_Upgrade.OpenInventoryFromButton(1);
                else
                    Debug.LogWarning("⚠️ UI_Upgrade chưa được gán trong Inspector");

                break;

            default:
                Debug.LogWarning("❓ Không nhận diện được UI index: " + index);
                break;
        }
    }

}
