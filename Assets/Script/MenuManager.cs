using System;
using System.Net;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public Transform contentParent;
    public GameObject buttonPrefab; // Prefab chỉ chứa Text (TextMeshProUGUI)
    public GameObject menu;
    private const sbyte CMD_SEND_MENU = -113;

    public void HandleMenu(byte[] data)
    {
        menu.SetActive(true);
        Debug.Log("📥 Bắt đầu xử lý HandleMenu...");
        Debug.Log("📦 Tổng byte nhận: " + data.Length);

        StringBuilder hexDump = new StringBuilder();
        foreach (byte b in data)
            hexDump.AppendFormat("{0:X2} ", b);
        Debug.Log("🔍 Hex dump: " + hexDump);

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        int index = 0;
        if (data.Length < 5)
        {
            Debug.LogWarning("⚠️ Dữ liệu không hợp lệ.");
            return;
        }

        // 🟡 Đọc npcId từ 4 byte đầu tiên
        int npcId = (data[index++] << 24) | (data[index++] << 16) | (data[index++] << 8) | data[index++];
        Debug.Log("🆔 NPC ID = " + npcId);

        int count = data[index++];
        Debug.Log("📋 Tổng số menu: " + count);

        for (int i = 0; i < count; i++)
        {
            if (index >= data.Length) break;

            int length = data[index++];
            if (index + length > data.Length) break;

            string itemText = Encoding.UTF8.GetString(data, index, length);
            index += length;

            Debug.Log($"✅ Menu[{i}] = \"{itemText}\"");

            GameObject newButtonObj = Instantiate(buttonPrefab, contentParent);
            newButtonObj.SetActive(true);

            TextMeshProUGUI label = newButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null)
            {
                Debug.LogError("❌ Không tìm thấy TextMeshProUGUI trong prefab.");
                continue;
            }

            label.text = itemText;

            Button btn = newButtonObj.GetComponent<Button>();
            if (btn != null)
            {
                int capturedIndex = i;
                btn.onClick.AddListener(() => OnMenuItemClicked(npcId,capturedIndex));
            }

            if (i < count - 1)
            {
                GameObject line = new GameObject("Line", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                line.transform.SetParent(contentParent, false);

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
        }

        Debug.Log("✅ Kết thúc xử lý HandleMenu.");
    }


    private void OnMenuItemClicked(int npcId, int index)
    {
        Debug.Log($"🖱️ Click menu[{index}] by npcId:{npcId}");

        try
        {
            var writer = SocketManager.Instance.Writer;
            if (writer == null)
            {
                Debug.LogError("Writer chưa khởi tạo.");
                return;
            }

            writer.Write(CMD_SEND_MENU);

            // Gửi độ dài payload (8 byte = 2 int)
            ushort length = 8;
            writer.Write((byte)(length >> 8));     // byte cao
            writer.Write((byte)(length & 0xFF));   // byte thấp

            // Gửi payload: npcId và index (int, big-endian)
            writer.Write(IPAddress.HostToNetworkOrder(npcId));
            writer.Write(IPAddress.HostToNetworkOrder(index));

            writer.Flush();
        }
        catch (Exception ex)
        {
            Debug.LogError("Lỗi khi gửi CMD -119: " + ex.Message);
        }
    }



}
