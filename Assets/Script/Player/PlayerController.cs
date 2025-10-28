using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.U2D.Animation; 
using static Unity.Burst.Intrinsics.X86.Avx;

public class PlayerController : MonoBehaviour
{
    public GameObject CurrentPlayer => currentPlayer;
    public GameObject playerPrefab;               // Gán prefab trong Inspector
    private GameObject currentPlayer;             // Đối tượng player hiện tại
    public CameraFollow cammera;
    private AudioSource[] audio;
    public CountdownTimer[] skills;
    public float healthLerpSpeed = 5f;
    private Dictionary<string, GameObject> otherPlayers = new Dictionary<string, GameObject>();
    private Dictionary<string, Vector3> previousPositions = new Dictionary<string, Vector3>();
    private Dictionary<string, float> lastMoveTimes = new Dictionary<string, float>();
    private Dictionary<string, Vector3> targetPositions = new Dictionary<string, Vector3>();
    public CHATWORLD chatw;
    private void Start()
    {
        audio = GetComponents<AudioSource>();
    }
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
        if (data == null || data.Length < 1)
        {
            Debug.LogWarning("⚠️ Gói tin Attack trống hoặc null");
            return;
        }

        int index = 0;

        // Đọc độ dài tên
        int nameLen = data[index++];
        if (nameLen <= 0)
        {
            Debug.LogWarning("⚠️ nameLen <= 0, gói tin lỗi");
            return;
        }

        // Kiểm tra còn đủ byte để đọc tên
        if (index + nameLen > data.Length)
        {
            Debug.LogWarning($"⚠️ Gói tin Attack thiếu dữ liệu tên. data.Length={data.Length}, cần={index + nameLen}");
            return;
        }

        string playerName = Encoding.UTF8.GetString(data, index, nameLen);
        index += nameLen;

        // 🔍 Kiểm tra còn đủ 8 byte cho 2 int (idMob + id_skill)
        if (index + 8 > data.Length)
        {
            Debug.LogError($"❌ Gói tin Attack bị thiếu dữ liệu! (index={index}, length={data.Length})");
            return;
        }

        // Đọc idMob
        int idMob = BitConverter.ToInt32(data, index);
        index += 4;

        // Đọc id_skill
        int id_skill = BitConverter.ToInt32(data, index);
        index += 4;


        // === Logic phần sau giữ nguyên ===
        GameObject attacker = null;
        if (playerName == SocketManager.Instance.Username && currentPlayer != null)
            attacker = currentPlayer;
        else if (otherPlayers.TryGetValue(playerName, out GameObject other))
            attacker = other;

        if (attacker == null)
        {
            Debug.LogWarning($"⚠️ Không tìm thấy player {playerName} để thực hiện Attack.");
            return;
        }

