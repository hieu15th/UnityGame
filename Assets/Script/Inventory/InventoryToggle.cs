using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryToggle : MonoBehaviour
{
    public GameObject inventoryUI; // Panel chính
    public Button openButton;      // Nút mở
    public Button closeButton;     // Nút đóng

    public GameObject bgr;         // Background Panel
    private GameObject UI_1, UI_2;         // UI túi
    public GameObject Bag,Char,Upgrade,Shop;          // UI nhân vật
    public GameObject select;          // UI nhân vật
    public GameObject ScrollView;
    public Button leftButton;      // Nút trái
    public Button rightButton;     // Nút phải
    private bool isOpen = false;
    private bool showingBag = true;
    [SerializeField] private MessageManager mess;
    [SerializeField] private BagLayoutAdjuster bag;

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
        //if (Upgrade == null)
        //{
        //    Debug.LogError("Upgrade vẫn chưa được gán trong " + gameObject.name);
        //    Upgrade = GameObject.Find("UI_Upgrade");
        //    if (Upgrade != null)
        //    {
        //        Debug.Log("Gán thành công Upgrade bằng Find");
        //    }
        //}
        closeButton.onClick.AddListener(CloseInventory);
        leftButton.onClick.AddListener(SwitchTab);
        rightButton.onClick.AddListener(SwitchTab);

        //UpdateTab();
    }

    void Default()
    {
        Load(0);
    }
    void Load(int index)
    {
        switch (index)
        {
            case 0:
                UI_1 = Bag;
                UI_2 = Char;
                break;
            case 1:
                UI_1 = Upgrade;
                UI_2 = Bag;
                break;
            case 2:
                UI_1 = Shop;
                UI_2 = Bag;
                break;
        }
        bag.type = index;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (!isOpen)
            {
                Default();
                ShowUIOnly();
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
        UI_1.SetActive(true);
        if(UI_1 == Bag)
        {
            ScrollView.SetActive(true);
        }
        UI_2.SetActive(false);
        openButton.gameObject.SetActive(false);
        select.SetActive(false);        
        UpdateTab();
    }

    public void CloseInventory()
    {
        if (!isOpen) return;

        isOpen = false;

        inventoryUI.SetActive(false);
        bgr.SetActive(false);
        UI_1.SetActive(false);
        ScrollView.SetActive(false);
        UI_2.SetActive(false);
        openButton.gameObject.SetActive(true);
        select.SetActive(true);
    }

    private void SwitchTab()
    {
        showingBag = !showingBag;
        UpdateTab();
    }
    public void OpenInventoryFromButton(int index)
    {
        if (!isOpen)
        {
            Load(index);
            ShowUIOnly();
        }
    }

    private void UpdateTab()
    {
        UI_1.SetActive(showingBag);

        UI_2.SetActive(!showingBag);
        if(UI_2.activeSelf && UI_2 == Bag)
        {
            ScrollView.SetActive(true);
        }
    }
}
