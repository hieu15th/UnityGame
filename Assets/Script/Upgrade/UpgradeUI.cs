using OptionDataNamespace;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUI : MonoBehaviour
{
    public Transform[] equipSlots;
    public MouseClickDetector click;
    public OptionScrollView optionScrollView;

    private Dictionary<int, ItemData> equippedItemMap = new Dictionary<int, ItemData>();
    public UpgradeManager upgradeManager;
    private int selectedIndex = -1;
    [HideInInspector] public bool choose = false;
    public GameObject btn_left,btn_close,btn_cancel;   // Nút "Sử dụng"
    public int index_choose = -1;
    public class ItemData
    {
        public int index, id, color, type, img, upgrade, quantity;
        public string name;
        public List<OptionData> options;
    }

    void Start()
    {
        for (int i = 0; i < equipSlots.Length; i++)
        {
            int index = i; // server gửi từ 0-7
            var button = equipSlots[i].GetComponent<Button>();
            if (button == null)
            {
                button = equipSlots[i].gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
            }

            button.onClick.AddListener(() => SelectSlot(index));
        }
    }

    void Update()
    {
        if (choose)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow)) MoveSelection(1);
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveSelection(-1);
        }

        if (Input.GetMouseButtonDown(0))
        {
            bool clickedOnAnySlot = false;

            foreach (var slot in equipSlots)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(
                    slot.GetComponent<RectTransform>(), Input.mousePosition, Camera.main))
                {
                    clickedOnAnySlot = true;
                    break;
                }
            }

            bool clickOnLeft = RectTransformUtility.RectangleContainsScreenPoint(btn_left.GetComponent<RectTransform>(), Input.mousePosition, Camera.main);

            if (clickOnLeft && selectedIndex >= 0)
            {
                click.SendCommand("upgrade", index_choose);
                return;
            }

            if (!clickedOnAnySlot && !clickOnLeft)
            {
                SetObjectActiveWithText("", btn_left);
                SetObjectActiveWithText("", btn_cancel);
                choose = false;
                selectedIndex = -1;
                UpdateSelectedSlotVisual();
            }
            bool cancel = RectTransformUtility.RectangleContainsScreenPoint(btn_cancel.GetComponent<RectTransform>(), Input.mousePosition, Camera.main);

            bool close = RectTransformUtility.RectangleContainsScreenPoint(btn_close.GetComponent<RectTransform>(), Input.mousePosition, Camera.main);
            if (close || cancel)
            {
                // Xóa dữ liệu item trong map
                equippedItemMap.Clear();

                // Reset toàn bộ slot về trạng thái trống
                for (int i = 0; i < equipSlots.Length; i++)
                {
                    ApplyToEquipSlot(i, -1, -1); // imgId = -1, color = -1 để reset
                }

                // Reset lựa chọn
                choose = false;
                selectedIndex = -1;
                UpdateSelectedSlotVisual();
                index_choose = -1;
                // Ẩn panel option nếu đang mở
                optionScrollView.Hide();
                SetObjectActiveWithText("", btn_left);
                SetObjectActiveWithText("", btn_cancel);
            }

        }
    }
    private IEnumerator WaitForUpgradeCheck()
    {
        yield return new WaitUntil(() => upgradeManager.check);
        Debug.Log("✅ upgradeManager.check = true, xử lý tiếp...");
    }


    public void HandleAddUpgrade(byte[] data)
    {
        try
        {

            equippedItemMap.Clear();

            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int itemCount = ReadInt32BigEndian(reader);
                if (itemCount == 1)
                {
                    upgradeManager.ResetAllObjects();

                    // Đợi check = true theo kiểu khác (ví dụ dùng callback hoặc sự kiện)
                    StartCoroutine(WaitForUpgradeCheck());
                }

                for (int i = 0; i < itemCount; i++)
                {
                    int index = ReadInt32BigEndian(reader); // 0-7
                    int id = ReadInt32BigEndian(reader);
                    int color = ReadInt32BigEndian(reader);
                    int type = ReadInt32BigEndian(reader);
                    int img = ReadInt32BigEndian(reader);
                    int upgrade = ReadInt32BigEndian(reader);
                    int quantity = ReadInt32BigEndian(reader);

                    int nameLen = ReadInt32BigEndian(reader);
                    byte[] nameBytes = reader.ReadBytes(nameLen);
                    if (nameBytes.Length != nameLen)
                    {
                        Debug.LogError($"❌ Không đủ byte để đọc tên item. Mong đợi {nameLen}, nhận {nameBytes.Length}");
                        return;
                    }
                    string itemName = Encoding.UTF8.GetString(nameBytes);

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
                        },
                        new OptionData
                        {
                            id = -2,
                            param = quantity,
                            color = 5,
                            type = -1,
                            name = "Số lượng:"
                        }
                    };

                    for (int j = 0; j < optionCount; j++)
                    {
                        int optId = ReadInt32BigEndian(reader);
                        int param = ReadInt32BigEndian(reader);
                        int optColor = ReadInt32BigEndian(reader);
                        int optType = ReadInt32BigEndian(reader);
                        int optNameLen = ReadInt32BigEndian(reader);
                        byte[] optNameBytes = reader.ReadBytes(optNameLen);
                        string optName = Encoding.UTF8.GetString(optNameBytes);

                        options.Add(new OptionData
                        {
                            id = optId,
                            param = param,
                            color = optColor,
                            type = optType,
                            name = optName
                        });
                    }

                    equippedItemMap[index] = new ItemData
                    {
                        index = index,
                        id = id,
                        color = color,
                        type = type,
                        img = img,
                        upgrade = upgrade,
                        quantity = quantity,
                        name = itemName,
                        options = options
                    };

                    ApplyToEquipSlot(index, img, color);
                }
                // Reset toàn bộ ô trang bị trước khi apply dữ liệu mới
                for (int i = 0; i < equipSlots.Length; i++)
                {
                    if (!equippedItemMap.ContainsKey(i))
                    {
                        ApplyToEquipSlot(i, -1, -1); // gửi id -1 để xóa
                    }
                }

                UpdateSelectedSlotVisual();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ Lỗi khi đọc item_equip: " + ex.Message);
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
    public void SelectSlot(int index)
    {
        if (index < 0 || index >= equipSlots.Length)
            return;

        selectedIndex = index;
        choose = true;

        UpdateSelectedSlotVisual();

        if (equippedItemMap.TryGetValue(index, out var item))
        {
            if(index == 0)
            {
                SetObjectActiveWithText("Bỏ ra", btn_cancel);
            } else
            {
                SetObjectActiveWithText("", btn_cancel);
            }
            SetObjectActiveWithText("Nâng cấp", btn_left);
            optionScrollView.ShowOptions(item.options);
        }
        else
        {
            SetObjectActiveWithText("", btn_left);
            SetObjectActiveWithText("", btn_cancel);
            optionScrollView.Hide();
        }
    }

    void UpdateSelectedSlotVisual()
    {
        for (int i = 0; i < equipSlots.Length; i++)
        {
            var img = equipSlots[i].Find("Background")?.GetComponent<Image>();

            if (img != null)
            {
                img.sprite = (i == selectedIndex && choose)
                    ? GetSpriteFromSheet("Items", "UI 1_1")
                    : GetSpriteFromSheet("Items", "UI 1_0");
            }
        }
    }

    void MoveSelection(int offset)
    {
        int newIndex = selectedIndex + offset;
        if (newIndex >= 0 && newIndex < equipSlots.Length)
            SelectSlot(newIndex);
    }

    void ApplyToEquipSlot(int index, int imgId, int color)
    {
        if (index < 0 || index >= equipSlots.Length)
        {
            Debug.LogWarning($"⚠️ Index đồ mặc {index} không hợp lệ.");
            return;
        }

        var slot = equipSlots[index];
        var icon = slot.Find("Item")?.GetComponent<Image>();
        var background = slot.GetComponent<Image>();
        var borderEffectTransform = slot.transform.Find("BorderEffect");
        var dotEffect = borderEffectTransform?.GetComponent<DotBorderEffect>();
        if (borderEffectTransform == null)
        {
            Debug.LogWarning($"⚠️ Slot {index}: Không tìm thấy object BorderEffect");
        }

        if (dotEffect == null)
        {
            Debug.LogWarning($"⚠️ Slot {index}: BorderEffect không có component DotBorderEffect");
        }

        if (dotEffect != null)
        {
            int dotCount = 0;
            Color dotColor = Color.black;

            if (equippedItemMap.TryGetValue(index, out ItemData item) && item != null)
            {
                int upgrade = item.upgrade;

                if (upgrade > 0)
                {
                    // dotCount tuần hoàn từ 1–4
                    dotCount = (upgrade - 1) % 4 + 1;

                    // group tăng mỗi 4 cấp (0:1–4, 1:5–8, ...)
                    int group = (upgrade - 1) / 4;

                    dotColor = group switch
                    {
                        0 => Color.green,
                        1 => Color.yellow,
                        2 => Color.cyan,
                        3 => Color.red,
                        4 => Color.magenta,
                        _ => Color.white
                    };
                }
            }

            dotEffect.Init(dotColor, dotCount);
        }
        else
        {
            Debug.LogWarning($"Bị null");

        }
        if (icon != null)
        {
            if (imgId == -1)
                icon.sprite = null;
            else
                icon.sprite = GetSpriteFromId(imgId);
        }

        if (background != null)
        {
            if (color == -1)
                background.sprite = GetSpriteFromSheet("Items", "UI 1_6"); // nền mặc định khi trống
            else
            {
                switch (color)
                {
                    case 0: background.sprite = GetSpriteFromSheet("Items", "UI 1_6"); break;
                    case 1: background.sprite = GetSpriteFromSheet("Items", "UI 1_9"); break;
                    case 2: background.sprite = GetSpriteFromSheet("Items", "UI 1_13"); break;
                    case 3: background.sprite = GetSpriteFromSheet("Items", "UI 1_5"); break;
                    default: background.sprite = GetSpriteFromSheet("Items", "UI 1_6"); break;
                }
            }
        }
    }


    private Sprite GetSpriteFromId(int id)
    {
        var sprite = Resources.Load<Sprite>($"Items/{id}");
        if (sprite == null)
            Debug.LogWarning($"⚠️ Không tìm thấy ảnh Items/{id}");
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
        Debug.LogWarning($"⚠️ Không tìm thấy sprite '{spriteName}' trong sheet '{sheetPath}'");
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
