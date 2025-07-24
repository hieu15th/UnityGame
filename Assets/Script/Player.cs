using UnityEngine;

public class Player : MonoBehaviour
{
    private int _hpNow;
    private int _hpMax;
    private int _gold;
    private int _diamond;

    public int hp_now
    {
        get => _hpNow;
        set => _hpNow = Mathf.Clamp(value, 0, _hpMax); // Không cho vượt quá max hoặc âm
    }

    public int hp_max
    {
        get => _hpMax;
        set
        {
            _hpMax = Mathf.Max(1, value);              // Đảm bảo hp_max luôn > 0
            _hpNow = Mathf.Clamp(_hpNow, 0, _hpMax);   // Giới hạn lại hp_now nếu cần
        }
    }

    public int gold
    {
        get => _gold;
        set => _gold = Mathf.Max(0, value);            // Không cho âm vàng
    }

    public int diamond
    {
        get => _diamond;
        set => _diamond = Mathf.Max(0, value);         // Không cho âm kim cương
    }
}
