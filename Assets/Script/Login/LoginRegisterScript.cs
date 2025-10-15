using System;
using System.Collections;
using System.Collections.Concurrent;
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

    private const byte CMD_LOGIN = 0x81;
    private const byte CMD_REGISTER = 0x82;  // thêm cmd đăng ký
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

        usernameInput.onValueChanged.AddListener(FilterInput);
        passwordInput.onValueChanged.AddListener(FilterInput);
    }

    private void FilterInput(string value)
    {
        TMP_InputField currentInput = EventSystem.current.currentSelectedGameObject?.GetComponent<TMP_InputField>();
        if (currentInput == null) return;

        string filtered = System.Text.RegularExpressions.Regex.Replace(value, "[^a-zA-Z0-9]", "");
        if (filtered != value)
        {
            currentInput.text = filtered;
            currentInput.caretPosition = filtered.Length;
        }
    }

    void Start()
    {
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        registerButton.onClick.AddListener(OnRegisterButtonClicked);
        passwordInput.contentType = TMP_InputField.ContentType.Password;
        passwordInput.asteriskChar = '*';
        passwordInput.ForceLabelUpdate();

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

        if (scrollingText != null) scrollingText.SetText("");
        if (loading != null) loading.SetActive(true);

        string username = usernameInput.text?.Trim();
        string password = passwordInput.text?.Trim();

        if (!ValidateInput(username, password)) return;

        connectionThread = new Thread(() => ConnectToServer(username, password, false));
        connectionThread.IsBackground = true;
        connectionThread.Start();
    }

    void OnRegisterButtonClicked()
    {
        if (isLoggingIn) return;
        isLoggingIn = true;

        if (scrollingText != null) scrollingText.SetText("");
        if (loading != null) loading.SetActive(true);

        string username = usernameInput.text?.Trim();
        string password = passwordInput.text?.Trim();

        if (!ValidateInput(username, password)) return;

        connectionThread = new Thread(() => ConnectToServer(username, password, true));
        connectionThread.IsBackground = true;
        connectionThread.Start();
    }

    private bool ValidateInput(string username, string password)
    {
        void FailEarly(string msg)
        {
            Enqueue(() => UpdateStatus(msg));
            if (loading != null) loading.SetActive(false);
            isLoggingIn = false;
        }

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            FailEarly("Tên đăng nhập hoặc mật khẩu trống!");
            return false;
        }
        if (username.Length < 3 || password.Length < 6)
        {
            FailEarly("Tên đăng nhập tối thiểu 3 ký tự, mật khẩu tối thiểu 6 ký tự.");
            return false;
        }
        if (username.Length > 10 || password.Length > 10)
        {
            FailEarly("Tên đăng nhập hoặc mật khẩu tối đa 10 ký tự.");
            return false;
        }
        return true;
    }

    void ConnectToServer(string username, string password, bool isRegister)
    {
        try
        {
            string serverIp = "127.0.0.1";
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

            stream.ReadTimeout = 10000;
            stream.WriteTimeout = 10000;

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
            buffer[offset++] = (byte)(isRegister ? CMD_REGISTER : CMD_LOGIN); // khác biệt chính
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

            int bytesRead;
            try
            {
                bytesRead = stream.Read(buffer, 0, 1);
            }
            catch (IOException)
            {
                Enqueue(() => UpdateStatus("⏰ Hết thời gian chờ phản hồi từ server (10s)."));
                client.Close();
                return;
            }

            if (bytesRead != 1)
            {
                Enqueue(() => UpdateStatus("Không nhận được phản hồi từ máy chủ"));
                return;
            }

            byte response = buffer[0];
            switch (response)
            {
                case 0x01: // Thành công (login hoặc register)
                    Enqueue(() =>
                    {
                        PlayerPrefs.SetString("SavedUsername", username);
                        PlayerPrefs.SetString("SavedPassword", password);
                        PlayerPrefs.Save();
                        // Auto-login: vào luôn Main
                        SceneManager.LoadScene("Main");
                    });
                    break;

                case 0xE3:
                    Enqueue(() => UpdateStatus(isRegister ?
                        "Tên tài khoản đã tồn tại!" :
                        "Tài khoản hoặc mật khẩu không chính xác!"));
                    client.Close();
                    break;

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
            scrollingText.SetText(message);
        }
        else
        {
            Debug.LogWarning("ScrollingText chưa được gán. Không thể cập nhật trạng thái.");
        }
    }

    private void Enqueue(Action action)
    {
        actionQueue.Enqueue(action);
    }
}
