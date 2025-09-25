using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Main : MonoBehaviour
{
    private Thread listenThread;
    private bool isRunning = false;

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
    private const sbyte CMD_SEND_QUANTITY_MOB = -110;
    private const sbyte CMD_SHOP = -109;
    private const sbyte CMD_INFOR_UPGRADE = -108;

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
   

    private void ListenToServer()
    {
        try
        {
            var reader = SocketManager.Instance.Reader;
            while (isRunning)
            {
                sbyte cmd = (sbyte)reader.ReadByte();
                ushort size = (ushort)((reader.ReadByte() << 8) | reader.ReadByte());
                byte[] data = reader.ReadBytes(size);
                Debug.Log($"Nhận message: {cmd}");
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
                        EnqueueMainThread(() =>
                        {
                            SocketManager.Instance.ResetConnection();
                            isRunning = false;
                            listenThread?.Interrupt();
                            listenThread = null;
                            Destroy(gameObject); // Hủy Main nếu là DontDestroyOnLoad
                            SceneManager.LoadScene("Login");

                        });
                        break;
                    case CMD_GETBAG:
                        EnqueueMainThread(() =>
                        {
                            if (bagUI != null)
                                bagUI.HandleBagData(data);
                            else
                                Debug.LogWarning("⚠️ BagUI chưa được gán trong Main");
                        });
                        break;
                    case CMD_ITEM_EQUIP:
                        EnqueueMainThread(() =>
                        {
                            if (charUI != null)
                                charUI.HandleEquipData(data);
                            else
                                Debug.LogWarning("⚠️ CharUI chưa được gán trong Main");
                        });
                        break;
                    case CMD_SEND_ALERT:
                        EnqueueMainThread(() =>
                        {
                            if (boxAlertUI != null)
                            {
                                using (MemoryStream ms = new MemoryStream(data))
                                using (BinaryReader reader = new BinaryReader(ms))
                                {
                                    int count = ReadInt32BigEndian(reader); // Số chuỗi, mặc định là 1
                                    if (count == 1)
                                    {
                                        int len = ReadInt32BigEndian(reader); // độ dài chuỗi
                                        byte[] msgBytes = reader.ReadBytes(len); // nội dung chuỗi
                                        string message = Encoding.UTF8.GetString(msgBytes);

                                        boxAlertUI.ShowAlert(message);
                                    }
                                    else
                                    {
                                        noteAddItem.handleAlert(data);
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogWarning("⚠️ boxAlertUI chưa được gán trong Main");
                            }
                        });
                        break;
                    case CMD_PLAYER_STATS:
                        EnqueueMainThread(() =>
                        {
                            if (optionPlayer != null)
                                optionPlayer.UpdateLines(data);
                            else
                                Debug.LogWarning("⚠️ CharUI chưa được gán trong Main");
                        });
                        break;
                    case CMD_SEND_NPC:
                        EnqueueMainThread(() =>
                        {
                            if (menuManager != null)
                                menuManager.HandleMenu(data);
                            else
                                Debug.LogWarning("⚠️ MenuManager chưa được gán trong Main");
                        });
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
                        EnqueueMainThread(() =>
                        {
                            if (shopUI != null)
                                shopUI.HandleBagData(data);
                            else
                                Debug.LogWarning("⚠️ shopUI chưa được gán trong Main");
                        });
                        break;
                    case CMD_INFOR_UPGRADE:
                        EnqueueMainThread(() =>
                        {
                            if (upgradeUI != null)
                                upgradeUI.HandleAddUpgrade(data);
                            else
                                Debug.LogWarning("⚠️ upgradeUI chưa được gán trong Main");
                        });
                        break;
                    default:
                        Debug.Log("📩 Nhận command khác: " + cmd);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("🔌 Mất kết nối server: " + ex.Message);
            EnqueueMainThread(() =>
            {
                SocketManager.Instance.ResetConnection();
                isRunning = false;
                listenThread?.Interrupt();
                listenThread = null;
                Destroy(gameObject); // Hủy Main nếu là DontDestroyOnLoad
                SceneManager.LoadScene("Login");

            });
        }
    }
    private int ReadInt32BigEndian(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }
}
