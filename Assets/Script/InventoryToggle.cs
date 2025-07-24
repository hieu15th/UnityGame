using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InventoryToggle : MonoBehaviour
{
    private const sbyte CMD_GETBAG = -120;
    private const sbyte CMD_ITEM_EQUIP = -119;

    public GameObject inventoryUI; // Panel chính
    public Button openButton;      // Nút mở
    public Button closeButton;     // Nút đóng

    public GameObject bgr;         // Background Panel
    public GameObject name_bag;    // Tên túi
    public GameObject name_pl;     // Tên nhân vật
    public GameObject bag;         // UI túi
    public GameObject pl;          // UI nhân vật
    public GameObject select;          // UI nhân vật
    public GameObject ScrollView;
    public Button leftButton;      // Nút trái
    public Button rightButton;     // Nút phải

    private bool isOpen = false;
    private bool showingBag = true;

    void Start()
    {
        openButton.onClick.AddListener(() =>
        {
            if (!isOpen)
            {
                ShowUIOnly();
                StartCoroutine(DelayAndSendGetBag());
            }
        });

        closeButton.onClick.AddListener(CloseInventory);
        leftButton.onClick.AddListener(SwitchTab);
        rightButton.onClick.AddListener(SwitchTab);

        UpdateTab();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (!isOpen)
            {
                ShowUIOnly();
                StartCoroutine(DelayAndSendGetBag());
            }
            else
            {
                CloseInventory();
            }
        }
    }

    private void ShowUIOnly()
    {
        isOpen = true;

        inventoryUI.SetActive(true);
        bgr.SetActive(true);
        bag.SetActive(true);
        ScrollView.SetActive(true);
        pl.SetActive(false);
        openButton.gameObject.SetActive(false);
        select.SetActive(false);
        UpdateTab();
    }

    private IEnumerator DelayAndSendGetBag()
    {
        yield return null; // Đợi 1 frame để đảm bảo UI đã được render
        SendGetBagCommand();
    }

    public void CloseInventory()
    {
        if (!isOpen) return;

        isOpen = false;

        inventoryUI.SetActive(false);
        bgr.SetActive(false);
        bag.SetActive(false);
        ScrollView.SetActive(false);
        pl.SetActive(false);
        openButton.gameObject.SetActive(true);
        select.SetActive(true);
    }

    private void SwitchTab()
    {
        showingBag = !showingBag;
        UpdateTab();
    }

    private void UpdateTab()
    {
        bag.SetActive(showingBag);
        name_bag.SetActive(showingBag);

        pl.SetActive(!showingBag);
        name_pl.SetActive(!showingBag);

        // Gửi lệnh tùy tab
        try
        {
            var writer = SocketManager.Instance.Writer;
            if (writer != null)
            {
                if (showingBag)
                {
                    writer.Write(CMD_GETBAG);
                    writer.Write((ushort)0);
                    writer.Flush();
                    Debug.Log("📤 Đã gửi CMD_GETBAG (túi)");
                }
                else
                {
                    writer.Write(CMD_ITEM_EQUIP);
                    writer.Write((ushort)0);
                    writer.Flush();
                    Debug.Log("📤 Đã gửi CMD_ITEM_EQUIP (nhân vật)");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Writer chưa được khởi tạo.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("❌ Lỗi khi gửi CMD trong UpdateTab: " + ex.Message);
        }
    }


    private void SendGetBagCommand()
    {
        try
        {
            var writer = SocketManager.Instance.Writer;
            if (writer != null)
            {
                writer.Write(CMD_GETBAG);
                writer.Write((ushort)0); // Không có payload
                writer.Flush();
                Debug.Log("📤 Đã gửi CMD_GETBAG đến server.");
            }
            else
            {
                Debug.LogWarning("⚠️ Writer chưa được khởi tạo.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("❌ Lỗi khi gửi CMD_GETBAG: " + ex.Message);
        }
    }
}
