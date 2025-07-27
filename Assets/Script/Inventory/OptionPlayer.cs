using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

public class OptionPlayer : MonoBehaviour
{
    public Transform contentParent;
    public GameObject linePrefab;

    private readonly List<GameObject> lines = new List<GameObject>();

    private static readonly string[] StatLabels =
    {
        "Máu",
        "Tấn công",
        "Chí mạng",
        "Phòng thủ",
        "Phản đòn",
        "Hút máu",
        "Né đòn"
    };

    public void UpdateLines(byte[] data)
    {
        ClearLines();

        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms))
        {
            int index = 0;
            while (ms.Position + 4 <= ms.Length && index < 7) // Chỉ đọc tối đa 7 giá trị
            {
                int value = ReadInt32BigEndian(reader);
                string label = (index < StatLabels.Length) ? StatLabels[index] : $"Chỉ số {index + 1}";
                string display = $"{label}: {value}";
                Debug.Log($"✅ {display}");
                AddLine(display);
                index++;
            }
        }
    }


    private void AddLine(string text)
    {
        GameObject line = Instantiate(linePrefab, contentParent);
        var tmp = line.GetComponent<TMP_Text>();
        if (tmp == null)
        {
            Debug.LogError("❌ linePrefab không có TMP_Text!");
            return;
        }
        tmp.text = text;
        lines.Add(line);
    }

    private void ClearLines()
    {
        foreach (var obj in lines)
        {
            if (obj) DestroyImmediate(obj); // Xóa ngay lập tức
        }
        lines.Clear();
    }


    private int ReadInt32BigEndian(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        if (bytes.Length < 4)
        {
            Debug.LogWarning("⚠️ Thiếu dữ liệu khi đọc int32.");
            return 0;
        }
        if (System.BitConverter.IsLittleEndian)
            System.Array.Reverse(bytes);
        return System.BitConverter.ToInt32(bytes, 0);
    }
}
