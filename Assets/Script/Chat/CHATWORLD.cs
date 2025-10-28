using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CHATWORLD : MonoBehaviour
{
    public TextMeshProUGUI text;        // Text hiển thị
    public RectTransform chatPanel;     // Panel (vùng hiển thị)
    public float speed = 100f;          // Tốc độ di chuyển (pixel/giây)
    public Color activeColor = new Color(0.6f, 0.4f, 0.2f, 0.5f); // Nâu nhạt
    public Color idleColor = new Color(0, 0, 0, 0);               // Trong suốt

    private Queue<string> msgQueue = new Queue<string>();
    private bool isDisplaying = false;
    private Image panelImage;

    void Awake()
    {
        panelImage = chatPanel.GetComponent<Image>();
        if (panelImage == null)
            panelImage = chatPanel.gameObject.AddComponent<Image>();

        panelImage.color = idleColor;

        // ❗ Đảm bảo text luôn hiển thị trên 1 dòng
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
    }

    void Update()
    {
        if (!isDisplaying && msgQueue.Count > 0)
        {
            string nextMsg = msgQueue.Dequeue();
            StartCoroutine(DisplayMessage(nextMsg));
        }
    }

    public void HandleChatWork(string mess)
    {
        if (!string.IsNullOrWhiteSpace(mess))
        {
            msgQueue.Enqueue(mess);
        }
    }

    private IEnumerator DisplayMessage(string message)
    {
        isDisplaying = true;
        text.text = message;
        panelImage.color = activeColor;

        RectTransform textRect = text.rectTransform;

        // Chờ layout cập nhật 1 frame
        yield return null;

        // 🔹 Lấy chiều rộng thật của text và tự điều chỉnh kích thước RectTransform
        float textWidth = text.preferredWidth;
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth);

        // ⚙️ Đặt text nằm ngoài bên phải panel
        textRect.anchoredPosition = new Vector2(chatPanel.rect.width + 20f, 0);

        // 🌀 Di chuyển chữ từ phải sang trái cho đến khi biến mất
        while (textRect.anchoredPosition.x > -textWidth - chatPanel.rect.width)
        {
            textRect.anchoredPosition -= Vector2.right * speed * Time.deltaTime;
            yield return null;
        }

        // Khi xong → reset
        panelImage.color = idleColor;
        text.text = "";
        isDisplaying = false;
    }
}
