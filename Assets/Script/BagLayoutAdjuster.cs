using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OptionDataNamespace;
using ItemDataNamespace;

public class BagLayoutAdjuster : MonoBehaviour
{
    public RectTransform bagPanel;
    public RectTransform content;
    public GridLayoutGroup gridLayout;
    public GameObject slotPrefab;
    public GameObject btn_left;
    public GameObject btn_right;
    public OptionScrollView optionScrollView;
    public MouseClickDetector click;
    private Dictionary<int, ItemData> currentItemMap = new Dictionary<int, ItemData>();
    private const int slotCount = 120;
    public int columnCount = 5;
    public float spacing = 2f;

    [HideInInspector] public bool choose = false;
    private int selectedSlotIndex = -1;
    private List<GameObject> slots = new List<GameObject>();

    void Start()
    {
        AdjustSlotSize();
        SpawnSlots();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            bool clickOnContent = RectTransformUtility.RectangleContainsScreenPoint(content, Input.mousePosition, Camera.main);
            bool clickOnLeft = RectTransformUtility.RectangleContainsScreenPoint(btn_left.GetComponent<RectTransform>(), Input.mousePosition, Camera.main);
            bool clickOnRight = RectTransformUtility.RectangleContainsScreenPoint(btn_right.GetComponent<RectTransform>(), Input.mousePosition, Camera.main);
            if (clickOnLeft && selectedSlotIndex >= 0)
            {
                click.SendCommand("use", selectedSlotIndex);
                return;
            }
            if (clickOnRight && selectedSlotIndex >= 0)
            {
                click.SendCommand("drop", selectedSlotIndex);
                return;
            }

            if (!clickOnContent && !clickOnLeft && !clickOnRight)
            {
                choose = false;
                optionScrollView.Hide();
                SetObjectActiveWithText("", btn_left);
                SetObjectActiveWithText("", btn_right);
                selectedSlotIndex = -1;
                UpdateSelectedSlotVisual();
            }
        }

