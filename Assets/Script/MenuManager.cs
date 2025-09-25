using System;
using System.Net;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentParent;
    public GameObject buttonPrefab;      // Prefab nút (TextMeshProUGUI + Button)
    public GameObject menu;              // Root Menu (có Image nền)
    public GameObject closeButtonPrefab; // Prefab nút đóng

    private const sbyte CMD_SEND_MENU = -113;

    public void HandleMenu(byte[] data)
    {
        if (data == null || data.Length < 5)
        {
            Debug.LogWarning("⚠️ Dữ liệu không hợp lệ để mở Menu.");
            return;
        }

        // Bật menu + đưa lên trên cùng
        menu.SetActive(true);
        menu.transform.SetAsLastSibling();

        // Làm nền trong suốt nhưng chặn click
        Image bgImg = menu.GetComponent<Image>();
        if (bgImg != null)
        {
            bgImg.color = new Color(0, 0, 0, 0);
            bgImg.raycastTarget = true;

            // Khi click nền thì tắt menu
            Button bgBtn = menu.GetComponent<Button>();
            if (bgBtn == null)
                bgBtn = menu.gameObject.AddComponent<Button>();

            bgBtn.transition = Selectable.Transition.None;
            bgBtn.onClick.RemoveAllListeners();
            bgBtn.onClick.AddListener(() =>
            {
                menu.SetActive(false);
                Debug.Log("📌 Menu đóng khi click nền.");
            });
        }

        // Xóa các item cũ
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        int index = 0;
        // Đọc npcId (4 byte big-endian)
        int npcId = (data[index++] << 24) | (data[index++] << 16) | (data[index++] << 8) | data[index++];
        int count = data[index++];

        // Sinh button từ dữ liệu
        for (int i = 0; i < count; i++)
        {
            if (index >= data.Length) break;

            int length = data[index++];
            if (index + length > data.Length) break;

            string itemText = Encoding.UTF8.GetString(data, index, length);
            index += length;

            GameObject newButtonObj = Instantiate(buttonPrefab, contentParent);
            newButtonObj.SetActive(true);

            TextMeshProUGUI label = newButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = itemText;
                label.enableWordWrapping = true;
                label.overflowMode = TextOverflowModes.Overflow;
            }

            Button btn = newButtonObj.GetComponent<Button>();
            if (btn != null)
            {
                int capturedIndex = i;
                btn.onClick.AddListener(() => OnMenuItemClicked(npcId, capturedIndex));
            }

            if (i < count - 1)
                AddLine(contentParent);
        }
    }

    private void AddLine(Transform parent)
    {
        GameObject line = new GameObject("Line", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        line.transform.SetParent(parent, false);

        Image img = line.GetComponent<Image>();
        img.color = new Color32(204, 0, 0, 255);

        RectTransform rt = line.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.offsetMin = new Vector2(0f, -1f);
        rt.offsetMax = new Vector2(0f, 1f);

        LayoutElement layout = line.GetComponent<LayoutElement>();
        layout.minHeight = 2;
        layout.preferredHeight = 2;
        layout.flexibleWidth = 1;
    }

    private void OnMenuItemClicked(int npcId, int index)
    {
        menu.SetActive(false); // Ẩn menu khi chọn item

        try
        {
            var writer = SocketManager.Instance.Writer;
            if (writer == null)
            {
                Debug.LogError("Writer chưa khởi tạo.");
                return;
            }

            writer.Write(CMD_SEND_MENU);

            // payload length = 8 (2 int)
            ushort length = 8;
            writer.Write((byte)(length >> 8));
            writer.Write((byte)(length & 0xFF));

            // Gửi npcId + index (big-endian)
            writer.Write(IPAddress.HostToNetworkOrder(npcId));
            writer.Write(IPAddress.HostToNetworkOrder(index));

            writer.Flush();
        }
        catch (Exception ex)
        {
            Debug.LogError("Lỗi khi gửi CMD -113: " + ex.Message);
        }
    }
}
