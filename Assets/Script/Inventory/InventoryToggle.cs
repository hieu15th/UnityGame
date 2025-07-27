using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InventoryToggle : MonoBehaviour
{
    public GameObject inventoryUI; // Panel chính
    public Button openButton;      // Nút mở
    public Button closeButton;     // Nút đóng

    public GameObject bgr;         // Background Panel
    public GameObject bag;         // UI túi
    public GameObject pl;          // UI nhân vật
    public GameObject select;          // UI nhân vật
    public GameObject ScrollView;
    public Button leftButton;      // Nút trái
    public Button rightButton;     // Nút phải
    private bool isOpen = false;
    private bool showingBag = true;
    [SerializeField] private MessageManager mess;

    void Start()
    {
        if (mess == null)
        {
            mess = Object.FindFirstObjectByType<MessageManager>();

            if (mess == null)
            {
                Debug.LogError("❌ Không tìm thấy MessageManager trong scene.");
                return;
            }
        }

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
        mess.SendRequest(-120);
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
    public void OpenInventoryFromButton()
    {
        if (!isOpen)
        {
            ShowUIOnly();
            StartCoroutine(DelayAndSendGetBag());
        }
    }

    private void UpdateTab()
    {
        bag.SetActive(showingBag);

        pl.SetActive(!showingBag);

        // Gửi lệnh tùy tab
        try
        {
            if (showingBag)
            {
                mess.SendRequest(-120);
            }
            else
            {
                mess.SendRequest(-119);
            }

        }
        catch (System.Exception ex)
        {
            Debug.LogError("❌ Lỗi khi gửi CMD trong UpdateTab: " + ex.Message);
        }
    }
}
