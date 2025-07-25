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
    //[SerializeField] private float coordinateMultiplier = 5f;

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
                    var animator = player.GetComponentInChildren<Animator>();
                    if (animator != null && animator.GetBool("1_Move"))
                    {
                        animator.SetBool("1_Move", false);
                    }
                }
            }
        }

        UpdatePlayerSortingOrder();
    }

    public void HandleNpcList(byte[] data)
    {
        Debug.Log($"📦 Data nhận được: {data.Length} byte. First 10 bytes: {string.Join(",", data.Take(10))}");

        if (data.Length < 1)
        {
            Debug.LogError("❌ Payload quá ngắn.");
            return;
        }

        byte nameLen = data[0];
        if (nameLen <= 0 || nameLen > data.Length - 1)
        {
            Debug.LogError($"❌ nameLen không hợp lệ: {nameLen}");
            return;
        }

        int bytesRemaining = data.Length - 1 - nameLen;
        int dataNeeded = 4 + 4 + 4 + 11 * 4; // = 56 byte

        if (bytesRemaining < dataNeeded)
        {
            Debug.LogError($"❌ Không đủ dữ liệu để đọc NPC. nameLen={nameLen}, còn lại={bytesRemaining}, cần={dataNeeded}");
            return;
        }


        string name = Encoding.UTF8.GetString(data, 1, nameLen);
        int offset = 1 + nameLen;

        try
        {
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

            Debug.Log($"📘 NPC: {name}, ID: {id}");

            Npc npc = new Npc(id, name, x, y,
                head, facehair, helmet, hair, body, armor, hand, leg, boot, weapon, cloak);

            NpcManager.Instance.SpawnNpc(npc);
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Lỗi khi đọc NPC: {ex.Message}");
        }
    }



    public void HandleSpawnPlayer(byte[] data)
    {
        if (data.Length < 1) return;

        int nameLen = data[0];
        if (data.Length < 1 + nameLen + 68) return; // 64 = 16 (4 stats) + 8 (xy) + 40 (10 trang bị)

        string username = System.Text.Encoding.UTF8.GetString(data, 1, nameLen);
        int offset = 1 + nameLen;

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
        Debug.LogWarning($"Nhận part");
        SpawnPlayer(username, currentHP, maxHP, x, y, gold, diamond,
                    hair, body, head, facehair, helmet,
                    armor, hand, leg, boot, weapon,cloak);
    }




    public void HandleMoveAll(byte[] data)
    {
        int index = 0;
        while (index < data.Length)
        {
            if (index + 1 > data.Length) break;

            int nameLen = data[index++];
            if (nameLen <= 0 || index + nameLen + 60 > data.Length)
            {
                Debug.LogWarning($"❌ Dữ liệu không hợp lệ. nameLen = {nameLen}, index = {index}, data.Length = {data.Length}");
                break;
            }

            string name = Encoding.UTF8.GetString(data, index, nameLen);
            index += nameLen;

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

        int nameLen = data[0];
        if (data.Length < 1 + nameLen + 8)
        {
            Debug.LogWarning("❌ Dữ liệu 0x84 không hợp lệ. Bỏ qua.");
            return;
        }

        string name = Encoding.UTF8.GetString(data, 1, nameLen);
        if (name == SocketManager.Instance.Username)
            return;

        int offset = nameLen + 1;
        float x = BitConverter.ToSingle(data, offset);
        float y = BitConverter.ToSingle(data, offset + 4);
        Vector3 newPos = new Vector3(x, y, 0);


        if (otherPlayers.TryGetValue(name, out GameObject other))
        {
            if (!previousPositions.ContainsKey(name) || Vector3.Distance(previousPositions[name], newPos) > 0.01f)
            {
                if (previousPositions.TryGetValue(name, out Vector3 prevPos))
                {
                    float deltaX = newPos.x - prevPos.x;
                    float deltaY = newPos.y - prevPos.y;


                    if (Mathf.Abs(deltaX) > 0.01f)
                    {
                        Vector3 scale = other.transform.localScale;
                        float oldScaleX = scale.x;
                        scale.x = deltaX > 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
                        other.transform.localScale = scale;

                        var nameTransform = other.transform.Find("Name");
                        if (nameTransform != null)
                        {
                            Vector3 nameScale = nameTransform.localScale;
                            nameScale.x = Mathf.Abs(nameScale.x) * Mathf.Sign(scale.x);
                            nameTransform.localScale = new Vector3(nameScale.x, nameScale.y, nameScale.z); // ✅ Giữ nguyên scale.y, z
                            nameTransform.localRotation = Quaternion.identity;
                        }

                        var healthTransform = other.GetComponentsInChildren<Transform>(true)
                            .FirstOrDefault(t => t.CompareTag("Health"));

                        if (healthTransform != null)
                        {
                            
                            Vector3 healthScale = healthTransform.localScale;
                            healthScale.x = Mathf.Abs(healthScale.x) * (scale.x < 0 ? -1:1);
                            healthTransform.localScale = healthScale;
                            Vector3 healthPos = healthTransform.localPosition;
                            healthPos.x = Mathf.Abs(healthPos.x) * (scale.x > 0 ? -1 : 1);
                            healthTransform.localPosition = healthPos;

                            healthTransform.localRotation = Quaternion.identity;
                        }

                    }
                }

                targetPositions[name] = newPos;
                previousPositions[name] = newPos;

                // Set walk animation
                var animator = other.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.SetBool("1_Move", true);
                }
                lastMoveTimes[name] = Time.time;
            }
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
                        int boot, int weapon,int cloak)
    {
        GameObject player;

        if (username == SocketManager.Instance.Username)
        {
            if (currentPlayer != null)
            {
                // ✅ Cập nhật bản thân nếu đã tồn tại
                player = currentPlayer;
                player.transform.position = new Vector3(x, y, 0);
            }
            else
            {
                // 🆕 Tạo mới bản thân
                player = Instantiate(playerPrefab, new Vector3(x, y, 0), Quaternion.identity);
                currentPlayer = player;
                player.tag = "Player";
                player.AddComponent<PlayerMovement>();
                cammera.SetTarget(player.transform);
                Debug.Log("📍 Spawn bản thân");
            }
        }
        else
        {
            if (otherPlayers.TryGetValue(username, out player))
            {
                // ✅ Cập nhật player khác nếu đã tồn tại
                player.transform.position = new Vector3(x, y, 0);
            }
            else
            {
                // 🆕 Tạo mới player khác
                player = Instantiate(playerPrefab, new Vector3(x, y, 0), Quaternion.identity);
                player.tag = "Npc";
                otherPlayers[username] = player;
            }
        }

        // 🧠 Cập nhật thông tin
        Player stats = player.GetComponent<Player>() ?? player.AddComponent<Player>();
        stats.hp_max = maxHP;
        stats.hp_now = currentHP;
        stats.gold = gold;
        stats.diamond = diamond;

        float hp = maxHP > 0 ? (float)currentHP / maxHP : 0;

        var nameText = player.transform.Find("Name")?.GetComponent<TextMeshPro>();
        if (nameText != null)
            nameText.text = username;

        var healthTransform = player.transform.Find("HealthBar")?.Find("Health")?.GetComponent<Transform>();
        if (healthTransform == null)
        {
            healthTransform = player.GetComponentsInChildren<Transform>()
                                    .FirstOrDefault(t => t.CompareTag("Health"))?.transform;
        }

        if (healthTransform != null)
        {
            var controller = healthTransform.GetComponent<HealthBarController>() ??
                             healthTransform.gameObject.AddComponent<HealthBarController>();

            controller.lerpSpeed = healthLerpSpeed;
            controller.SetHP(hp);
        }
        else
        {
            Debug.LogWarning("[DEBUG] Không tìm thấy thanh máu với tag 'Health' hoặc dưới HealthBar/Health.");
        }

        PartManager.Instance.ApplyParts(player, hair, body, head, facehair, helmet,
                   armor, hand, leg, boot, weapon, cloak);
    }




    public void SpawnOtherPlayer(string name, int hp, int maxHP, float x, float y,
                            int hair, int body, int head, int facehair, int helmet,
                            int armor,
                            int hand, int leg,
                            int boot, int weapon,int cloak)
    {

        if (otherPlayers.TryGetValue(name, out GameObject existing))
        {
            existing.transform.position = new Vector3(x, y, 0);

            var controller = existing.GetComponentInChildren<HealthBarController>();
            if (controller != null)
            {
                controller.SetHP((float)hp / maxHP);
            }

            Animator otherAnimator = existing.GetComponentInChildren<Animator>();
            if (otherAnimator != null)
            {
                otherAnimator.SetBool("1_Move", false);
            }
            Debug.Log("Cập nhật lại player");
            // 👉 Gọi lại ApplyParts nếu player đã tồn tại
            PartManager.Instance.ApplyParts(existing, hair, body, head, facehair, helmet,
                       armor, hand, leg, boot, weapon, cloak);

            return;
        }


        GameObject player = Instantiate(playerPrefab, new Vector3(x, y, 0), Quaternion.identity);
        player.tag = "Npc"; // ✅ Gán tag là "Npc" cho người chơi khác

        // Hiển thị tên
        var nameText = player.transform.Find("Name")?.GetComponent<TextMeshPro>();
        if (nameText != null)
            nameText.text = name;

        // Health bar
        var healthTransform = player.transform.Find("HealthBar")?.Find("Health")?.GetComponent<Transform>();
        if (healthTransform == null)
        {
            healthTransform = player.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.CompareTag("Health"))?.transform;
        }

        if (healthTransform != null)
        {
            var controller = healthTransform.GetComponent<HealthBarController>();
            if (controller == null)
                controller = healthTransform.gameObject.AddComponent<HealthBarController>();

            controller.lerpSpeed = healthLerpSpeed;
            controller.SetHP((float)hp / maxHP);
        }

        Animator otherAnimator2 = player.GetComponentInChildren<Animator>();
        if (otherAnimator2 != null)
        {
            otherAnimator2.SetBool("1_Move", false);
        }

        otherPlayers[name] = player;
        previousPositions[name] = new Vector3(x, y, 0);

        // 👉 Gán parts như người chơi chính
        PartManager.Instance.ApplyParts(player, hair, body, head, facehair, helmet,
                   armor, hand, leg, boot, weapon, cloak);

    }


    private void UpdatePlayerSortingOrder()
    {
        if (currentPlayer == null) return;

        Vector3 myPos = currentPlayer.transform.position;
        var myGroup = currentPlayer.GetComponent<UnityEngine.Rendering.SortingGroup>();

        foreach (var kvp in otherPlayers)
        {
            GameObject other = kvp.Value;
            if (other == null) continue;

            Vector3 otherPos = other.transform.position;
            var otherGroup = other.GetComponent<UnityEngine.Rendering.SortingGroup>();

            if (Mathf.Abs(myPos.x - otherPos.x) <= 3f && myPos.y <= otherPos.y)
            {
                // Trường hợp đặc biệt: x nhỏ hơn và y nằm trong khoảng ±3
                if (myGroup != null) myGroup.sortingOrder = 20;
                if (otherGroup != null) otherGroup.sortingOrder = 10;
            }
            else
            {
                // Trường hợp mặc định: y thấp hơn → vẽ trên
                if (myGroup != null) myGroup.sortingOrder = -(int)(myPos.y * 1000);
                if (otherGroup != null) otherGroup.sortingOrder = -(int)(otherPos.y * 1000);
            }
        }
    }
}
