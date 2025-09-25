using UnityEngine;
using TMPro;

[ExecuteAlways]
public class ScrollingText : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text alert;                 // TMP_Text của Alert
    [SerializeField] private RectTransform maskContainer;    // Viewport có RectMask2D

    [Header("Scroll")]
    [SerializeField] private float speed = 60f;              // px/giây (sang trái)
    [SerializeField] private float gapPixels = 60f;          // khoảng trống giữa 2 bản sao
    [SerializeField] private bool useUnscaledTime = false;   // true nếu muốn bỏ qua TimeScale

    private RectTransform alertRT;
    private string originalText = "";
    private float textWidth;          // width của text gốc
    private float containerWidth;     // width của viewport
    private float cycleLength;        // textWidth + gapPixels
    private float scrollX;            // tích lũy dịch chuyển
    private bool scrolling;
    public GameObject box_alert;

    void Reset()
    {
        alert = GetComponent<TMP_Text>();
    }

    void OnEnable() { Init(); }
    void Start() { Init(); }

    void OnRectTransformDimensionsChange()
    {
        // Khi container đổi kích thước (xoay màn hình, scale UI...) -> tính lại
        if (enabled && gameObject.activeInHierarchy) Refresh();
    }

    void Update()
    {
        if (!scrolling || alertRT == null) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        scrollX += speed * dt;

        // Dịch sang trái: x giảm. Dùng modulo để lặp mượt.
        float offset = scrollX % cycleLength;    // 0..cycleLength
        alertRT.anchoredPosition = new Vector2(-offset, alertRT.anchoredPosition.y);
    }

    // ===== public API =====
    public void SetText(string txt)
    {
        string newText = string.IsNullOrWhiteSpace(txt) ? "" : txt.Trim();

        // ⬇️ Nếu rỗng: xóa nội dung cũ và dừng cuộn
        if (newText.Length == 0)
        {
            originalText = "";
            scrollX = 0f;
            scrolling = false;

            if (alert == null) alert = GetComponent<TMP_Text>();
            if (alert != null)
            {
                alert.text = ""; // xoá text cũ đang hiển thị
                if (alertRT == null) alertRT = alert.rectTransform;
                var rt = alertRT;
                rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y); // reset về đầu
            }

            if (box_alert) box_alert.SetActive(false); // ẩn khung alert nếu có
            return; // không cần Init/Refresh nữa
        }

        // ⬇️ Có nội dung: hiển thị và tính lại cuộn
        originalText = newText;
        scrollX = 0f;
        if (box_alert && !box_alert.activeSelf) box_alert.SetActive(true);
        Refresh(); // hoặc Init() nếu bạn muốn set lại anchor/pivot
    }


    // ===== internal =====
    void Init()
    {
        if (alert == null)
        {
            alert = GetComponent<TMP_Text>();
            if (alert == null) { Debug.LogError("AlertMarquee: cần TMP_Text"); enabled = false; return; }
        }

        if (maskContainer == null)
        {
            // mặc định lấy parent làm container
            maskContainer = alert.rectTransform.parent as RectTransform;
        }

        alertRT = alert.rectTransform;

        // Cấu hình để trượt mượt + bị cắt bởi mask
        alert.enableWordWrapping = false;
        alert.overflowMode = TextOverflowModes.Overflow;

        // Anchor/pivot trái-giữa để trượt theo trục X
        alertRT.anchorMin = new Vector2(0f, 0.5f);
        alertRT.anchorMax = new Vector2(0f, 0.5f);
        alertRT.pivot = new Vector2(0f, 0.5f);

        if (string.IsNullOrEmpty(originalText)) originalText = alert.text;

        Refresh();
    }

    void Refresh()
    {
        if (alert == null || alertRT == null) return;

        containerWidth = maskContainer ? maskContainer.rect.width : alertRT.rect.width;

        // Tính width của text gốc
        alert.text = originalText;
        alert.ForceMeshUpdate();
        textWidth = alert.preferredWidth;

        if (textWidth <= containerWidth || string.IsNullOrEmpty(originalText))
        {
            // Không cần cuộn
            scrolling = false;
            scrollX = 0f;
            alertRT.anchoredPosition = new Vector2(0f, alertRT.anchoredPosition.y);
            alert.text = originalText;
            return;
        }

        // Cần cuộn → nhân đôi nội dung + gap để lặp mượt
        string spacer = $"<space={gapPixels}px>";
        string doubled = originalText + spacer + originalText;

        alert.text = doubled;
        alert.ForceMeshUpdate();

        cycleLength = textWidth + gapPixels;   // độ dài 1 vòng theo text gốc
        scrollX = 0f;
        alertRT.anchoredPosition = new Vector2(0f, alertRT.anchoredPosition.y);
        scrolling = true;
    }
}
