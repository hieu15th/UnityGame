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
                _ => throw new ArgumentException($"Hành động không hợp lệ: {action}")
            };
            mess.SendMessage(-118, actionCode, index);
            
            //Debug.Log($"📤 Gửi CMD_USE_ITEM với subCmd={actionCode}, index={index}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Lỗi khi gửi CMD_USE_ITEM: {ex.Message}");
        }
    }



}
