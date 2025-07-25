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

    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
    [SerializeField] private PlayerController playerHandler;
    [SerializeField] private BagLayoutAdjuster bagUI;
    [SerializeField] private EquipLayoutAdjuster charUI;
    [SerializeField] private BoxAlertUI boxAlertUI;
    [SerializeField] private OptionPlayer optionPlayer;


    void Start()
    {
        SendPlayerRequest();
        SendNpcRequest();
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
    private void SendNpcRequest()
    {
        try
        {
            var writer = SocketManager.Instance.Writer;
            if (writer == null)
            {
                Debug.LogError("Writer chưa khởi tạo.");
                return;
            }

            writer.Write(CMD_REQUEST_NPC);
            writer.Write((ushort)0);
            writer.Flush();
        }
        catch (Exception ex)
        {
            Debug.LogError("Lỗi khi gửi CMD -115: " + ex.Message);
        }
    }
    private void SendPlayerRequest()
    {
        try
        {
            var writer = SocketManager.Instance.Writer;
            if (writer == null)
            {
                Debug.LogError("Writer chưa khởi tạo.");
                return;
            }

            writer.Write(CMD_REQUEST_PLAYER);
            writer.Write((ushort)0);
            writer.Flush();
        }
        catch (Exception ex)
        {
            Debug.LogError("Lỗi khi gửi CMD -127: " + ex.Message);
        }
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
                                    if (count > 0)
                                    {
                                        int len = ReadInt32BigEndian(reader); // độ dài chuỗi
                                        byte[] msgBytes = reader.ReadBytes(len); // nội dung chuỗi
                                        string message = Encoding.UTF8.GetString(msgBytes);

                                        boxAlertUI.ShowAlert(message);
                                    }
                                    else
                                    {
                                        Debug.LogWarning("❌ Không có chuỗi nào trong dữ liệu alert.");
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
                    case CMD_REQUEST_NPC:
                        EnqueueMainThread(() => playerHandler.HandleNpcList(data));
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
