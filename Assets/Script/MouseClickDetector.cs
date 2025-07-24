using UnityEngine;
using UnityEngine.EventSystems;
using System;

[RequireComponent(typeof(RectTransform))]
public class MouseClickDetector : MonoBehaviour
{
    // Các mã lệnh – sửa cho đúng với server
    private const sbyte CMD_USE_ITEM = -118;
    private const sbyte CMD_USE = -128;
    private const sbyte CMD_DROP = -127;
    private const sbyte CMD_UNDRESS = -126;

    // Gán giá trị này từ bên ngoài (vd: qua Unity Inspector hoặc script khác)
    public int itemIndex = -1;
    public string action; // Mặc định là "use", có thể là "drop" hoặc "inspect"

    /// <summary>
    /// Gửi lệnh đến server theo action ("use", "drop", "inspect") và index item
    /// </summary>
    public void SendCommand(string action, int index)
    {
        try
        {
            var writer = SocketManager.Instance.Writer;
            if (writer == null)
            {
                Debug.LogWarning("⚠️ Writer chưa được khởi tạo.");
                return;
            }

            sbyte actionCode = action.ToLower() switch
            {
                "use" => CMD_USE,
                "drop" => CMD_DROP,
                "undress" => CMD_UNDRESS,
                _ => throw new ArgumentException($"Hành động không hợp lệ: {action}")
            };

            writer.Write(CMD_USE_ITEM); // CMD_USE_ITEM

            // Gửi payload length = 5 (subCmd + index)
            writer.Write((byte)0x00); // High byte
            writer.Write((byte)0x05); // Low byte

            writer.Write(actionCode); // subCmd

            // Gửi index kiểu int (4 byte, BigEndian)
            byte[] indexBytes = BitConverter.GetBytes(index);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(indexBytes); // Chuyển về BigEndian
            writer.Write(indexBytes); // 4 byte

            writer.Flush();

            Debug.Log($"📤 Gửi CMD_USE_ITEM với subCmd={actionCode}, index={index}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Lỗi khi gửi CMD_USE_ITEM: {ex.Message}");
        }
    }



}
