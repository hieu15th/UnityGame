using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.U2D.Animation; 
using static Unity.Burst.Intrinsics.X86.Avx;

public class PlayerController : MonoBehaviour
{
    public GameObject CurrentPlayer => currentPlayer;
    public GameObject playerPrefab;               // Gán prefab trong Inspector
    private GameObject currentPlayer;             // Đối tượng player hiện tại
    public CameraFollow cammera;
    public float healthLerpSpeed = 5f;
    private Dictionary<string, GameObject> otherPlayers = new Dictionary<string, GameObject>();
    private Dictionary<string, Vector3> previousPositions = new Dictionary<string, Vector3>();
    private Dictionary<string, float> lastMoveTimes = new Dictionary<string, float>();
    private Dictionary<string, Vector3> targetPositions = new Dictionary<string, Vector3>();
    public ZoneAttack zatt;
    void Update()
    {
        float currentTime = Time.time;
        foreach (var kvp in otherPlayers)
        {
            string name = kvp.Key;
            GameObject player = kvp.Value;

            if (targetPositions.TryGetValue(name, out Vector3 targetPos))
            {
                Vector3 currentPos = player.transform.position;
                float speed = 1f;
                if (Vector3.Distance(currentPos, targetPos) > 0.01f)
                {
                    player.transform.position = Vector3.MoveTowards(currentPos, targetPos, speed * Time.deltaTime);
                }
            }

            if (lastMoveTimes.TryGetValue(name, out float lastTime))
            {
                if (currentTime - lastTime > 0.1f)
                {
                    Animator[] animators = player.GetComponentsInChildren<Animator>(true);
                    foreach (var anim in animators)
                    {
                        // Kiểm tra xem animator có parameter "1_Move" không
                        if (anim.parameters.Any(p => p.name == "1_Move"))
                        {
                            anim.SetBool("1_Move", false);
                        }
                    }

                }
            }
        }
    }
    public void HandleAttack(byte[] data)
    {
        if (data.Length < 1) return;

        int index = 0;

        // Đọc tên
        int nameLen = data[index++];
        if (nameLen <= 0 || data.Length < index + nameLen + 4)
        {
            Debug.LogError("❌ Dữ liệu Attack không hợp lệ.");
            return;
        }

        string playerName = Encoding.UTF8.GetString(data, index, nameLen);
        index += nameLen;

        // Đọc idMob
        int idMob = BitConverter.ToInt32(data, index);
        index += 4;

        Debug.Log($"⚔️ {playerName} attack mob {idMob}");

        GameObject attacker = null;

        // ✅ Kiểm tra có phải bản thân không
        if (playerName == SocketManager.Instance.Username && currentPlayer != null)
        {
            attacker = currentPlayer;
        }
        else if (otherPlayers.TryGetValue(playerName, out GameObject other))
        {
            Debug.LogWarning($"Tìm thấy player {playerName} để thực hiện Attack.");
            attacker = other;
        }

        if (attacker != null)
        {
            Animator[] animators = attacker.GetComponentsInChildren<Animator>(true);
            foreach (var anim in animators)
            {
                // Kiểm tra xem animator có parameter "2_Attack" không
                if (anim.parameters.Any(p => p.name == "2_Attack" && p.type == AnimatorControllerParameterType.Trigger))
                {
                    anim.SetTrigger("2_Attack");
                }
            }

            ZoneAttack[] allZoneAttacks = Resources.FindObjectsOfTypeAll<ZoneAttack>();
            foreach (var zatt in allZoneAttacks)
            {
                zatt.PlayAttackAnimation(idMob);
            }

        }
        else
        {
            Debug.LogWarning($"⚠️ Không tìm thấy player {playerName} để thực hiện Attack.");
        }
    }


