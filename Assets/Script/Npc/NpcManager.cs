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
    public void SpawnNpcs(List<Npc> npcList)
    {
        HashSet<int> newNpcIds = new HashSet<int>();

        foreach (Npc npc in npcList)
        {
            newNpcIds.Add(npc.id);

            // Nếu đã tồn tại NPC, cập nhật vị trí và bỏ qua việc tạo mới
            if (npcMap.ContainsKey(npc.id))
            {
                GameObject existing = npcMap[npc.id];
                existing.transform.position = new Vector3(npc.x, npc.y, 0);
                continue;
            }

            if (npcPrefab == null)
            {
                Debug.LogError("❌ Chưa gán prefab NPC!");
                return;
            }

            // Random hướng hiển thị
            int direction = Random.Range(0, 2) == 0 ? -1 : 1;

            GameObject go = Instantiate(npcPrefab, new Vector3(npc.x, npc.y, 0), Quaternion.identity);

            // 👉 Đặt tên GameObject theo tên NPC
            go.name = npc.name;

            // 👉 Lật NPC theo hướng ngẫu nhiên
            Vector3 npcScale = go.transform.localScale;
            npcScale.x *= direction;
            go.transform.localScale = npcScale;

            // 👉 Gán tên hiển thị phía trên đầu NPC
            Transform nameTransform = go.transform.Find("Name");
            if (nameTransform != null)
            {
                Vector3 nameScale = nameTransform.localScale;
                nameScale.x *= direction;
                nameTransform.localScale = nameScale;

                TextMeshPro nameText = nameTransform.GetComponent<TextMeshPro>();
                if (nameText != null)
                {
                    // 👉 Viết hoa chữ cái đầu tiên
                    string displayName = npc.name;
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        displayName = char.ToUpper(displayName[0]) + displayName.Substring(1);
                    }

                    nameText.fontStyle = FontStyles.Bold;
                    nameText.text = $"<b><color=#00FF00><size=22>{displayName}</size></color></b>";
                }
            }


            // Xóa vùng tấn công nếu có
            ZoneAttack attack = go.GetComponentInChildren<ZoneAttack>();
            if (attack != null)
            {
                Destroy(attack);
            }

            // Gán thông tin ID
            NpcInfo info = go.AddComponent<NpcInfo>();
            info.id = npc.id;

            // Ẩn thanh máu
            Transform healthTransform = go.transform.Find("Heath_Bar");
            if (healthTransform != null)
            {
                healthTransform.gameObject.SetActive(false);
            }

            // Gán tag và part
            go.tag = "Npc";

            PartManager.Instance.ApplyParts(go,
                npc.hair, npc.body, npc.head, npc.facehair, npc.helmet,
                npc.armor, npc.hand, npc.leg, npc.boot, npc.weapon, npc.cloak
            );

            // Lưu vào map
            npcMap[npc.id] = go;
        }

        // Xoá NPC cũ không còn trong danh sách
        List<int> idsToRemove = new List<int>();
        foreach (int id in npcMap.Keys)
        {
            if (!newNpcIds.Contains(id))
            {
                idsToRemove.Add(id);
            }
        }

        foreach (int id in idsToRemove)
        {
            if (npcMap.TryGetValue(id, out GameObject npc))
            {
                GameObject.Destroy(npc);
                npcMap.Remove(id);
            }
        }
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
}
