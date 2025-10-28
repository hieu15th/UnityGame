using UnityEngine;

public class UI_CHAT : MonoBehaviour
{
    public GameObject ui_chat;

    // Gọi hàm này khi nhấn nút
    public void ToggleChat()
    {
        if (ui_chat != null)
        {
            bool isActive = ui_chat.activeSelf; // kiểm tra trạng thái hiện tại
            ui_chat.SetActive(!isActive);        // đảo trạng thái
            var infor = ui_chat.GetComponent<CHAT>();
            infor.type = 0;
        }
    }
}