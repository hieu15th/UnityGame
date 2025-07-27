using TMPro;
using UnityEngine;
using UnityEngine.UI; // Để dùng LayoutRebuilder

public class PlayerUI : MonoBehaviour
{
    public TextMeshProUGUI posText;
    public TextMeshProUGUI hp;
    public TextMeshProUGUI gold;
    public TextMeshProUGUI diamond;

    public float scalePos = 5f;
    public PlayerController p;

    void Update()
    {
        if (p?.CurrentPlayer == null) return;

        Vector3 pos = p.CurrentPlayer.transform.position * scalePos;
        if (posText != null)
        {
            posText.SetText($"{(int)pos.x},{(int)pos.y}");
            posText.ForceMeshUpdate();
        }

        Player pl = p.CurrentPlayer.GetComponent<Player>();
        if (pl == null) return;

        // HP format
        string FormatHP(int value) => value >= 1000
            ? $"{(value / 1000f):F1}".Replace(".", ",") + "k"
            : value.ToString();

        if (hp != null)
        {
            hp.SetText($"{FormatHP(pl.hp_now)}/{FormatHP(pl.hp_max)}");
            hp.ForceMeshUpdate();
        }

        if (gold != null)
        {
            gold.SetText($"{pl.gold}");
            gold.ForceMeshUpdate();
        }

        if (diamond != null)
        {
            diamond.SetText($"{pl.diamond}");
            diamond.ForceMeshUpdate();
        }

    }

}
