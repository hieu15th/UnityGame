using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static SkillUI;

public class Skill : MonoBehaviour
{
    [Header("Danh sách ô kỹ năng trên UI")]
    public Transform[] skills; // Các slot hiển thị skill trong UI

    private readonly Dictionary<int, SkillInfo> ownedSkillEquip = new();
    public int selectedSkillIndex = -1; // Skill hiện đang được chọn
    public int skill_id = 0;
    private GameObject pl;
    private void Update()
    {
        if(pl == null)
        {
            pl = GameObject.FindGameObjectWithTag("Player");
        }
        if (selectedSkillIndex != -1)
        {
            OnSelectSkill(selectedSkillIndex);
        }
        UpdateSkillSlotsVisibility();
    }

    private void UpdateSkillSlotsVisibility()
    {
        if (skills == null || skills.Length == 0)
            return;

        int activeCount = ownedSkillEquip.Count;

        for (int i = 0; i < skills.Length; i++)
        {
            bool shouldShow = i < activeCount;
            if (skills[i] != null)
            {
                skills[i].gameObject.SetActive(shouldShow);
            }
        }
    }

    // ================== XỬ LÝ DỮ LIỆU SKILL ==================
    public void handleSkill(byte[] data)
    {
        try
        {
            ownedSkillEquip.Clear();

            if (data == null || data.Length < 4)
            {
                return;
            }

            using MemoryStream ms = new(data);
            using BinaryReader reader = new(ms);

            int skillCount = ReadInt32BigEndian(reader);

            for (int i = 0; i < skillCount; i++)
            {
                SkillInfo skill = new SkillInfo();
                skill.id = ReadInt32BigEndian(reader);
                skill.part = ReadInt32BigEndian(reader);
                skill.cd = ReadInt32BigEndian(reader);
                ownedSkillEquip[i] = skill;
            }

            RefreshEquippedSkillLayout();
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Lỗi đọc skill equip: {ex}");
        }
    }

    // ================== CẬP NHẬT GIAO DIỆN ==================
    private void RefreshEquippedSkillLayout()
    {
        if (skills == null || skills.Length == 0)
            return;

        // --- Reset slot cũ (chỉ reset hiển thị, không destroy SkillCD) ---
        for (int i = 0; i < skills.Length; i++)
        {
            var slot = skills[i];
            if (slot == null)
                continue;

            var icon = slot.Find("Item")?.GetComponent<Image>();
            var bg = slot.Find("Image")?.GetComponent<Image>();

            if (bg != null)
                bg.sprite = GetSpriteFromSheet("Items", "UI 1_0");
            if (icon != null)
                icon.sprite = null;
        }

        // --- Gắn skill mới ---
        foreach (var kv in ownedSkillEquip)
        {
            int index = kv.Key;
            SkillInfo skill = kv.Value;

            if (index < 0 || index >= skills.Length)
                continue;

            var slot = skills[index];
            if (slot == null)
                continue;

            var icon = slot.Find("Item")?.GetComponent<Image>();
            var bg = slot.Find("Image")?.GetComponent<Image>();

            // Load icon skill
            var sprite = GetSpriteFromSheet("Skills", skill.part.ToString());
            if (icon != null)
                icon.sprite = sprite;

            if (bg != null)
                bg.sprite = GetSpriteFromSheet("Items", "UI 1_0");

            // --- Cập nhật hoặc tạo mới SkillCD ---
            var skillData = slot.GetComponent<SkillCD>();
            if (skillData == null)
                skillData = slot.gameObject.AddComponent<SkillCD>();

            // Cập nhật dữ liệu
            skillData.info = skill;

            // --- Gắn sự kiện click ---
            var button = slot.GetComponent<Button>() ?? slot.gameObject.AddComponent<Button>();
            int capturedIndex = index;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnSelectSkill(capturedIndex));
        }
    }



    // ================== XỬ LÝ CHỌN SKILL ==================
    public void OnSelectSkill(int index)
    {
        if (index < 0 || index >= skills.Length)
        {
            Debug.LogWarning($"⚠️ Skill index {index} không hợp lệ.");
            return;
        }

        // Reset toàn bộ BG về mặc định
        for (int i = 0; i < skills.Length; i++)
        {
            var slot = skills[i];
            if (slot == null) continue;

            var bgObj = slot.Find("Image");
            var bg = bgObj?.GetComponent<Image>();
            if (bg != null)
                bg.sprite = GetSpriteFromSheet("Items", "UI 1_0");
        }

        // Đổi màu slot được chọn
        var selectedSlot = skills[index];
        var selectedBg = selectedSlot.Find("Image")?.GetComponent<Image>();
        if (selectedBg != null)
            selectedBg.sprite = GetSpriteFromSheet("Items", "UI 1_1");
        var skillData = skills[index]?.GetComponent<SkillCD>();
        if (skillData != null)
        {
            skill_id = skillData.info.id;
        }
        else
        {
            skill_id = 0;
        }
        pl.GetComponent<SkillUse>().id_skill = skill_id;
        selectedSkillIndex = index;
    }

    // ================== HÀM TIỆN ÍCH ==================
    private int ReadInt32BigEndian(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        if (bytes.Length < 4)
            throw new EndOfStreamException("Không đủ byte để đọc Int32");

        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);

        return BitConverter.ToInt32(bytes, 0);
    }

    private Sprite GetSpriteFromSheet(string sheetPath, string spriteName)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(sheetPath);
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning($"⚠️ Không tìm thấy sprite sheet: {sheetPath}");
            return null;
        }

        foreach (var sprite in sprites)
        {
            if (sprite.name == spriteName)
                return sprite;
        }

        Debug.LogWarning($"⚠️ Không tìm thấy sprite '{spriteName}' trong sheet '{sheetPath}'");
        return null;
    }
}