    public void HandleNpcList(byte[] data)
    {
        if (data.Length < 2)
        {
            Debug.LogError("❌ Payload quá ngắn.");
            return;
        }

        int npcCount = BitConverter.ToInt32(data, 0); // ✔️ đúng vì Java ghi 4 byte
        int offset = 4; // ✔️ tăng offset lên 4

        List<Npc> npcList = new List<Npc>();

        for (int i = 0; i < npcCount; i++)
        {
            if (offset >= data.Length) break;

            byte nameLen = data[offset]; offset++;
            if (offset + nameLen + 56 > data.Length)
            {
                Debug.LogError($"❌ Không đủ dữ liệu cho NPC thứ {i}.");
                break;
            }

            string name = Encoding.UTF8.GetString(data, offset, nameLen); offset += nameLen;

            int id = BitConverter.ToInt32(data, offset); offset += 4;
            float x = BitConverter.ToSingle(data, offset); offset += 4;
            float y = BitConverter.ToSingle(data, offset); offset += 4;

            int head = BitConverter.ToInt32(data, offset); offset += 4;
            int facehair = BitConverter.ToInt32(data, offset); offset += 4;
            int helmet = BitConverter.ToInt32(data, offset); offset += 4;
            int hair = BitConverter.ToInt32(data, offset); offset += 4;
            int body = BitConverter.ToInt32(data, offset); offset += 4;
            int armor = BitConverter.ToInt32(data, offset); offset += 4;
            int leg = BitConverter.ToInt32(data, offset); offset += 4;
            int boot = BitConverter.ToInt32(data, offset); offset += 4;
            int hand = BitConverter.ToInt32(data, offset); offset += 4;
            int cloak = BitConverter.ToInt32(data, offset); offset += 4;
            int weapon = BitConverter.ToInt32(data, offset); offset += 4;

            Npc npc = new Npc(id, name, x, y,
                head, facehair, helmet, hair, body, armor, hand, leg, boot, weapon, cloak);

            npcList.Add(npc);
        }
        NpcManager.Instance.SpawnNpcs(npcList);
    }




    public void HandleSpawnPlayer(byte[] data)
    {
        if (data.Length < 1) return;

        int nameLen = data[0];
        if (data.Length < 1 + nameLen + 72) return; // 64 = 16 (4 stats) + 8 (xy) + 40 (10 trang bị)

        string username = System.Text.Encoding.UTF8.GetString(data, 1, nameLen);
        int offset = 1 + nameLen;
        int type = BitConverter.ToInt32(data, offset); offset += 4;

        float x = BitConverter.ToSingle(data, offset); offset += 4;
        float y = BitConverter.ToSingle(data, offset); offset += 4;

        int currentHP = BitConverter.ToInt32(data, offset); offset += 4;
        int maxHP = BitConverter.ToInt32(data, offset); offset += 4;

        int gold = BitConverter.ToInt32(data, offset); offset += 4;
        int diamond = BitConverter.ToInt32(data, offset); offset += 4;

        int head = BitConverter.ToInt32(data, offset); offset += 4;
        int facehair = BitConverter.ToInt32(data, offset); offset += 4;
        int helmet = BitConverter.ToInt32(data, offset); offset += 4;
        int hair = BitConverter.ToInt32(data, offset); offset += 4;
        int body = BitConverter.ToInt32(data, offset); offset += 4;
        int armor = BitConverter.ToInt32(data, offset); offset += 4;
        int hand = BitConverter.ToInt32(data, offset); offset += 4;
        int leg = BitConverter.ToInt32(data, offset); offset += 4;
        int boot = BitConverter.ToInt32(data, offset); offset += 4;
        int weapon = BitConverter.ToInt32(data, offset); offset += 4;
        int cloak = BitConverter.ToInt32(data, offset); offset += 4;

        SocketManager.Instance.Username = username;
        SpawnPlayer(username, currentHP, maxHP, x, y, gold, diamond,
                    hair, body, head, facehair, helmet,
                    armor, hand, leg, boot, weapon,cloak,type);
    }




