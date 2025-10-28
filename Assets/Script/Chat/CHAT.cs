using System;
using System.Text;
using TMPro;
using UnityEngine;

public class CHAT : MonoBehaviour
{
    public TMP_InputField input;
    public RectTransform send;
    public RectTransform exit;
    public GameObject opBag;
    public byte type; // 0 = chat thường, 1 = chat đặc biệt

    private void OnEnable()
    {
        // ⚙️ Cập nhật giới hạn ký tự dựa vào type khi bật khung chat
        UpdateCharacterLimit();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;

            // ✅ Kiểm tra click vào vùng send
            if (RectTransformUtility.RectangleContainsScreenPoint(send, mousePos, Camera.main))
            {
                string message = input.text.Trim();
                sendChat(message);
                input.text = "";
            }
            // ✅ Kiểm tra click vào vùng exit
            else if (RectTransformUtility.RectangleContainsScreenPoint(exit, mousePos, Camera.main))
            {
                Debug.Log("❌ Thoát chat");
                input.text = ""; // xóa nội dung
                gameObject.SetActive(false); // ẩn khung chat
                opBag.SetActive(true);
            }
        }
    }

    private void sendChat(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            SendCHAT(message);
        }

        gameObject.SetActive(false);
        opBag.SetActive(true);
    }

    public void SendCHAT(string message)
    {
        try
        {
            var writer = SocketManager.Instance.Writer;
            if (writer == null)
            {
                Debug.LogWarning("⚠️ Writer chưa được khởi tạo.");
                return;
            }

            byte[] msgBytes = Encoding.UTF8.GetBytes(message);

            byte[] payload = new byte[1 + msgBytes.Length];
            payload[0] = type;
            Buffer.BlockCopy(msgBytes, 0, payload, 1, msgBytes.Length);

            writer.Write((byte)0x98);

            ushort size = (ushort)payload.Length;
            byte[] sizeBytes = BitConverter.GetBytes(size);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(sizeBytes);
            writer.Write(sizeBytes);

            writer.Write(payload);
            writer.Flush();

            Debug.Log($"📨 Gửi chat (type={type}): \"{message}\" ({payload.Length} bytes)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Lỗi khi gửi chat: {ex.Message}");
        }
    }

    private void UpdateCharacterLimit()
    {
        // 🧭 Giới hạn ký tự theo type
        if (type == 0)
            input.characterLimit = 20;
        else if (type == 1)
            input.characterLimit = 30;
    }
}
