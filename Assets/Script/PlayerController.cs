using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    void ApplyParts(GameObject player, int hair, int body, int head, int facehair, int helmet, int armor,
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

        // 👉 Gán part đầy đủ
        ApplyParts(player, hair, body, head, facehair, helmet,
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
            ApplyParts(existing, hair, body, head, facehair, helmet,
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
        ApplyParts(player, hair, body, head, facehair, helmet,
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
