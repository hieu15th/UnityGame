using UnityEngine;

public class OpenSetting : MonoBehaviour
{
    public GameObject UI_Setting;

    // Gọi hàm này khi nhấn nút
    public void ToggleSetting()
    {
        if (UI_Setting != null)
        {
            bool isActive = UI_Setting.activeSelf; // kiểm tra trạng thái hiện tại
            UI_Setting.SetActive(!isActive);        // đảo trạng thái
        }
    }
}
