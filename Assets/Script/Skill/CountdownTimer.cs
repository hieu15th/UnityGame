using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CountdownTimer : MonoBehaviour
{
    [Header("UI References")]
    public Image countdownImage;          // Ảnh fill countdown
    public TextMeshProUGUI cd;            // Text hiển thị thời gian

    private float duration;               // Tổng thời gian (giây)
    private float timeLeft;               // Thời gian còn lại
    private bool isCounting;              // Đang đếm hay không

    // --- Ẩn mọi thứ khi bắt đầu ---
    void Awake()
    {
        if (countdownImage != null)
            countdownImage.gameObject.SetActive(false);

        if (cd != null)
            cd.gameObject.SetActive(false);

        isCounting = false;
    }

    void Update()
    {
        if (!isCounting)
            return;

        timeLeft -= Time.deltaTime;

        if (countdownImage != null)
            countdownImage.fillAmount = Mathf.Clamp01(timeLeft / duration);

        // 🕒 Hiển thị thời gian thập phân (1 chữ số sau dấu .)
        if (cd != null)
            cd.text = timeLeft.ToString("F1"); // ví dụ: 2.3, 1.7, 0.5

        if (timeLeft <= 0)
        {
            StopCountdown();
        }
    }

    // --- Bắt đầu đếm ---
    public void StartCountdown()
    {
        var s = GetComponent<SkillCD>();
        if (s == null)
        {
            Debug.LogWarning("⚠️ CountdownTimer không tìm thấy SkillCD trên cùng GameObject.");
            return;
        }

        duration = s.info.cd / 1000f;
        timeLeft = s.info.cd / 1000f;

        if (countdownImage != null)
        {
            countdownImage.fillAmount = 1f;
            countdownImage.gameObject.SetActive(true);
        }

        if (cd != null)
        {
            cd.text = duration.ToString("F1");
            cd.gameObject.SetActive(true);
        }

        isCounting = true;
    }

    // --- Dừng và ẩn ---
    public void StopCountdown()
    {
        isCounting = false;

        if (countdownImage != null)
            countdownImage.gameObject.SetActive(false);

        if (cd != null)
            cd.gameObject.SetActive(false);
    }

    public bool IsCounting() => isCounting;
}
