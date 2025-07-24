using System;
using System.Collections;
using System.Collections.Concurrent; // For ConcurrentQueue
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Unity.Burst.Intrinsics.X86.Avx;


public class LoginRegisterScript : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isLoggingIn = false;

    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public Button registerButton;
    [SerializeField] private List<TMP_InputField> inputFields;

    private const string REGISTER_URL = "http://sssss"; // Replace with actual URL
    private const byte CMD_LOGIN = 0x81;
    private const byte CMD_FULL_SIZE = 0xE0;
    private const byte NOT_LOGIN = 0xE3;

    [SerializeField] private GameObject loading;
    private Thread connectionThread;

    [SerializeField] private ScrollingText scrollingText;

    private readonly ConcurrentQueue<Action> actionQueue = new ConcurrentQueue<Action>();

    void Awake()
    {
        if (loading != null)
        {
            loading.SetActive(false);
        }
    }

    void Start()
    {
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        registerButton.onClick.AddListener(OnRegisterButtonClicked);

        string savedUsername = PlayerPrefs.GetString("SavedUsername", "");
        string savedPassword = PlayerPrefs.GetString("SavedPassword", "");

        usernameInput.text = savedUsername;
        passwordInput.text = savedPassword;
    }
    private void HandleTabNavigation()
    {
        if (!Input.GetKeyDown(KeyCode.Tab)) return;

        var current = EventSystem.current.currentSelectedGameObject;
        if (current == null) return;

        var currentInput = current.GetComponent<TMP_InputField>();
        if (currentInput == null) return;

        int currentIndex = inputFields.IndexOf(currentInput);
        if (currentIndex == -1) return;

        int nextIndex = (currentIndex + 1) % inputFields.Count;

        TMP_InputField nextInput = inputFields[nextIndex];
        EventSystem.current.SetSelectedGameObject(nextInput.gameObject);
        nextInput.ActivateInputField();
    }

    void Update()
    {
        while (actionQueue.TryDequeue(out Action action))
        {
            action?.Invoke();
        }
        HandleTabNavigation();

    }

    void OnLoginButtonClicked()
    {
        if (isLoggingIn) return;
        isLoggingIn = true;
        if (scrollingText != null)
        {
            scrollingText.ClearText();
        }
        if (loading != null)
        {
            loading.SetActive(true);
        }

        string username = usernameInput.text?.Trim();
        string password = passwordInput.text?.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            UpdateStatus("Tên đăng nhập hoặc mật khẩu trống!");
            if (loading != null) loading.SetActive(false);
            return;
        }
        if (username.Length < 3 || password.Length < 6)
        {
            UpdateStatus("Tên đăng nhập và mật khẩu tối thiếu 6 kí tự");
            if (loading != null) loading.SetActive(false);
            return;
        }
        if (username.Length > 10 || password.Length > 10)
        {
            UpdateStatus("Tên đăng nhập hoặc mật khẩu tối đa 10 kí tự");
            if (loading != null) loading.SetActive(false);
            return;
        }

        connectionThread = new Thread(() => ConnectToServer(username, password));
        connectionThread.IsBackground = true;
        connectionThread.Start();
    }

    void ConnectToServer(string username, string password)
    {
        try
        {
            string serverIp = "localhost";
            int serverPort = 14444;

            if (SocketManager.Instance == null)
            {
                Debug.LogError("SocketManager.Instance is null");
                Enqueue(() => UpdateStatus("Lỗi nội bộ: Socket chưa được khởi tạo."));
                return;
            }

            if (!SocketManager.Instance.IsConnected())
            {
                if (!SocketManager.Instance.Connect(serverIp, serverPort))
                {
                    Enqueue(() => UpdateStatus("Máy chủ đang bảo trì!"));
                    return;
                }
            }

            client = SocketManager.Instance.Client;
            stream = SocketManager.Instance.Stream;

            if (client == null || stream == null)
            {
                Debug.LogError("Client hoặc stream là null sau khi kết nối.");
                Enqueue(() => UpdateStatus("Không thể thiết lập kết nối tới server"));
                return;
            }

            byte[] usernameBytes = Encoding.UTF8.GetBytes(username);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] versionBytes = Encoding.UTF8.GetBytes("abxch123kasd_1");

            int dataSize = usernameBytes.Length + 1 + passwordBytes.Length + 1 + versionBytes.Length;
            if (dataSize > 4096)
            {
                Enqueue(() => UpdateStatus("Dữ liệu đăng nhập quá lớn!"));
                return;
            }

            byte[] buffer = new byte[1 + 2 + dataSize]; 
            int offset = 0;
            buffer[offset++] = CMD_LOGIN;
            buffer[offset++] = (byte)(dataSize >> 8);
            buffer[offset++] = (byte)(dataSize & 0xFF);

            Array.Copy(usernameBytes, 0, buffer, offset, usernameBytes.Length);
            offset += usernameBytes.Length;
            buffer[offset++] = 0;

            Array.Copy(passwordBytes, 0, buffer, offset, passwordBytes.Length);
            offset += passwordBytes.Length;
            buffer[offset++] = 0;

            Array.Copy(versionBytes, 0, buffer, offset, versionBytes.Length);
            offset += versionBytes.Length;

            stream.Write(buffer, 0, buffer.Length);

            int bytesRead = stream.Read(buffer, 0, 1);
            if (bytesRead != 1)
            {
                Enqueue(() => UpdateStatus("Không nhận được phản hồi từ máy chủ"));
                return;
            }

            byte response = buffer[0];
            switch (response)
            {
                case 0x01: // Thành công
                    Enqueue(() =>
                    {
                        PlayerPrefs.SetString("SavedUsername", username);
                        PlayerPrefs.SetString("SavedPassword", password);
                        PlayerPrefs.Save();
                        StartCoroutine(GoToMainScene());
                    });
                    break;

                case 0xE3: // Sai tài khoản/mật khẩu
                    Enqueue(() => UpdateStatus("Tài khoản hoặc mật khẩu không chính xác!"));
                    client.Close();
                    break;

                case 0xE5: // Server gửi message có length kèm
                    {
                        int lenHi = stream.ReadByte();
                        int lenLo = stream.ReadByte();
                        if (lenHi == -1 || lenLo == -1) break;

                        int msgLen = (lenHi << 8) | lenLo;
                        byte[] msgBuffer = new byte[msgLen];
                        int read = stream.Read(msgBuffer, 0, msgLen);
                        if (read < msgLen) break;

                        string msg = Encoding.UTF8.GetString(msgBuffer);
                        Enqueue(() => UpdateStatus(msg));
                        client.Close();
                        break;
                    }

                case 0x86: // Bị cấm hoặc login nơi khác
                    {
                        int lenHi = stream.ReadByte();
                        int lenLo = stream.ReadByte();
                        if (lenHi == -1 || lenLo == -1) break;

                        int msgLen = (lenHi << 8) | lenLo;
                        byte[] msgBuffer = new byte[msgLen];
                        int read = stream.Read(msgBuffer, 0, msgLen);
                        if (read < msgLen) break;

                        string msg = Encoding.UTF8.GetString(msgBuffer);
                        Enqueue(() =>
                        {
                            Debug.LogWarning("🚫 Tài khoản bị cấm hoặc đăng nhập nơi khác: " + msg);
                            UpdateStatus(msg);
                            client.Close();
                        });
                        break;
                    }

                default:
                    Enqueue(() => UpdateStatus($"Phản hồi không xác định: 0x{response:X2}"));
                    client.Close();
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Lỗi khi kết nối server: {ex.Message}");
            Enqueue(() => UpdateStatus($"Lỗi: {ex.Message}"));
        }
        finally
        {
            Enqueue(() =>
            {
                if (loading != null)
                {
                    loading.SetActive(false);
                    isLoggingIn = false;
                }
            });
        }
    }



    private IEnumerator GoToMainScene()
    {
        UpdateStatus("Đăng nhập thành công! Đang chuyển...");
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene("Main");
    }

    void OnRegisterButtonClicked()
    {
        Application.OpenURL(REGISTER_URL);
    }

    void OnDestroy()
    {
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Interrupt();
        }

    }



    public void UpdateStatus(string message)
    {
        if (scrollingText != null)
        {
            scrollingText.UpdateText(message);
        }
        else
        {
            Debug.LogWarning("ScrollingText chưa được gán. Không thể cập nhật trạng thái.");
        }
        Debug.Log($"Trạng thái: {message} tại {DateTime.Now}");
    }

    private void Enqueue(Action action)
    {
        actionQueue.Enqueue(action);
    }
}