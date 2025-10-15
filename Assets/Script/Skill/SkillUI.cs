using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SkillUI : MonoBehaviour
{
    [Header("4 ô skill trang bị")]
    public Transform[] equippedSlots; // 4 ô có sẵn
    public int selectedEquipIndex = -1;

    [Header("Danh sách skill (ScrollView)")]
    public RectTransform ownedSkillLayout;
    public GameObject slotPrefab;
    public int columnCount = 5;
    public float spacing = 4f;
    public Vector2 fixedCellSize = new Vector2(40f, 40f);

    [Header("Liên kết bên ngoài")]
    public SkillDetail op;
    public GameObject button;
    public MouseClickDetector click;

    // Data
    private readonly Dictionary<int, SkillInfo> ownedSkillEquip = new();
    private readonly Dictionary<int, SkillInfo> ownedSkillMap = new();
    private readonly List<GameObject> ownedSkillSlots = new();

    public SkillData selectedSkill; // skill đang chọn

    // ================== CLASS ==================
    [Serializable]
    public class SkillInfo
    {
        public int id;
        public string name;
        public string detail;
        public int color;
        public int type;
        public int part;
        public float cd;
    }

    public class SkillData : MonoBehaviour
    {
        public SkillInfo info;
    }

    // ================== UNITY ==================
    void Start()
    {
        for (int i = 0; i < equippedSlots.Length; i++)
        {
            int index = i;
            var btn = equippedSlots[i].GetComponent<Button>() ?? equippedSlots[i].gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => OnClickEquippedSlot(index));
        }

        AdjustOwnedSkillGrid();
        UpdateEquippedVisual();
    }

    void Update()
    {
        if (selectedSkill == null && selectedEquipIndex == -1)
        {
            SetObjectActiveWithText("", button);
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(button.GetComponent<RectTransform>(), Input.mousePosition, Camera.main))
            {
                if (selectedSkill != null)
                {
                    click.SendCommand("equip_skill", selectedSkill.info.id);
                }
                else if (selectedEquipIndex != -1)
                {
                    click.SendCommand("unequip_skill", ownedSkillEquip[selectedEquipIndex].id);
                    selectedEquipIndex = -1;
                }
            }
        }
    }

    void OnDisable()
    {
        selectedSkill = null;
        selectedEquipIndex = -1;
        SetObjectActiveWithText("", button);

        UpdateOwnedSkillVisual();
        UpdateEquippedVisual();
        op?.Hide();
    }

    // ================== GIAO DIỆN ==================
    public void SetObjectActiveWithText(string text, GameObject obj)
    {
        if (obj == null) return;
        var textTransform = obj.transform.Find("Text");
        if (textTransform == null) return;

        var tmpText = textTransform.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText == null) return;

        if (!string.IsNullOrEmpty(text))
        {
            obj.SetActive(true);
            tmpText.text = text;
        }
        else obj.SetActive(false);
    }

    // ================== TRANG BỊ ==================
    void UpdateEquippedVisual()
    {
        for (int i = 0; i < equippedSlots.Length; i++)
        {
            var bg = equippedSlots[i].Find("Background")?.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = GetSpriteFromSheet("Items", i == selectedEquipIndex ? "UI 1_1" : "UI 1_0");
            }
        }
    }

    public void OnClickEquippedSlot(int index)
    {
        selectedEquipIndex = index;
        selectedSkill = null;

        UpdateEquippedVisual();
        UpdateOwnedSkillVisual();

        if (ownedSkillEquip.TryGetValue(index, out var skill))
        {
            op.ShowSkillDetail(skill);
            SetObjectActiveWithText("Gỡ", button);
        }
        else
        {
            SetObjectActiveWithText("", button);
            op.Hide();
        }
    }

    // ================== ĐỌC DỮ LIỆU ==================
    public void HandleSkillData(byte[] data)
    {
        try
        {
            ownedSkillMap.Clear();

            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            int skillCount = ReadInt32BigEndian(reader);

            for (int i = 0; i < skillCount; i++)
            {
                SkillInfo info = new SkillInfo
                {
                    id = ReadInt32BigEndian(reader),
                    color = ReadInt32BigEndian(reader),
                    type = ReadInt32BigEndian(reader),
                    part = ReadInt32BigEndian(reader),
                    name = Encoding.UTF8.GetString(reader.ReadBytes(ReadInt32BigEndian(reader))),
                    detail = Encoding.UTF8.GetString(reader.ReadBytes(ReadInt32BigEndian(reader))),
                    cd = (float)ReadInt32BigEndian(reader) / 1000f
                };

                ownedSkillMap[i] = info;
            }

            RefreshOwnedSkillLayout();
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ Lỗi đọc skill data: " + ex.Message);
        }
    }

    public void HandleSkillEquip(byte[] data)
    {
        try
        {
            ownedSkillEquip.Clear();

            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            int skillCount = ReadInt32BigEndian(reader);

            for (int i = 0; i < skillCount; i++)
            {
                SkillInfo info = new SkillInfo
                {
                    id = ReadInt32BigEndian(reader),
                    color = ReadInt32BigEndian(reader),
                    type = ReadInt32BigEndian(reader),
                    part = ReadInt32BigEndian(reader),
                    name = Encoding.UTF8.GetString(reader.ReadBytes(ReadInt32BigEndian(reader))),
                    detail = Encoding.UTF8.GetString(reader.ReadBytes(ReadInt32BigEndian(reader))),
                    cd = (float)ReadInt32BigEndian(reader) / 1000f
                };

                ownedSkillEquip[i] = info;
            }

            RefreshEquippedSkillLayout();
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ Lỗi đọc skill equip: " + ex.Message);
        }
    }

    // ================== CẬP NHẬT UI ==================
    void RefreshEquippedSkillLayout()
    {
        for (int i = 0; i < equippedSlots.Length; i++)
        {
            var slot = equippedSlots[i];
            var icon = slot.Find("Item")?.GetComponent<Image>();
            var nameText = slot.Find("Name")?.GetComponent<TextMeshProUGUI>();
            var bg = slot.Find("Background")?.GetComponent<Image>();

            if (bg) bg.sprite = GetSpriteFromSheet("Items", "UI 1_0");
            if (icon) icon.sprite = null;
            if (nameText) nameText.text = "";

            var old = slot.GetComponent<SkillData>();
            if (old) DestroyImmediate(old);
        }

        foreach (var kv in ownedSkillEquip)
        {
            int index = kv.Key;
            SkillInfo info = kv.Value;

            if (index >= equippedSlots.Length) continue;
            var slot = equippedSlots[index];

            var icon = slot.Find("Item")?.GetComponent<Image>();
            var nameText = slot.Find("Name")?.GetComponent<TextMeshProUGUI>();

            if (icon)
                icon.sprite = GetSpriteFromSheet("Skills", info.part.ToString());
            if (nameText)
            {
                nameText.text = info.name;
                nameText.color = GetColorById(info.color);
            }

            var data = slot.AddComponent<SkillData>();
            data.info = info;
        }
    }

    void RefreshOwnedSkillLayout()
    {
        foreach (Transform child in ownedSkillLayout)
            Destroy(child.gameObject);
        ownedSkillSlots.Clear();

        foreach (var kv in ownedSkillMap)
        {
            SkillInfo info = kv.Value;
            GameObject slot = Instantiate(slotPrefab, ownedSkillLayout);
            ownedSkillSlots.Add(slot);

            var data = slot.AddComponent<SkillData>();
            data.info = info;

            var icon = slot.transform.Find("Item")?.GetComponent<Image>();
            var nameText = slot.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            var bg = slot.transform.Find("Background")?.GetComponent<Image>();

            if (icon)
                icon.sprite = GetSpriteFromSheet("Skills", info.part.ToString());
            if (nameText)
            {
                nameText.text = info.name;
                nameText.color = GetColorById(info.color);
            }
            if (bg)
                bg.sprite = GetSpriteFromSheet("Items", "UI 1_0");

            var btn = slot.GetComponent<Button>() ?? slot.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnClickSkill(data));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(ownedSkillLayout);
    }

    // ================== CLICK ==================
    void OnClickSkill(SkillData skill)
    {
        selectedSkill = skill;
        SetObjectActiveWithText("Gán", button);

        UpdateOwnedSkillVisual();
        selectedEquipIndex = -1;
        UpdateEquippedVisual();

        op.ShowSkillDetail(skill.info);
    }

    void UpdateOwnedSkillVisual()
    {
        foreach (var slot in ownedSkillSlots)
        {
            var data = slot.GetComponent<SkillData>();
            var bg = slot.transform.Find("Background")?.GetComponent<Image>();
            if (bg == null || data == null) continue;

            bg.sprite = (selectedSkill != null && data.info.id == selectedSkill.info.id)
                ? GetSpriteFromSheet("Items", "UI 1_1")
                : GetSpriteFromSheet("Items", "UI 1_0");
        }
    }

    // ================== GRID ==================
    private void AdjustOwnedSkillGrid()
    {
        var grid = ownedSkillLayout.GetComponent<GridLayoutGroup>() ??
                   ownedSkillLayout.gameObject.AddComponent<GridLayoutGroup>();

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columnCount;
        grid.spacing = new Vector2(spacing, spacing);
        grid.padding = new RectOffset(5, 5, 5, 5);
        grid.cellSize = fixedCellSize;

        var fitter = ownedSkillLayout.GetComponent<ContentSizeFitter>() ??
                     ownedSkillLayout.gameObject.AddComponent<ContentSizeFitter>();

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    // ================== TIỆN ÍCH ==================
    private Color GetColorById(int colorId) => colorId switch
    {
        0 => Color.white,
        1 => Color.green,
        2 => new Color(0.6f, 0f, 1f),
        3 => Color.yellow,
        4 => Color.red,
        _ => Color.gray,
    };

    private int ReadInt32BigEndian(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    private Sprite GetSpriteFromSheet(string sheetPath, string spriteName)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(sheetPath);
        foreach (var sprite in sprites)
            if (sprite.name == spriteName)
                return sprite;
        return null;
    }
}
