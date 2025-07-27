using System;
using UnityEngine;

public class MessageManager : MonoBehaviour
{
    private const sbyte CMD_REQUEST_PLAYER = -125;
    private const sbyte CMD_MOVE = -124;
    private const sbyte CMD_MOVE_ALL = -123;
    private const sbyte CMD_BAND = -122;
    private const sbyte CMD_DISCONNECT = -121;
    private const sbyte CMD_GETBAG = -120;
    private const sbyte CMD_ITEM_EQUIP = -119;
    private const sbyte CMD_SEND_ALERT = -117;
    private const sbyte CMD_PLAYER_STATS = -116;
    private const sbyte CMD_REQUEST_NPC = -115;
    private const sbyte CMD_SEND_NPC = -114;
    private const sbyte CMD_SEND_UI = -113;
    public void SendRequest(sbyte cmd)
    {
        try
        {
            var writer = SocketManager.Instance.Writer;
            if (writer == null)
            {
                Debug.LogError("Writer chưa khởi tạo.");
                return;
            }

            writer.Write(cmd);
            writer.Write((ushort)0);
            writer.Flush();
        }
        catch (Exception ex)
        {
            Debug.LogError("Lỗi khi gửi CMD " + cmd + ": " + ex.Message);
        }
    }
    public void SendMessage(sbyte cmd, sbyte subcmd, int index)
    {
        try
        {
            var writer = SocketManager.Instance.Writer;
            if (writer == null)
            {
                Debug.LogWarning("⚠️ Writer chưa được khởi tạo.");
                return;
            }
            writer.Write(cmd); // CMD_USE_ITEM

            // Gửi payload length = 5 (subCmd + index)
            writer.Write((byte)0x00); // High byte
            writer.Write((byte)0x05); // Low byte

            writer.Write(subcmd); // subCmd

            // Gửi index kiểu int (4 byte, BigEndian)
            byte[] indexBytes = BitConverter.GetBytes(index);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(indexBytes); // Chuyển về BigEndian
            writer.Write(indexBytes); // 4 byte

            writer.Flush();
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Lỗi khi gửi message:"+ cmd +" lỗi"+ ex.Message);
        }
    }
}
    
