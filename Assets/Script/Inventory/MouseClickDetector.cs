using System;
using UnityEngine;
using UnityEngine.EventSystems;
using static Unity.Burst.Intrinsics.X86.Avx;

[RequireComponent(typeof(RectTransform))]
public class MouseClickDetector : MonoBehaviour
{
    // Các mã lệnh – sửa cho đúng với server
    private const sbyte CMD_USE = -128;
    private const sbyte CMD_DROP = -127;
    private const sbyte CMD_UNDRESS = -126;
    private const sbyte CMD_SELL = -125;
    private const sbyte CMD_INFOR_UPGRADE = -124;
    private const sbyte CMD_UPGRADE = -123;

    // Gán giá trị này từ bên ngoài (vd: qua Unity Inspector hoặc script khác)
    public int itemIndex = -1;
    public string action; // Mặc định là "use", có thể là "drop" hoặc "inspect"
    [SerializeField] private MessageManager mess;

    public void Start()
    {
        if (mess == null)
        {
            mess = FindAnyObjectByType<MessageManager>();
            if (mess == null)
            {
                Debug.LogError("❌ Không tìm thấy MessageManager trong scene.");
                return;
            }
        }
    }
    public void SendCommand(string action, int index)
    {
        try
        {

            sbyte actionCode = action.ToLower() switch
            {
                "use" => CMD_USE,
                "drop" => CMD_DROP,
                "undress" => CMD_UNDRESS,
                "sell" => CMD_SELL,
                "add_upgarde" => CMD_INFOR_UPGRADE,
                "upgrade" => CMD_UPGRADE,
                _ => throw new ArgumentException($"Hành động không hợp lệ: {action}")
            };
            mess.SendMessage(-118, actionCode, index);

        }
        catch (Exception ex)
        {
            if (mess == null) Debug.LogError("❌ mess đang NULL");

            Debug.LogError($"❌ Lỗi khi gửi CMD_USE_ITEM: {ex.Message}");
        }

    }
    public void SendCommandBuy(int index, int type)
    {
        try
        {
            var writer = SocketManager.Instance.Writer;
            if (writer == null)
            {
                Debug.LogWarning("⚠️ Writer chưa được khởi tạo.");
                return;
            }

            writer.Write((sbyte)-109); // CMD_SEND_SHOP

            // Ghi độ dài payload = 10 byte (2 + 4 + 4)
            ushort length = 10;
            writer.Write((byte)(length >> 8));   // byte cao
            writer.Write((byte)(length & 0xFF)); // byte thấp

            // payload
            writer.Write((byte)0x00); // subCmd
            writer.Write((byte)0x05); // param

            byte[] indexBytes = BitConverter.GetBytes(index);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(indexBytes);
            writer.Write(indexBytes);

            byte[] typeBytes = BitConverter.GetBytes(type);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(typeBytes);
            writer.Write(typeBytes);

            writer.Flush();
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Lỗi khi gửi CMD_SEND_SHOP: {ex.Message}");
        }
    }
}
