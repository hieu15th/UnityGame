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
        if (listenThread != null && listenThread.IsAlive)
        {
            listenThread.Abort(); // Hoặc dùng flag để dừng thread cũ nếu cần
        }

        isRunning = true;
        listenThread = new Thread(ListenToServer);
        listenThread.IsBackground = true;
        listenThread.Start();
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

    MemoryStream buffer = new MemoryStream();

    private void ListenToServer()
    {
        try
        {
            var reader = SocketManager.Instance.Reader;
            byte[] recvBuffer = new byte[2048];
            MemoryStream buffer = new MemoryStream();

            while (isRunning)
            {
                // Đọc dữ liệu mới từ socket
                int bytesRead = reader.BaseStream.Read(recvBuffer, 0, recvBuffer.Length);
                if (bytesRead <= 0)
                {
                    Debug.LogWarning("⚠️ Server ngắt kết nối hoặc socket bị đóng.");
                    break;
                }

                // Ghi dữ liệu mới vào buffer
                buffer.Write(recvBuffer, 0, bytesRead);

                // Phân tích buffer để xử lý các gói đầy đủ
                while (buffer.Length >= 3) // cần tối thiểu 3 byte: cmd + 2 byte size
                {
                    byte[] buf = buffer.ToArray();

                    sbyte cmd = (sbyte)buf[0];
                    ushort size = (ushort)((buf[1] << 8) | buf[2]);

                    // Nếu chưa đủ dữ liệu để xử lý gói này, chờ thêm
                    if (buffer.Length < 3 + size)
                        break;

                    // Trích dữ liệu của gói
                    byte[] data = new byte[size];
                    Array.Copy(buf, 3, data, 0, size);

                    // Cắt phần đã xử lý khỏi buffer
                    int remaining = (int)(buffer.Length - (3 + size));
                    buffer.SetLength(0);
                    if (remaining > 0)
                        buffer.Write(buf, 3 + size, remaining);

                    // Xử lý command
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
                    Debug.LogWarning("⚠️ Mất kết nối server: " + ex.Message);
                    SocketManager.Instance.ResetConnection();
                    isRunning = false;
                    listenThread?.Interrupt();
                    listenThread = null;
                    Destroy(gameObject);
                    SceneManager.LoadScene("Login");
                });
            }
            else
            {
                // Nếu là ban, chỉ tắt thread nhẹ nhàng
                isRunning = false;
                listenThread?.Interrupt();
                listenThread = null;
            }
        }
    }
    private void HandleCommand(sbyte cmd, byte[] data)
    {
        switch (cmd)
        {
            case CMD_REQUEST_PLAYER:
                EnqueueMainThread(() => playerHandler.HandleSpawnPlayer(data));
                break;

            case CMD_MOVE_ALL:
                EnqueueMainThread(() => playerHandler.HandleMoveAll(data));
                break;

            case CMD_MOVE:
                EnqueueMainThread(() => playerHandler.HandleMove(data));
                break;

            case CMD_DISCONNECT:
                EnqueueMainThread(() => playerHandler.HandleDisconnect(data));
                break;

            case CMD_BAND:
                isBanned = true;
                isRunning = false;
                EnqueueMainThread(() =>
                {
                    if (boxAlertUI != null)
                    {
                        SocketManager.Instance.ResetConnection();
                        listenThread?.Interrupt();
                        listenThread = null;
                        boxAlertUI.Band("Tài khoản được đăng nhập ở nơi khác");
                    }
                    else Debug.LogWarning("⚠️ boxAlertUI chưa được gán trong Main");
                });
                break;

            case CMD_GETBAG:
                EnqueueMainThread(() => bagUI?.HandleBagData(data));
                break;

            case CMD_ITEM_EQUIP:
                EnqueueMainThread(() => charUI?.HandleEquipData(data));
                break;

            case CMD_SEND_ALERT:
                EnqueueMainThread(() =>
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
                });
                break;

            case CMD_PLAYER_STATS:
                EnqueueMainThread(() => optionPlayer?.UpdateLines(data));
                break;

            case CMD_SEND_NPC:
                EnqueueMainThread(() => menuManager?.HandleMenu(data));
                break;

            case CMD_REQUEST_NPC:
                EnqueueMainThread(() => playerHandler.HandleNpcList(data));
                break;

            case CMD_SEND_UI:
                EnqueueMainThread(() => UI_manager.HandleUI(data));
                break;

            case CMD_SEND_MOBS:
                EnqueueMainThread(() => MobsManager.handleSpawmMobs(data));
                break;

            case CMD_SEND_QUANTITY_MOB:
                EnqueueMainThread(() => playerUI.HandleMobData(data));
                break;

            case CMD_SHOP:
                EnqueueMainThread(() => shopUI?.HandleBagData(data));
                break;

            case CMD_INFOR_UPGRADE:
                EnqueueMainThread(() => upgradeUI?.HandleAddUpgrade(data));
                break;

            case CMD_SEND_ATTACK:
                EnqueueMainThread(() => playerHandler.HandleAttack(data));
                break;

            case CMD_ALL_SKILL:
                EnqueueMainThread(() => skillUI.HandleSkillData(data));
                break;

            case CMD_SKILL:
                EnqueueMainThread(() => skillUI.HandleSkillEquip(data));
                break;

            case CMD_SKILL_COUNTDOWN:
                EnqueueMainThread(() => skill.handleSkill(data));
                break;

            default:
                Debug.Log($"📥 Nhận command khác: 0x{cmd:X2} ({data.Length} bytes)");
                break;
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

}