        // 🔥 Gọi animation nếu tồn tại trigger "2_Attack"
        Animator[] animators = attacker.GetComponentsInChildren<Animator>(true);
        foreach (var anim in animators)
        {
            // 🔹 Bỏ qua animator không có controller
            if (anim == null || anim.runtimeAnimatorController == null)
                continue;

            // 🔹 Kiểm tra parameter tồn tại
            if (anim.parameters.Any(p => p.name == "2_Attack" && p.type == AnimatorControllerParameterType.Trigger))
            {
                anim.SetTrigger("2_Attack");

                // Nếu là người chơi hiện tại thì xử lý UI skill
                if (attacker == currentPlayer)
                {
                    GameObject firstUISkill = Resources.FindObjectsOfTypeAll<GameObject>()
                        .FirstOrDefault(obj => obj.name == "UI_SKILL");

                    if (firstUISkill != null)
                    {
                        var s = firstUISkill.GetComponent<Skill>();
                        if (s != null)
                        {

                            int idx = s.selectedSkillIndex;

                            // ✅ Kiểm tra an toàn trước khi truy cập mảng
                            if (idx >= 0 && idx < skills.Length && skills[idx] != null)
                            {
                                //anim.SetTrigger("7_Skill");
                                skills[idx].StartCountdown();
                            }
                        }
                    }
                }

                // 🔊 Phát âm thanh
                if (audio != null && audio.Length > 0 && audio[0] != null)
                    audio[0].Play();
            }
        }
        // Gọi hiệu ứng khu vực
        foreach (var zatt in Resources.FindObjectsOfTypeAll<ZoneAttack>())
        {
            zatt.PlayAttackAnimation(idMob, -id_skill);
        }
    }



    public void HandleNpcList(byte[] data)
    {
        if (data.Length < 4)
        {
            Debug.LogError("❌ Payload quá ngắn (thiếu số lượng NPC).");
            return;
        }

        // Đọc int big-endian (Java mặc định writeInt là big-endian)
        int npcCount = (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
        int offset = 4;

        List<Npc> npcList = new List<Npc>();

        for (int i = 0; i < npcCount; i++)
        {
            if (offset >= data.Length)
            {
                Debug.LogError($"❌ Thiếu dữ liệu cho NPC thứ {i} (offset={offset}, length={data.Length})");
                break;
            }

            byte nameLen = data[offset++];
            if (offset + nameLen + 52 > data.Length)
            {
                Debug.LogError($"❌ Không đủ dữ liệu cho NPC thứ {i} (nameLen={nameLen}, offset={offset}, dataLen={data.Length})");
                break;
            }

            string name = Encoding.UTF8.GetString(data, offset, nameLen);
            offset += nameLen;

            // Các int còn lại server ghi little-endian -> cần đảo byte
            int id = ReadInt32LE(data, ref offset);
            float x = ReadFloatLE(data, ref offset);
            float y = ReadFloatLE(data, ref offset);
            int head = ReadInt32LE(data, ref offset);
            int facehair = ReadInt32LE(data, ref offset);
            int helmet = ReadInt32LE(data, ref offset);
            int hair = ReadInt32LE(data, ref offset);
            int body = ReadInt32LE(data, ref offset);
            int armor = ReadInt32LE(data, ref offset);
            int leg = ReadInt32LE(data, ref offset);
            int boot = ReadInt32LE(data, ref offset);
            int hand = ReadInt32LE(data, ref offset);
            int cloak = ReadInt32LE(data, ref offset);
            int weapon = ReadInt32LE(data, ref offset);

            // ⚙️ Khởi tạo NPC model theo thứ tự constructor của bạn
            Npc npc = new Npc(id, name, x, y,
                head, facehair, helmet, hair, body, armor,
                hand, leg, boot, weapon, cloak);

            npcList.Add(npc);
        }

        NpcManager.Instance.SpawnNpcs(npcList);
    }

    private int ReadInt32LE(byte[] data, ref int offset)
    {
        int value = BitConverter.ToInt32(data, offset);
        offset += 4;
        return value;
    }

    private float ReadFloatLE(byte[] data, ref int offset)
    {
        float value = BitConverter.ToSingle(data, offset);
        offset += 4;
        return value;
    }





    public void HandleChat(byte[] data)
    {
        try
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                byte type = reader.ReadByte(); // 1 byte

                int nameLen = reader.ReadByte(); // 1 byte
                string name = Encoding.UTF8.GetString(reader.ReadBytes(nameLen)); // tên người gửi

                // Đọc độ dài tin nhắn (2 byte Big Endian)
                byte[] msgLenBytes = reader.ReadBytes(2);
                if (msgLenBytes.Length < 2)
                {
                    Debug.LogWarning("⚠️ Dữ liệu chat không đủ 2 byte cho độ dài message.");
                    return;
                }
                int msgLen = (msgLenBytes[0] << 8) | msgLenBytes[1];

                // Đọc nội dung message
                string msg = Encoding.UTF8.GetString(reader.ReadBytes(msgLen));

                switch (type)
                {
                    case 0: // chat thường
                        ShowChatMessage(name,msg);
                        break;
                    case 1: // chat người chơi
                        chatw.HandleChatWork(name +": "+ msg);
                        break;
                    default:
                        Debug.LogWarning($"⚠️ Loại chat chưa xử lý: {type}");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Lỗi HandleChat: {ex.Message}");
        }
    }


    private void ShowChatMessage(string name, string msg)
    {
        GameObject target = null;

        // 🔍 Duyệt tất cả GameObject trong scene
        foreach (var obj in FindObjectsOfType<GameObject>())
        {
            if (obj.name == name)
            {
                target = obj;
                target.GetComponent<DisplayChat>().mess = msg;
                break; // ✅ Dừng ngay khi tìm thấy
            }
        }

        if (target == null)
        {
            Debug.LogWarning($"⚠️ Không tìm thấy GameObject có tên: {name}");
            return;
        }
    }


    public void HandleSpawnPlayer(byte[] data)
    {
        try
        {
            if (data == null || data.Length < 1)
            {
                Debug.LogWarning("[SpawnPlayer] ❌ Dữ liệu rỗng hoặc null");
                return;
            }

            int nameLen = data[0];
            if (data.Length < 1 + nameLen + 72)
            {
                Debug.LogWarning($"[SpawnPlayer] ❌ Gói tin quá ngắn. Length={data.Length}, nameLen={nameLen}");
                return;
            }

            // Đọc tên người chơi
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

            // 🧩 Log debug chi tiết
            //Debug.Log(
            //    $"[SpawnPlayer] ✅ {username}\n" +
            //    $"Type: {type}, Pos: ({x:F2}, {y:F2})\n" +
            //    $"HP: {currentHP}/{maxHP}, Gold: {gold}, Diamond: {diamond}\n" +
            //    $"Equip: head={head}, facehair={facehair}, helmet={helmet}, hair={hair}, body={body}, armor={armor}, hand={hand}, leg={leg}, boot={boot}, weapon={weapon}, cloak={cloak}"
            //);

            // Cập nhật thông tin người chơi
            SocketManager.Instance.Username = username;

            // Gọi hàm spawn player
            SpawnPlayer(username, currentHP, maxHP, x, y, gold, diamond,
                        hair, body, head, facehair, helmet,
                        armor, hand, leg, boot, weapon, cloak, type);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SpawnPlayer] ❌ Lỗi khi giải mã gói tin: {ex.Message}\n{ex.StackTrace}");
        }
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
        player.name = username;
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
        player.name = name;

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