    public void HandleMoveAll(byte[] data)
    {
        int index = 0;
        while (index < data.Length)
        {
            if (index + 1 > data.Length) break;

            int nameLen = data[index++];
            if (nameLen <= 0 || index + nameLen + 64 > data.Length)
            {
                Debug.LogWarning($"❌ Dữ liệu không hợp lệ. nameLen = {nameLen}, index = {index}, data.Length = {data.Length}");
                break;
            }

            string name = Encoding.UTF8.GetString(data, index, nameLen);
            index += nameLen;
            int type = BitConverter.ToInt32(data, index); index += 4;

            float x = BitConverter.ToSingle(data, index); index += 4;
            float y = BitConverter.ToSingle(data, index); index += 4;

            int currentHP = BitConverter.ToInt32(data, index); index += 4;
            int maxHP = BitConverter.ToInt32(data, index); index += 4;

            int head = BitConverter.ToInt32(data, index); index += 4;
            int facehair = BitConverter.ToInt32(data, index); index += 4;
            int helmet = BitConverter.ToInt32(data, index); index += 4;
            int hair = BitConverter.ToInt32(data, index); index += 4;
            int body = BitConverter.ToInt32(data, index); index += 4;
            int armor = BitConverter.ToInt32(data, index); index += 4;
            int hand = BitConverter.ToInt32(data, index); index += 4;
            int leg = BitConverter.ToInt32(data, index); index += 4;
            int boot = BitConverter.ToInt32(data, index); index += 4;
            int weapon = BitConverter.ToInt32(data, index); index += 4;
            int cloak = BitConverter.ToInt32(data, index); index += 4;
            Debug.LogWarning($"Nhận dữ liệu người chơi khác");

            SpawnOtherPlayer(name, currentHP, maxHP, x, y,
                    hair, body, head, facehair, helmet,
                    armor,
                    hand, leg,
                    boot, weapon, cloak);
        }
    }



    public void HandleMove(byte[] data)
    {
        if (data.Length < 1) return;

        int index = 0;
        int nameLen = data[index++];
        if (nameLen <= 0 || data.Length < index + nameLen + 64) return;

        string name = Encoding.UTF8.GetString(data, index, nameLen);
        index += nameLen;

        //if (name == SocketManager.Instance.Username)
        //    return;
        int type = BitConverter.ToInt32(data, index); index += 4;

        float x = BitConverter.ToSingle(data, index); index += 4;
        float y = BitConverter.ToSingle(data, index); index += 4;
        int currentHP = BitConverter.ToInt32(data, index); index += 4;
        int maxHP = BitConverter.ToInt32(data, index); index += 4;

        int head = BitConverter.ToInt32(data, index); index += 4;
        int body = BitConverter.ToInt32(data, index); index += 4;
        int facehair = BitConverter.ToInt32(data, index); index += 4;
        int helmet = BitConverter.ToInt32(data, index); index += 4;
        int hair = BitConverter.ToInt32(data, index); index += 4;
        int armor = BitConverter.ToInt32(data, index); index += 4;
        int hand = BitConverter.ToInt32(data, index); index += 4;
        int leg = BitConverter.ToInt32(data, index); index += 4;
        int boot = BitConverter.ToInt32(data, index); index += 4;
        int weapon = BitConverter.ToInt32(data, index); index += 4;
        int cloak = BitConverter.ToInt32(data, index); index += 4;

        Vector3 newPos = new Vector3(x, y, 0);

        if (!otherPlayers.TryGetValue(name, out GameObject other))
        {
            Debug.Log($"🟡 Người chơi mới xuất hiện: {name}");
            SpawnOtherPlayer(name, currentHP, maxHP, x, y,
                hair, body, head, facehair, helmet,
                armor, hand, leg, boot, weapon, cloak);
            return;
        }

        if (!previousPositions.ContainsKey(name) || Vector3.Distance(previousPositions[name], newPos) > 0.01f)
        {
            Vector3 prevPos = previousPositions.ContainsKey(name) ? previousPositions[name] : newPos;

            float deltaX = newPos.x - prevPos.x;
            float deltaY = newPos.y - prevPos.y;

            // Xoay hướng
            if (Mathf.Abs(deltaX) > 0.01f)
            {
                Vector3 scale = other.transform.Find("UnitRoot").localScale;
                scale.x = deltaX > 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
                other.transform.Find("UnitRoot").localScale = scale;
            }

            targetPositions[name] = newPos;
            previousPositions[name] = newPos;

            Animator[] animators = other.GetComponentsInChildren<Animator>(true);
            foreach (var anim in animators)
            {
                if (anim.parameters.Any(p => p.name == "1_Move"))
                {
                    anim.SetBool("1_Move", true);
                }
            }

            lastMoveTimes[name] = Time.time;
        }
    }




    public void HandleDisconnect(byte[] data)
    {
        if (data.Length < 1) return;

        int nameLen = data[0];
        if (data.Length < 1 + nameLen) return;

        string name = System.Text.Encoding.UTF8.GetString(data, 1, nameLen);

        if (otherPlayers.ContainsKey(name))
        {
            Destroy(otherPlayers[name]);
            otherPlayers.Remove(name);
            previousPositions.Remove(name);
            targetPositions.Remove(name);
            lastMoveTimes.Remove(name);
        }
    }