        if (choose)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow)) MoveSelection(1);
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveSelection(-1);
            else if (Input.GetKeyDown(KeyCode.DownArrow)) MoveSelection(columnCount);
            else if (Input.GetKeyDown(KeyCode.UpArrow)) MoveSelection(-columnCount);
        }
    }



    void AdjustSlotSize()
    {
        float totalWidth = bagPanel.rect.width;
        float totalSpacing = (columnCount - 1) * spacing;
        float slotWidth = (totalWidth - totalSpacing - 10f) / columnCount;

        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columnCount;
        gridLayout.cellSize = new Vector2(slotWidth, slotWidth);
        gridLayout.spacing = new Vector2(spacing, spacing);
        gridLayout.padding = new RectOffset(5, 5, 5, 5);
    }


    public void HandleBagData(byte[] data)
    {
        try
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int bagSize = ReadInt32BigEndian(reader);
                Debug.Log($"\ud83c\udf92 T\u00fai c\u00f3 {bagSize} \u00f4 m\u1edf kh\u00f3a (t\u1ed5ng {slotCount})");

                SpawnSlots();

                int itemCount = ReadInt32BigEndian(reader);
                Debug.Log($"\ud83d\udce6 T\u1ed5ng s\u1ed1 item: {itemCount}");

                Dictionary<int, ItemData> itemMap = new Dictionary<int, ItemData>();

                for (int i = 0; i < itemCount; i++)
                {
                    int index = ReadInt32BigEndian(reader);
                    int itemId = ReadInt32BigEndian(reader);
                    int color = ReadInt32BigEndian(reader);
                    int type = ReadInt32BigEndian(reader);
                    int img = ReadInt32BigEndian(reader);
                    int upgrade = ReadInt32BigEndian(reader);
                    int quantity = ReadInt32BigEndian(reader);

                    int nameLen = ReadInt32BigEndian(reader);
                    string itemName = Encoding.UTF8.GetString(reader.ReadBytes(nameLen));

                    int optionCount = ReadInt32BigEndian(reader);
                    List<OptionData> options = new List<OptionData>
                    {
                        new OptionData
                        {
                            id = -1,
                            param = upgrade,
                            color = 0,
                            type = -1,
                            name = itemName
                        }
                    };

                    for (int j = 0; j < optionCount; j++)
                    {
                        int optionId = ReadInt32BigEndian(reader);
                        int param = ReadInt32BigEndian(reader);
                        int optColor = ReadInt32BigEndian(reader);
                        int optType = ReadInt32BigEndian(reader);
                        int optNameLen = ReadInt32BigEndian(reader);
                        string optName = Encoding.UTF8.GetString(reader.ReadBytes(optNameLen));

                        options.Add(new OptionData
                        {
                            id = optionId,
                            param = param,
                            color = optColor,
                            type = optType,
                            name = optName
                        });
                    }

                    itemMap[index] = new ItemData
                    {
                        itemId = itemId,
                        color = color,
                        type = type,
                        img = img,
                        upgrade = upgrade,
                        quantity = quantity,
                        name = itemName,
                        options = options
                    };
                }

                currentItemMap = itemMap;

                for (int i = 0; i < slotCount; i++)
                {
                    GameObject slot = slots[i];
                    Image slotImage = slot.GetComponent<Image>();
                    var iconTransform = slot.transform.Find("Item");
                    var quantityTransform = slot.transform.Find("Quantity");

                    var icon = iconTransform?.GetComponent<Image>();
                    var quantityText = quantityTransform?.GetComponent<TextMeshProUGUI>();

                    if (slotImage == null)
                    {
                        Debug.LogWarning($"\u26a0\ufe0f Slot {i}: Kh\u00f4ng c\u00f3 Image tr\u00ean Slot");
                        continue;
                    }

                    if (itemMap.TryGetValue(i, out ItemData item))
                    {
                        switch (item.color)
                        {
                            case 0: slotImage.sprite = GetSpriteFromSheet("Items", "UI 1_6"); break;
                            case 1: slotImage.sprite = GetSpriteFromSheet("Items", "UI 1_9"); break;
                            case 2: slotImage.sprite = GetSpriteFromSheet("Items", "UI 1_13"); break;
                            case 3: slotImage.sprite = GetSpriteFromSheet("Items", "UI 1_5"); break;
                            default: slotImage.sprite = GetSpriteFromSheet("Items", "bgr_item_0"); break;
                        }

                        if (icon != null) icon.sprite = GetSpriteFromId(item.img);
                        if (quantityText != null)
                            quantityText.text = item.quantity > 1 ? item.quantity.ToString() : "";
                    }
                    else if (i < bagSize)
                    {
                        if (icon != null) icon.sprite = GetSpriteFromSheet("Items", "UI 1_6");
                        if (quantityText != null) quantityText.text = "";
                    }
                    else
                    {
                        if (icon != null) icon.sprite = GetSpriteFromSheet("Items", "UI 1_17");
                        if (quantityText != null) quantityText.text = "";
                    }
                }

                UpdateSelectedSlotVisual();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("\u274c L\u1ed7i khi \u0111\u1ecdc d\u1eef li\u1ec7u t\u00fai: " + ex.Message);
        }
    }

    void SpawnSlots()
    {
        slots.Clear();

        foreach (Transform child in gridLayout.transform)
            Destroy(child.gameObject);

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slot = Instantiate(slotPrefab, gridLayout.transform);
            slots.Add(slot);

            SlotClickHandler handler = slot.AddComponent<SlotClickHandler>();
            handler.Init(this, i);
        }
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Count)
            return;

        selectedSlotIndex = index;
        choose = true;

        UpdateSelectedSlotVisual();
        ScrollToSlot(index);

        if (currentItemMap.TryGetValue(index, out var item))
        {
            Debug.Log($"\ud83d\udd0d Slot {index} c\u00f3 item ID={item.itemId}, {item.options.Count} option");
            optionScrollView.ShowOptions(item.options);
            SetObjectActiveWithText("Sử dụng", btn_left);
            SetObjectActiveWithText("Vứt bỏ", btn_right);
        }
        else
        {
            SetObjectActiveWithText("", btn_left);
            SetObjectActiveWithText("", btn_right);
            optionScrollView.Hide();
        }
    }
    public void SetObjectActiveWithText(string name, GameObject obj)
    {
        if (obj == null) return;

        if (!string.IsNullOrEmpty(name))
        {
            var textTransform = obj.transform.Find("Text");
            if (textTransform != null)
            {
                var tmpText = textTransform.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpText != null)
                {
                    obj.SetActive(true);
                    tmpText.text = name;
                }
                else
                {
                    Debug.LogWarning("Không tìm thấy TextMeshProUGUI trong Text GameObject.");
                }
            }
            else
            {
                Debug.LogWarning("Không tìm thấy GameObject con tên là 'Text'.");
            }
        }
        else
        {
            obj.SetActive(false);
        }
    }
    void UpdateSelectedSlotVisual()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            Transform background = slots[i].transform.Find("Background");

            if (background != null)
            {
                Image img = background.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = (i == selectedSlotIndex && choose)
                        ? GetSpriteFromSheet("Items", "UI 1_1")
                        : GetSpriteFromSheet("Items", "UI 1_0");
                }
            }
        }
    }

    void MoveSelection(int offset)
    {
        int newIndex = selectedSlotIndex + offset;
        if (newIndex >= 0 && newIndex < slots.Count)
            SelectSlot(newIndex);
    }

    void ScrollToSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return;

        RectTransform slotRect = slots[index].GetComponent<RectTransform>();
        float viewportHeight = bagPanel.rect.height;
        float contentHeight = content.rect.height;
        float slotHeight = slotRect.rect.height;

        float slotLocalY = -slotRect.localPosition.y;
        float slotTop = slotLocalY;
        float slotBottom = slotLocalY + slotHeight;

        float currentScroll = content.anchoredPosition.y;
        float newScroll = currentScroll;

        if (slotBottom > currentScroll + viewportHeight)
            newScroll = slotBottom - viewportHeight;
        else if (slotTop < currentScroll)
            newScroll = slotTop;

        float maxScroll = Mathf.Max(0, contentHeight - viewportHeight);
        newScroll = Mathf.Clamp(newScroll, 0, maxScroll);

        content.anchoredPosition = new Vector2(content.anchoredPosition.x, newScroll);
    }

    private Sprite GetSpriteFromId(int id)
    {
        var sprite = Resources.Load<Sprite>($"Items/{id}");
        if (sprite == null)
            Debug.LogWarning($"\u26a0\ufe0f Kh\u00f4ng t\u00ecm th\u1ea5y \u1ea3nh Items/{id}");
        return sprite;
    }

    private Sprite GetSpriteFromSheet(string sheetPath, string spriteName)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(sheetPath);
        foreach (var sprite in sprites)
        {
            if (sprite.name == spriteName)
                return sprite;
        }

        Debug.LogWarning($"\u26a0\ufe0f Kh\u00f4ng t\u00ecm th\u1ea5y sprite '{spriteName}' trong sheet '{sheetPath}'");
        return null;
    }

    private int ReadInt32BigEndian(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }
}