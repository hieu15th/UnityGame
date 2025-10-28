using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryToggle : MonoBehaviour
{
    public GameObject inventoryUI;
    public Button openButton;
    public Button closeButton;

    public GameObject bgr;
    public GameObject Bag, Char, Upgrade, Shop, Skill;
    public GameObject select,chat;
    public GameObject ScrollView;
    public Button leftButton;
    public Button rightButton;

    private List<GameObject> activeTabs = new List<GameObject>();
    private int currentTabIndex = 0;
    private bool isOpen = false;

    [SerializeField] private MessageManager mess;
    [SerializeField] private BagLayoutAdjuster bag;
    [SerializeField] private UpgradeUI upgrade;

    void Start()
    {
        if (mess == null)
        {
            mess = FindFirstObjectByType<MessageManager>();
            if (mess == null)
            {
                Debug.LogError("❌ Không tìm thấy MessageManager trong scene.");
                return;
            }
        }

        closeButton.onClick.AddListener(CloseInventory);
        leftButton.onClick.AddListener(() => SwitchTab(-1));
        rightButton.onClick.AddListener(() => SwitchTab(1));
    }

    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.I))
        //{
        //    if (!isOpen)
        //    {
        //        Load(0, 1, 4); // ví dụ mặc định mở Bag + Char
        //        ShowUIOnly();
        //    }
        //    else
        //    {
        //        CloseInventory();
        //    }
        //}
    }

    // 👉 Hàm Load mới: cho phép truyền nhiều tab
    public void Load(params int[] indices)
    {
        activeTabs.Clear();
        foreach (int i in indices)
        {
            switch (i)
            {
                case 0: activeTabs.Add(Bag); break;
                case 1: activeTabs.Add(Char); break;
                case 2: activeTabs.Add(Upgrade); break;
                case 3: activeTabs.Add(Shop); break;
                case 4: activeTabs.Add(Skill); break;
            }
        }
        currentTabIndex = 0;
    }
    private void ShowUIOnly()
    {
        isOpen = true;
        inventoryUI.SetActive(true);
        bgr.SetActive(true);
        openButton.gameObject.SetActive(false);
        select.SetActive(false);
        chat.SetActive(false);
        UpdateTab();
    }

    public void CloseInventory()
    {
        if (!isOpen) return;
        isOpen = false;
        upgrade.clear();
        inventoryUI.SetActive(false);
        bgr.SetActive(false);
        Bag.SetActive(false);
        Char.SetActive(false);
        Upgrade.SetActive(false);
        Shop.SetActive(false);
        Skill.SetActive(false);
        ScrollView.SetActive(false);
        openButton.gameObject.SetActive(true);
        select.SetActive(true);
        chat.SetActive(true);
    }

    private void SwitchTab(int direction)
    {
        if (activeTabs.Count == 0) return;

        currentTabIndex += direction;
        if (currentTabIndex >= activeTabs.Count) currentTabIndex = 0;
        if (currentTabIndex < 0) currentTabIndex = activeTabs.Count - 1;

        UpdateTab();
    }

    public void OpenInventoryFromButton(params int[] indices)
    {
        switch (indices[0])
        {
            case 2: bag.type = 1; break;
            case 3: bag.type = 2; break;
            default: bag.type = 0; break;
        }
        if (!isOpen)
        {
            Load(indices);
            ShowUIOnly();
        }
    }

    private void UpdateTab()
    {
        // Tắt tất cả tab
        Bag.SetActive(false);
        Char.SetActive(false);
        Upgrade.SetActive(false);
        Shop.SetActive(false);
        Skill.SetActive(false);
        ScrollView.SetActive(false);

        if (activeTabs.Count == 0) return;

        GameObject tab = activeTabs[currentTabIndex];
        tab.SetActive(true);

        if (tab == Bag)
            ScrollView.SetActive(true);

        // Gán type cho BagLayoutAdjuster nếu có
        //if (tab == Bag) bag.type = 0;
        //else if (tab == Char) bag.type = 1;
        //else if (tab == Upgrade) bag.type = 2;
        //else if (tab == Shop) bag.type = 3;
        //else if (tab == Skill) bag.type = 4;
    }
}
