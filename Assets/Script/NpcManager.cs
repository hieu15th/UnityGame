using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class NpcManager : MonoBehaviour
{
    public static NpcManager Instance { get; private set; }

    // Prefab NPC gốc dùng để clone
    public GameObject npcPrefab;

    // Lưu tất cả các NPC đang tồn tại: id -> GameObject
    private Dictionary<int, GameObject> npcMap = new Dictionary<int, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Nếu cần giữ qua các scene
    }

    /// <summary>
    /// Tạo 1 NPC mới từ dữ liệu truyền vào.
    /// </summary>
    public void SpawnNpc(Npc npc)
    {
        if (npcMap.ContainsKey(npc.id))
        {
            Debug.LogWarning($"❗ NPC với ID {npc.id} đã tồn tại.");
            return;
        }

        if (npcPrefab == null)
        {
            Debug.LogError("❌ Chưa gán prefab NPC!");
            return;
        }

        GameObject go = Instantiate(npcPrefab, new Vector3(npc.x, npc.y, 0), Quaternion.identity);
        var nameText = go.transform.Find("Name")?.GetComponent<TextMeshPro>();
        if (nameText != null)
        {
            nameText.text = $"<b><color=#00FF00><size=22>{npc.id}_{npc.name}</size></color></b>";
        }
        var info = go.AddComponent<NpcInfo>();
        info.id = npc.id;
        var healthTransform = go.transform.Find("Heath_Bar");
        if (healthTransform != null)
        {
            healthTransform.gameObject.SetActive(false);
        }
        go.tag = "Npc";
        //// Set tên hoặc label lên UI nếu cần
        //var text = go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        //if (text != null) text.text = npc.name;

        // Gọi PartManager.ApplyParts để gắn phần thân, tóc, áo... cho NPC
        PartManager.Instance.ApplyParts(go,
            npc.hair, npc.body, npc.head, npc.facehair, npc.helmet,
            npc.armor, npc.hand, npc.leg, npc.boot, npc.weapon, npc.cloak
        );

        npcMap[npc.id] = go;
    }

    /// <summary>
    /// Xoá NPC khỏi bản đồ.
    /// </summary>
    public void RemoveNpc(int id)
    {
        if (npcMap.TryGetValue(id, out GameObject npc))
        {
            Destroy(npc);
            npcMap.Remove(id);
        }
    }

    /// <summary>
    /// Xoá toàn bộ NPC.
    /// </summary>
    public void ClearAll()
    {
        foreach (var npc in npcMap.Values)
        {
            Destroy(npc);
        }
        npcMap.Clear();
    }
}
