using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    public TextMeshProUGUI posText;
    public TextMeshProUGUI hp;
    public TextMeshProUGUI gold;
    public TextMeshProUGUI diamond;
    public TextMeshProUGUI mob_2;
    public TextMeshProUGUI mob_3;
    public TextMeshProUGUI mob_4;

    private long quantity2, quantity3, quantity4;
    public float scalePos = 5f;
    public PlayerController p;

    // ===== Animation cho gold/diamond =====
    private long displayGold, targetGold;
    private long displayDiamond, targetDiamond;
    private float goldSpeed, diamondSpeed;

    [SerializeField] private float minDuration = 0.5f;   // nhanh nhất 0.5s
    [SerializeField] private float maxDuration = 3f;     // chậm nhất 3s
    [SerializeField] private float unitPerSecond = 500f; // 500 đơn vị = 1s

    private readonly CultureInfo culture;

    public PlayerUI()
    {
        // Dùng dấu phân cách ngàn là ","
        culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberGroupSeparator = ",";
    }

    private static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static string FormatMMSS(long ms)
    {
        if (ms < 0) ms = 0;
        long totalSec = ms / 1000;
        long mm = totalSec / 60;
        long ss = totalSec % 60;
        return $"{mm:00}:{ss:00}";
    }

    private static long NormalizeToRemainingMs(long value)
    {
        long now = NowMs();
        const long sevenDaysMs = 7L * 24 * 60 * 60 * 1000;
        if (value > now - sevenDaysMs && value < now + sevenDaysMs)
        {
            long remain = value - now;
            return remain > 0 ? remain : 0;
        }
        return value < 0 ? 0 : value;
    }

    // ===== Helper format số =====
    private static string FormatAbbrev(long value)
    {
        if (value >= 1_000_000_000)
            return (value / 1_000_000_000f).ToString("0.##") + "B"; // Billion
        if (value >= 1_000_000)
            return (value / 1_000_000f).ToString("0.##") + "M";     // Million
        return value.ToString();
    }

    private string FormatFull(long value)
    {
        return value.ToString("N0", culture); // 1,234,567
    }

    void Start()
    {
        displayGold = targetGold = 0;
        displayDiamond = targetDiamond = 0;
    }

    void Update()
    {
        if (p?.CurrentPlayer == null) return;

        Player pl = p.CurrentPlayer.GetComponent<Player>();
        if (pl == null) return;

        // ===== GOLD =====
        if (pl.gold != targetGold)
        {
            targetGold = pl.gold;
            long diff = Math.Abs(targetGold - displayGold);
            float duration = Mathf.Clamp(diff / unitPerSecond, minDuration, maxDuration);
            goldSpeed = diff / duration;
        }

        if (displayGold != targetGold)
        {
            float step = goldSpeed * Time.deltaTime;
            if (Mathf.Abs(targetGold - displayGold) <= step)
                displayGold = targetGold;
            else
                displayGold += (targetGold > displayGold ? (long)step : -(long)step);
        }

        if (gold != null)
        {
            string abbrev = FormatAbbrev(displayGold);
            string full = FormatFull(displayGold);
            gold.SetText($"{abbrev}");
        }

        // ===== DIAMOND =====
        if (pl.diamond != targetDiamond)
        {
            targetDiamond = pl.diamond;
            long diff = Math.Abs(targetDiamond - displayDiamond);
            float duration = Mathf.Clamp(diff / unitPerSecond, minDuration, maxDuration);
            diamondSpeed = diff / duration;
        }

        if (displayDiamond != targetDiamond)
        {
            float step = diamondSpeed * Time.deltaTime;
            if (Mathf.Abs(targetDiamond - displayDiamond) <= step)
                displayDiamond = targetDiamond;
            else
                displayDiamond += (targetDiamond > displayDiamond ? (long)step : -(long)step);
        }

        if (diamond != null)
        {
            string abbrev = FormatAbbrev(displayDiamond);
            string full = FormatFull(displayDiamond);
            diamond.SetText($"{abbrev}");
        }

        // ===== POS =====
        Vector3 pos = p.CurrentPlayer.transform.position * scalePos;
        if (posText != null)
            posText.SetText($"{(int)pos.x} , {(int)pos.y}");

        // ===== HP =====
        string FormatHP(int value) => value >= 1000
            ? $"{(value / 1000f):F1}" + "k"
            : value.ToString();

        if (hp != null)
            hp.SetText($"{FormatHP(pl.hp_now)}/{FormatHP(pl.hp_max)}");

        // ===== MOB countdowns =====
        if (mob_2 != null)
            mob_2.SetText(quantity2 > 100 ? FormatMMSS(NormalizeToRemainingMs(quantity2)) : ((int)quantity2).ToString());

        if (mob_3 != null)
            mob_3.SetText(quantity3 > 100 ? FormatMMSS(NormalizeToRemainingMs(quantity3)) : ((int)quantity3).ToString());

        if (mob_4 != null)
            mob_4.SetText(quantity4 > 100 ? FormatMMSS(NormalizeToRemainingMs(quantity4)) : ((int)quantity4).ToString());
    }

    // ===== đọc gói mob data =====
    private static bool TryReadInt32LE(byte[] src, int offset, out int value)
    {
        value = 0;
        if (src == null || offset < 0 || offset + 4 > src.Length) return false;
        value = (src[offset] & 0xFF)
              | ((src[offset + 1] & 0xFF) << 8)
              | ((src[offset + 2] & 0xFF) << 16)
              | ((src[offset + 3] & 0xFF) << 24);
        return true;
    }

    private static bool TryReadInt64LE(byte[] src, int offset, out long value)
    {
        value = 0L;
        if (src == null || offset < 0 || offset + 8 > src.Length) return false;

        long b0 = (long)(src[offset] & 0xFF);
        long b1 = (long)(src[offset + 1] & 0xFF) << 8;
        long b2 = (long)(src[offset + 2] & 0xFF) << 16;
        long b3 = (long)(src[offset + 3] & 0xFF) << 24;
        long b4 = (long)(src[offset + 4] & 0xFF) << 32;
        long b5 = (long)(src[offset + 5] & 0xFF) << 40;
        long b6 = (long)(src[offset + 6] & 0xFF) << 48;
        long b7 = (long)(src[offset + 7] & 0xFF) << 56;
        value = b0 | b1 | b2 | b3 | b4 | b5 | b6 | b7;
        return true;
    }

    private const int ExpectedPayloadSize = 36;

    public void HandleMobData(byte[] data)
    {
        if (data == null || data.Length < ExpectedPayloadSize) return;

        int offset = 0;
        if (!TryReadInt32LE(data, offset, out int mob_lv2)) return; offset += 4;
        if (!TryReadInt32LE(data, offset, out int mob_lv3)) return; offset += 4;
        if (!TryReadInt32LE(data, offset, out int mob_lv4)) return; offset += 4;
        if (!TryReadInt64LE(data, offset, out long timedelay_mob_lv2)) return; offset += 8;
        if (!TryReadInt64LE(data, offset, out long timedelay_mob_lv3)) return; offset += 8;
        if (!TryReadInt64LE(data, offset, out long timedelay_mob_lv4)) return; offset += 8;

        quantity2 = mob_lv2 > 0 ? mob_lv2 : timedelay_mob_lv2;
        quantity3 = mob_lv3 > 0 ? mob_lv3 : timedelay_mob_lv3;
        quantity4 = mob_lv4 > 0 ? mob_lv4 : timedelay_mob_lv4;
    }
}
