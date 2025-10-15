using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public bool isMuted;

    void Awake()
    {
        // Đảm bảo chỉ có 1 AudioManager tồn tại xuyên suốt game
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Đọc trạng thái âm thanh đã lưu
        isMuted = PlayerPrefs.GetInt("Muted", 0) == 1;
        ApplyVolume();
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        PlayerPrefs.SetInt("Muted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyVolume();
    }

    public void ApplyVolume()
    {
        AudioListener.volume = isMuted ? 0f : 1f;
    }
}
