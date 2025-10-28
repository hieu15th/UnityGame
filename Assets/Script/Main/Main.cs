using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class Main : MonoBehaviour
{
    private Thread listenThread;
    private bool isRunning = false;
    private bool isBanned = false;

    private const sbyte CMD_REQUEST_PLAYER = -125;
    private const sbyte CMD_MOVE = -124;
    private const sbyte CMD_MOVE_ALL = -123;
    private const sbyte CMD_BAND = -122;
    private const sbyte CMD_DISCONNECT = -121;
    private const sbyte CMD_GETBAG = -120;
    private const sbyte CMD_ITEM_EQUIP = -119;
    private const sbyte CMD_SEND_ALERT = -117;
    private const sbyte CMD_PLAYER_STATS = -116;
    private const sbyte CMD_REQUEST_NPC = -115;
    private const sbyte CMD_SEND_NPC = -114;
    private const sbyte CMD_SEND_UI = -113;
    private const sbyte CMD_SEND_MOBS = -112;
    private const sbyte CMD_SEND_ATTACK = -111;
    private const sbyte CMD_SEND_QUANTITY_MOB = -110;
    private const sbyte CMD_SHOP = -109;
    private const sbyte CMD_INFOR_UPGRADE = -108;
    private const sbyte CMD_ALL_SKILL = -107;
    private const sbyte CMD_SKILL = -106;
    private const sbyte CMD_SKILL_COUNTDOWN = -105;
    private const sbyte CMD_CHAT = -104;

    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
    [SerializeField] private PlayerController playerHandler;
    [SerializeField] private BagLayoutAdjuster bagUI;
    [SerializeField] private EquipLayoutAdjuster charUI;
    [SerializeField] private BoxAlertUI boxAlertUI;
    [SerializeField] private OptionPlayer optionPlayer;
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private MessageManager mess;
    [SerializeField] private UIManager UI_manager;
    [SerializeField] private MobsManager MobsManager;
    [SerializeField] private NoteAddItem noteAddItem;
    [SerializeField] private PlayerUI playerUI;
    [SerializeField] private ShopLayout shopUI;
    [SerializeField] private UpgradeUI upgradeUI;
    [SerializeField] private SkillUI skillUI;
    [SerializeField] private Skill skill;

    void Start()
    {
        if (mess == null)
        {
            mess = FindAnyObjectByType<MessageManager>();
            if (mess == null)
            {
                Debug.LogError("❌ Không tìm thấy MessageManager trong scene.");
                return;
            }
        }

        mess.SendRequest(-125);
        mess.SendRequest(-115);
        StartListening();
    }

    private void StartListening()
    {
        if (SocketManager.Instance?.Reader == null)
        {
            Debug.LogError("⚠️ SocketManager chưa sẵn sàng, không thể start listen.");
            return;
        }

        if (listenThread != null && listenThread.IsAlive)
        {
            isRunning = false;
            listenThread.Interrupt();
            listenThread.Join(500);
        }

        isRunning = true;
        listenThread = new Thread(ListenToServer)
        {
            IsBackground = true
        };
        listenThread.Start();

        Debug.Log("🎧 Listen thread started successfully");
    }


    void Update()
    {
        while (mainThreadActions.TryDequeue(out Action action))
            action?.Invoke();
    }

    void OnDestroy()
    {
        isRunning = false;
        if (listenThread != null && listenThread.IsAlive)
            listenThread.Interrupt();
    }


    private void EnqueueMainThread(Action action)
    {
        mainThreadActions.Enqueue(action);
    }


    private void ListenToServer()
    {
        try
        {
            var reader = SocketManager.Instance.Reader;
            byte[] recvBuffer = new byte[2048];
            MemoryStream buffer = new MemoryStream();

            while (isRunning)
            {
                int bytesRead = reader.BaseStream.Read(recvBuffer, 0, recvBuffer.Length);
                if (bytesRead <= 0)
                {
                    //Debug.LogWarning($"[{DateTime.Now:HH:mm:ss.fff}] ⚠️ Server ngắt kết nối hoặc socket bị đóng.");
                    break;
                }

                buffer.Write(recvBuffer, 0, bytesRead);
                //Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] 📩 Nhận {bytesRead} bytes, tổng buffer hiện tại = {buffer.Length}");

                while (true)
                {
                    // Chưa đủ header
                    if (buffer.Length < 3)
                    {
                        //Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] 🟡 Chưa đủ header (mới có {buffer.Length}/3 bytes).");
                        break;
                    }

                    byte[] buf = buffer.ToArray();
                    sbyte cmd = (sbyte)buf[0];
                    ushort size = (ushort)((buf[1] << 8) | buf[2]);

                    if (size > 10000)
                    {
                        string time = DateTime.Now.ToString("HH:mm:ss.fff");

                        string logText =
                            $"[{time}] 🚨 Gói bất thường: CMD={cmd}, size={size}, buffer={buffer.Length}\n" +
                            $"[{time}] ⛔ HEX: {BitConverter.ToString(buf, 0, Math.Min(32, buf.Length))}\n";

                        // 🔴 In cảnh báo ra console
                        Debug.LogError(logText);

                        // 🧊 Hiển thị CMD bất thường trên màn hình
                        GameObject alertObj = new GameObject("AbnormalPacketAlert");
                        Canvas canvas = alertObj.AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        alertObj.AddComponent<CanvasScaler>();
                        alertObj.AddComponent<GraphicRaycaster>();

                        GameObject textObj = new GameObject("AlertText");
                        textObj.transform.SetParent(alertObj.transform, false);
                        var tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
                        tmp.text = $"🚨 CMD={cmd}\nSIZE={size}\nĐã bị đóng băng.";
                        tmp.fontSize = 48;
                        tmp.color = Color.red;
                        tmp.alignment = TMPro.TextAlignmentOptions.Center;
                        tmp.rectTransform.sizeDelta = new Vector2(800, 400);
                        tmp.rectTransform.anchoredPosition = Vector2.zero;

                        // 🧱 Đóng băng toàn bộ game (ngừng Update, animation, physics)
                        Time.timeScale = 0f;

#if UNITY_EDITOR
                        Debug.Break(); // Chỉ dừng Editor, không ảnh hưởng khi build
#endif

                        isRunning = false;
                        break;
                    }


                    // Nếu chưa đủ dữ liệu cho gói này
                    if (buffer.Length < 3 + size)
                    {
                        //Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] ⏳ Gói CMD={cmd} (0x{cmd:X2}) CHƯA ĐỦ: {buffer.Length}/{3 + size} bytes.");
                        //Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}]    HEX hiện tại: {BitConverter.ToString(buf, 0, (int)buffer.Length)}");
                        break;
                    }

                    // ✅ Đã đủ dữ liệu -> xử lý
                    byte[] data = new byte[size];
                    Array.Copy(buf, 3, data, 0, size);

                    int remaining = (int)(buffer.Length - (3 + size));

                    // Dọn buffer cũ
                    buffer.SetLength(0);
                    if (remaining > 0)
                        buffer.Write(buf, 3 + size, remaining);

                    //Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Gói HOÀN CHỈNH: CMD={cmd} (0x{cmd:X2}), size={size}, data đầu={BitConverter.ToString(data, 0, Math.Min(8, data.Length))}");
                    HandleCommand(cmd, data);
                }
            }
        }
        catch (Exception ex)
        {
            if (!isBanned)
            {
                EnqueueMainThread(() =>
                {
                    if (boxAlertUI != null)
                    {
                        SocketManager.Instance.ResetConnection();
                        listenThread?.Interrupt();
                        listenThread = null;
                        boxAlertUI.Band("Máy chủ hiện đang bảo trì");
                    }
                });
            }
            else
            {
                isRunning = false;
                listenThread?.Interrupt();
                listenThread = null;
            }
        }
    }



    public void logout()
    {
        SocketManager.Instance.ResetConnection();
        isRunning = false;
        listenThread?.Interrupt();
        listenThread = null;
        Destroy(gameObject);
        SceneManager.LoadScene("Login");
    }
    private void HandleCommand(sbyte cmd, byte[] data)
    {
        try
        {
            switch (cmd)
            {
                case CMD_REQUEST_PLAYER:
                    SafeInvoke("HandleSpawnPlayer", () => playerHandler.HandleSpawnPlayer(data));
                    break;

                case CMD_MOVE_ALL:
                    SafeInvoke("HandleMoveAll", () => playerHandler.HandleMoveAll(data));
                    break;

                case CMD_MOVE:
                    SafeInvoke("HandleMove", () => playerHandler.HandleMove(data));
                    break;

                case CMD_DISCONNECT:
                    SafeInvoke("HandleDisconnect", () => playerHandler.HandleDisconnect(data));
                    break;

                case CMD_BAND:
                    isBanned = true;
                    isRunning = false;
                    EnqueueMainThread(() =>
                    {
                        try
                        {
                            if (boxAlertUI != null)
                            {
                                SocketManager.Instance.ResetConnection();
                                listenThread?.Interrupt();
                                listenThread = null;
                                boxAlertUI.Band("Tài khoản được đăng nhập ở nơi khác");
                            }
                            else
                                Debug.LogWarning("⚠️ boxAlertUI chưa được gán trong Main");
                        }
                        catch (Exception ex)
                        {
                            FreezeGame($"❌ Lỗi trong CMD_BAND: {ex.Message}", ex);
                        }
                    });
                    break;

                case CMD_GETBAG:
                    SafeInvoke("HandleBagData", () => bagUI?.HandleBagData(data));
                    break;

                case CMD_ITEM_EQUIP:
                    SafeInvoke("HandleEquipData", () => charUI?.HandleEquipData(data));
                    break;

                case CMD_SEND_ALERT:
                    EnqueueMainThread(() =>
                    {
                        try
                        {
                            if (boxAlertUI != null)
                            {
                                using (MemoryStream ms = new MemoryStream(data))
                                using (BinaryReader reader = new BinaryReader(ms))
                                {
                                    int count = ReadInt32BigEndian(reader);
                                    if (count == 1)
                                    {
                                        int len = ReadInt32BigEndian(reader);
                                        byte[] msgBytes = reader.ReadBytes(len);
                                        string message = Encoding.UTF8.GetString(msgBytes);
                                        boxAlertUI.ShowAlert(message);
                                    }
                                    else noteAddItem.handleAlert(data);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            FreezeGame($"❌ Lỗi trong CMD_SEND_ALERT: {ex.Message}", ex);
                        }
                    });
                    break;

                case CMD_PLAYER_STATS:
                    SafeInvoke("UpdateLines", () => optionPlayer?.UpdateLines(data));
                    break;

                case CMD_SEND_NPC:
                    SafeInvoke("HandleMenu", () => menuManager?.HandleMenu(data));
                    break;

                case CMD_REQUEST_NPC:
                    SafeInvoke("HandleNpcList", () => playerHandler.HandleNpcList(data));
                    break;

                case CMD_SEND_UI:
                    SafeInvoke("HandleUI", () => UI_manager.HandleUI(data));
                    break;

                case CMD_SEND_MOBS:
                    SafeInvoke("handleSpawmMobs", () => MobsManager.handleSpawmMobs(data));
                    break;

                case CMD_SEND_QUANTITY_MOB:
                    SafeInvoke("HandleMobData", () => playerUI.HandleMobData(data));
                    break;

                case CMD_SHOP:
                    SafeInvoke("HandleBagData (shop)", () => shopUI?.HandleBagData(data));
                    break;

                case CMD_INFOR_UPGRADE:
                    SafeInvoke("HandleAddUpgrade", () => upgradeUI?.HandleAddUpgrade(data));
                    break;

                case CMD_SEND_ATTACK:
                    SafeInvoke("HandleAttack", () => playerHandler.HandleAttack(data));
                    break;

                case CMD_ALL_SKILL:
                    SafeInvoke("HandleSkillData", () => skillUI.HandleSkillData(data));
                    break;

                case CMD_SKILL:
                    SafeInvoke("HandleSkillEquip", () => skillUI.HandleSkillEquip(data));
                    break;

                case CMD_SKILL_COUNTDOWN:
                    SafeInvoke("handleSkill", () => skill.handleSkill(data));
                    break;
                case CMD_CHAT:
                    SafeInvoke("HandleChat", () => playerHandler.HandleChat(data));
                    break;
                default:
                    Debug.Log($"📥 Nhận command khác: 0x{cmd:X2} ({data.Length} bytes)");
                    break;
            }
        }
        catch (Exception ex)
        {
            FreezeGame($"🔥 Lỗi tổng thể trong HandleCommand (cmd=0x{cmd:X2})", ex);
        }
    }


    private int ReadInt32BigEndian(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    public void HandleBandDisconnect()
    {
        SocketManager.Instance.ResetConnection();
        isRunning = false;

        listenThread?.Interrupt();
        listenThread = null;

        Destroy(gameObject);
        SceneManager.LoadScene("Login");
    }
    private void SafeInvoke(string name, Action action)
    {
        try
        {
            EnqueueMainThread(() =>
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    FreezeGame($"❌ Lỗi trong {name}: {ex.Message}", ex);
                }
            });
        }
        catch (Exception ex)
        {
            FreezeGame($"❌ Lỗi khi EnqueueMainThread({name}): {ex.Message}", ex);
        }
    }

    private void FreezeGame(string message, Exception ex)
    {
        Debug.LogError($"{message}\n{ex}");
        Time.timeScale = 0f; // ⏸ Dừng toàn bộ game
        isRunning = false;

        // Nếu có UI cảnh báo, hiển thị lên
        if (boxAlertUI != null)
        {
            boxAlertUI.ShowAlert(message + "\n\n" + ex.Message);
        }

        // Nếu cần dừng cả thread lắng nghe
        listenThread?.Interrupt();
    }


}
