using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class PartManager : MonoBehaviour
{
    public static PartManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void ApplyParts(GameObject player, int hair, int body, int head, int facehair, int helmet, int armor,
                             int hand, int leg, int boot, int weapon, int cloak)
    {
        var baseParts = new (string partName, int index)[]
        {
        ("body", body),
        ("hair", hair),
        ("head", head),
        ("facehair", facehair),
        ("helmet", helmet),
        ("shoulder_l", hand),
        ("armor_l", armor),
        ("weapon", weapon),
        ("shoulder_r", hand),
        ("armor_r", armor),
        ("foot_l", leg),
        ("boot_l", boot),
        ("foot_r", leg),
        ("boot_r", boot),
        ("armor", armor),
        ("cloak", cloak),
        };

        //Debug.Log($"🧩 Bắt đầu ApplyParts cho Player: {player.name}");

        foreach (var part in baseParts)
        {
            //Debug.Log($"➡️ Apply part: {part.partName} với index: {part.index}");
            ApplySinglePart(player, part.partName, part.index);
        }

        Debug.Log("✅ Hoàn tất ApplyParts.");
    }


    public static readonly Dictionary<string, string> PartPathMap = new Dictionary<string, string>
    {
        // ✅ Đầu
        { "hair",        "UnitRoot/Root/BodySet/P_Body/HeadSet/P_Head/P_Hair/7_Hair" },
        { "head",        "UnitRoot/Root/BodySet/P_Body/HeadSet/P_Head/P_Head/5_Head" },
        { "facehair",    "UnitRoot/Root/BodySet/P_Body/HeadSet/P_Head/P_Mustache/6_FaceHair" },
        { "helmet",      "UnitRoot/Root/BodySet/P_Body/HeadSet/P_Head/P_Helmet/11_Helmet1" },

        // ✅ Tay trái
        { "shoulder_l",    "UnitRoot/Root/BodySet/P_Body/ArmSet/ArmR/P_RArm/P_Arm/-20_R_Arm" },
        { "armor_l",       "UnitRoot/Root/BodySet/P_Body/ArmSet/ArmR/P_RArm/P_Arm/-20_R_Arm/P_Shoulder/-15_R_Shoulder" },
        { "weapon",      "UnitRoot/Root/BodySet/P_Body/ArmSet/ArmR/P_RArm/P_Weapon/R_Weapon"},

        // ✅ Tay phải
        { "shoulder_r",    "UnitRoot/Root/BodySet/P_Body/ArmSet/ArmL/P_LArm/P_Arm/20_L_Arm" },
        { "armor_r",       "UnitRoot/Root/BodySet/P_Body/ArmSet/ArmL/P_LArm/P_Arm/20_L_Arm/P_Shoulder/25_L_Shoulder" },


        // ✅ Thân trên
        { "body",        "UnitRoot/Root/BodySet/P_Body/Body" },
        { "cloak",        "UnitRoot/Root/BodySet/P_Body/P_Back/Back" },
        { "armor",       "UnitRoot/Root/BodySet/P_Body/Body/P_ClothBody/ClothBody" },

        // ✅ Chân trái
        { "foot_l",       "UnitRoot/Root/P_RFoot/_12R_Foot" },
        { "boot_l",      "UnitRoot/Root/P_RFoot/P_RCloth/_11R_Cloth" },

        // ✅ Chân phải
        { "foot_r",       "UnitRoot/Root/P_LFoot/_3L_Foot" },
        { "boot_r",      "UnitRoot/Root/P_LFoot/P_LCloth/_2L_Cloth" },

        // ✅ Thanh máu (nếu cần dùng lại sau)
        //{ "health_bar",  "Canvas/Heath_Bar" },
    };




    void ApplySinglePart(GameObject player, string partName, int partIndex)
    {
        if (partIndex < 0)
        {
            Debug.LogWarning($"⚠️ Bỏ qua '{partName}' vì partIndex < 0");
            return;
        }

        if (!PartPathMap.TryGetValue(partName.ToLower(), out string path))
        {
            Debug.LogWarning($"❌ Không có đường dẫn cho partName '{partName}' trong PartPathMap.");
            return;
        }

        Transform partObj = player.transform.Find(path);
        if (partObj == null)
        {
            Debug.LogWarning($"❌ Không tìm thấy GameObject theo path '{path}' trong player.");
            return;
        }

        var resolver = partObj.GetComponent<SpriteResolver>();
        if (resolver == null)
        {
            Debug.LogWarning($"❌ GameObject tại '{path}' không có SpriteResolver.");
            return;
        }

        if (resolver.spriteLibrary == null)
        {
            Debug.LogWarning($"⚠️ '{partName}' không có Sprite Library Asset.");
        }

        string category = partName.ToLower();
        string label = $"{partIndex}";

        //Debug.Log($"🎯 Đặt Category: '{category}', Label: '{label}' cho part '{partName}' tại '{path}'");

        resolver.SetCategoryAndLabel(category, label);
    }

}