    public void SpawnPlayer(string username, int currentHP, int maxHP, float x, float y,
                        int gold, int diamond,
                        int hair, int body, int head, int facehair, int helmet,
                        int armor, int hand, int leg,
                        int boot, int weapon, int cloak, int type)
    {
        if (username != SocketManager.Instance.Username)
        {
            Debug.LogWarning($"SpawnPlayer chỉ dành cho chính bản thân, bỏ qua {username}");
            return;
        }

        GameObject player;

        if (currentPlayer != null)
        {
            // ✅ Nếu đã có player, chỉ cập nhật vị trí khi type == 0
            player = currentPlayer;
            if (type == 0)
            {
                player.transform.position = new Vector3(x, y, 0);
            }
        }
        else
        {
            // 🆕 Tạo mới player bản thân
            player = Instantiate(playerPrefab, new Vector3(x, y, 0), Quaternion.identity);
            currentPlayer = player;
            player.tag = "Player";
            player.AddComponent<PlayerMovement>();
            cammera.SetTarget(player.transform);
        }

        // 🧠 Cập nhật thông tin stats
        Player stats = player.GetComponent<Player>() ?? player.AddComponent<Player>();
        stats.hp_max = maxHP;
        stats.hp_now = currentHP;
        stats.gold = gold;
        stats.diamond = diamond;

        float hp = maxHP > 0 ? (float)currentHP / maxHP : 0;

        // 🔤 Tên
        var nameText = player.transform.Find("Name")?.GetComponent<TextMeshPro>();
        if (nameText != null)
            nameText.text = username;

        // ❤️ Thanh máu
        var healthTransform = player.transform.Find("HealthBar")?.Find("Health")?.GetComponent<Transform>();
        if (healthTransform == null)
            healthTransform = player.GetComponentsInChildren<Transform>()
                                    .FirstOrDefault(t => t.CompareTag("Health"))?.transform;

        if (healthTransform != null)
        {
            var controller = healthTransform.GetComponent<HealthBarController>()
                             ?? healthTransform.gameObject.AddComponent<HealthBarController>();
            controller.lerpSpeed = healthLerpSpeed;
            controller.SetHP(hp);
        }

        // 👕 Gán parts
        PartManager.Instance.ApplyParts(player, hair, body, head, facehair, helmet,
                                       armor, hand, leg, boot, weapon, cloak);
    }



    public void SpawnOtherPlayer(string name, int hp, int maxHP, float x, float y,
                             int hair, int body, int head, int facehair, int helmet,
                             int armor, int hand, int leg, int boot, int weapon, int cloak)
    {
        Vector3 spawnPos = new Vector3(x, y, 0);

        GameObject player;

        // Nếu player đã tồn tại → cập nhật vị trí mới
        if (otherPlayers.TryGetValue(name, out GameObject existing))
        {
            player = existing;
            player.transform.position = spawnPos;
            previousPositions[name] = spawnPos;
        }
        else
        {
            // Tạo mới player
            player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            player.tag = "Npc";

            var nameText = player.transform.Find("Name")?.GetComponent<TextMeshPro>();
            if (nameText != null) nameText.text = name;

            otherPlayers[name] = player;
            previousPositions[name] = spawnPos;
        }

        // Health bar
        var healthTransform = player.transform.Find("HealthBar")?.Find("Health")?.GetComponent<Transform>()
                              ?? player.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.CompareTag("Health"))?.transform;

        if (healthTransform != null)
        {
            var controller = healthTransform.GetComponent<HealthBarController>()
                             ?? healthTransform.gameObject.AddComponent<HealthBarController>();
            controller.lerpSpeed = healthLerpSpeed;
            controller.SetHP((float)hp / maxHP);
        }

        // Stats
        Player stats = player.GetComponent<Player>() ?? player.AddComponent<Player>();
        stats.hp_max = maxHP;
        stats.hp_now = hp;

        // Animator reset trạng thái move
        Animator[] animators = player.GetComponentsInChildren<Animator>(true);
        foreach (var anim in animators)
        {
            if (anim.parameters.Any(p => p.name == "1_Move"))
                anim.SetBool("1_Move", false);
        }

        // Gán parts
        PartManager.Instance.ApplyParts(player, hair, body, head, facehair, helmet,
                                        armor, hand, leg, boot, weapon, cloak);
    }


}
