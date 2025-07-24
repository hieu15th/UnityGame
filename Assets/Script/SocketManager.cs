using System;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

public class SocketManager : MonoBehaviour
{
    public static SocketManager Instance { get; private set; }

    public TcpClient Client { get; private set; }
    public NetworkStream Stream { get; private set; }
    public BinaryReader Reader { get; private set; }
    public BinaryWriter Writer { get; private set; }
    public string Username { get; set; } // 👈 THÊM DÒNG NÀY

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 👈 Cái này là quan trọng nhất
            Debug.Log("🟢 SocketManager giữ lại giữa các scene.");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool Connect(string ip, int port)
    {
        try
        {
            Client = new TcpClient();
            Client.Connect(ip, port);
            Stream = Client.GetStream();

            if (Stream == null)
            {
                Debug.LogError("Không lấy được NetworkStream");
                return false;
            }

            Reader = new BinaryReader(Stream);
            Writer = new BinaryWriter(Stream);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("Lỗi khi connect: " + ex.Message);
            Client = null;
            Stream = null;
            Reader = null;
            Writer = null;
            return false;
        }
    }

    public bool IsConnected()
    {
        return Client != null && Client.Connected && Stream != null;
    }
    public void ResetConnection()
    {
        Close();
        Client = null;
        Stream = null;
        Reader = null;
        Writer = null;
    }
    public void Close()
    {
        Reader?.Close();
        Writer?.Close();
        Stream?.Close();
        Client?.Close();
    }

}
