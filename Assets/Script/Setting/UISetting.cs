using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 👈 thêm dòng này để dùng EventSystem

public class UISetting : MonoBehaviour
{
    public GameObject[] UI;
    public GameObject volume;
    public GameObject panel;
    public GameObject chat;
    void Start()
    {
        UpdateVolumeUI();
    }

    void Update()
    {
        foreach (GameObject go in UI)
        {
            if (go.activeSelf)
                go.SetActive(false);
        }

        // Khi click chuột trái
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUI(panel))
            {
                gameObject.SetActive(false);
                foreach (GameObject go in UI)
                {
                    if (!go.activeSelf)
                        go.SetActive(true);
                }
            }
        }
    }

    private bool IsPointerOverUI(GameObject targetPanel)
    {
        // Kiểm tra xem có UI nào dưới con trỏ không
        if (EventSystem.current == null)
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // Duyệt xem panel hoặc con của nó có trong danh sách trúng raycast không
        foreach (var r in results)
        {
            if (r.gameObject == targetPanel || r.gameObject.transform.IsChildOf(targetPanel.transform))
                return true;
        }
        return false;
    }

    public void ToggleAudio()
    {
        AudioManager.Instance.ToggleMute();
        UpdateVolumeUI();
    }
    public void opChatW()
    {
        chat.GetComponent<CHAT>().type = 1;
        chat.SetActive(true);
        gameObject.SetActive(false);
    }
    private void UpdateVolumeUI()
    {
        bool isMuted = AudioManager.Instance.isMuted;
        var text = volume.GetComponentInChildren<TextMeshProUGUI>();
        text.text = isMuted ? "Âm lượng: Tắt" : "Âm lượng: Bật";
    }


}
