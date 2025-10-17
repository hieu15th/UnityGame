using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class MobsManager : MonoBehaviour
{
    public List<Mobs> receivedMobs = new List<Mobs>();
    public List<GameObject> mobPrefabs; // Prefab theo part
    private Dictionary<int, GameObject> mobInstances = new Dictionary<int, GameObject>(); // Lưu mob theo ID

    public void handleSpawmMobs(byte[] data)
    {
        int offset = 0;
        receivedMobs.Clear();

        if (data.Length < 1)
        {
            Debug.LogError("❌ Dữ liệu không hợp lệ (dài quá ngắn).");
            return;
        }

        byte index = data[offset++];
        switch (index)
        {
            case 0:
                // Kiểm tra độ dài dữ liệu trước khi đọc mobCount
                if (offset + 4 > data.Length)
                {
                    Debug.LogError("❌ Không đủ dữ liệu để đọc mobCount.");
                    return;
                }

                int mobCount = BitConverter.ToInt32(data, offset);
                offset += 4;

                // Kiểm tra xem số lượng mob có hợp lệ không
                if (mobCount < 0 || offset + mobCount * 4 > data.Length)
                {
                    Debug.LogError($"❌ Không đủ dữ liệu để đọc {mobCount} mobs.");
                    return;
                }

                for (int i = 0; i < mobCount; i++)
                {
                    Mobs mob = new Mobs();

                    // Kiểm tra ID của mob
                    if (offset + 4 > data.Length)
                    {
                        Debug.LogError($"❌ Không đủ dữ liệu để đọc ID của mob thứ {i}");
                        return;
                    }
                    mob.id = BitConverter.ToInt32(data, offset);
                    offset += 4;

                    // Kiểm tra tên mob
                    if (offset + 1 > data.Length)
                    {
                        Debug.LogError($"❌ Không đủ dữ liệu để đọc độ dài tên mob thứ {i}");
                        return;
                    }

                    int nameLen = data[offset++];
                    if (nameLen < 0 || offset + nameLen > data.Length)
                    {
                        Debug.LogError($"❌ Độ dài tên không hợp lệ hoặc vượt quá mảng (nameLen={nameLen}) ở mob thứ {i}");
                        return;
                    }

                    mob.name = Encoding.UTF8.GetString(data, offset, nameLen);
                    offset += nameLen;

                    // Kiểm tra chỉ số các thuộc tính mob
                    if (offset + 4 * 4 > data.Length)
                    {
                        Debug.LogError($"❌ Không đủ dữ liệu để đọc chỉ số mob thứ {i} sau tên.");
                        return;
                    }

                    mob.current_hp = BitConverter.ToInt32(data, offset); offset += 4;
                    mob.hp = BitConverter.ToInt32(data, offset); offset += 4;
                    mob.dame = BitConverter.ToInt32(data, offset); offset += 4;
                    mob.part = BitConverter.ToInt32(data, offset); offset += 4;
                    mob.x = BitConverter.ToSingle(data, offset); offset += 4;
                    mob.y = BitConverter.ToSingle(data, offset); offset += 4;

                    receivedMobs.Add(mob);
                }

                SpawmMobs();
                break;

            case 1:
                // Kiểm tra dữ liệu trước khi đọc ID và HP
                if (offset + 8 > data.Length)
                {
                    return;
                }

                int id = BitConverter.ToInt32(data, offset);
                offset += 4;
                int hp_del = BitConverter.ToInt32(data, offset);
                offset += 4;
                int type = BitConverter.ToInt32(data, offset);
                offset += 4;
                // Cập nhật thông tin mob nếu có
                if (mobInstances.ContainsKey(id))
                {
                    GameObject existing = mobInstances[id];
                    var mover = existing.GetComponent<MobMover>();
                    MobData mobData = existing.GetComponent<MobData>();
                    if (mobData != null)
                    {
                        mobData.hp_del = hp_del;
                        mobData.type = type;
                    }
                }
                break;
            case 2:
                if (offset + 4 > data.Length)
                {
                    Debug.LogError("❌ Không đủ dữ liệu để đọc ID và HP của mob.");
                    return;
                }

                int id_attack = BitConverter.ToInt32(data, offset);
                offset += 4;

                // Cập nhật thông tin mob nếu có
                if (mobInstances.ContainsKey(id_attack))
                {
                    GameObject existing = mobInstances[id_attack];
                    var mover = existing.GetComponent<MobMover>();
                    MobData mobData = existing.GetComponent<MobData>();
                    if (mobData != null)
                    {
                        mobData.attack = true;
                    }
                }
                break;
        }
    }


    public void SpawmMobs()
    {
        HashSet<int> receivedIds = new HashSet<int>();
        foreach (var mob in receivedMobs)
        {
            receivedIds.Add(mob.id);
        }

        List<int> toRemove = new List<int>();
        foreach (var id in mobInstances.Keys)
        {
            if (!receivedIds.Contains(id))
            {
                GameObject mobObj = mobInstances[id];
                var mobData = mobObj.GetComponent<MobData>();
                if (mobData != null && mobData.current_hp != 0) 
                {
                    toRemove.Add(id);
                }
            }
        }

        // Xóa mob cũ khỏi scene và dictionary
        foreach (int id in toRemove)
        {
            GameObject oldMob = mobInstances[id];
            Destroy(oldMob);
            mobInstances.Remove(id);
        }


        foreach (var mob in receivedMobs)
        {
            Vector3 newPos = new Vector3(mob.x, mob.y, 0);

            if (mobInstances.ContainsKey(mob.id))
            {
                GameObject existing = mobInstances[mob.id];
                var mover = existing.GetComponent<MobMover>();
                if (mover != null)
                {
                    mover.SetTargetPosition(mob.x, mob.y);
                }
                MobData mobData = existing.GetComponent<MobData>();
                if (mobData != null)
                {
                    mobData.id = mob.id;
                    mobData.name = mob.name;
                    mobData.current_hp = mob.current_hp;
                    mobData.hp = mob.hp;
                    mobData.dame = mob.dame;
                    mobData.part = mob.part;
                    if (mobData.current_hp == 0)
                    {
                        mobInstances.Remove(mob.id);
                    }
                }
            }
            else
            {
                if (mob.current_hp != 0)
                {
                    int index = mob.part;
                    if (index >= 1 && index <= mobPrefabs.Count && mobPrefabs[index - 1] != null)
                    {
                        GameObject prefab = mobPrefabs[index - 1];
                        GameObject newMob = Instantiate(prefab, newPos, Quaternion.identity);
                        MobData mobData = newMob.GetComponent<MobData>();
                        if (mobData == null)
                        {
                            mobData = newMob.AddComponent<MobData>();
                        }
                        mobData.id = mob.id;
                        mobData.name = mob.name;
                        mobData.current_hp = mob.current_hp;
                        mobData.hp = mob.hp;
                        mobData.dame = mob.dame;
                        mobData.part = mob.part;

                        mobInstances[mob.id] = newMob;
                        var mover = newMob.GetComponent<MobMover>();
                        if (mover != null)
                        {
                            mover.SetTargetPosition(mob.x, mob.y);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Không tìm thấy prefab cho part={index} (ID={mob.id})");
                    }
                }
            }
        }
    }


}
