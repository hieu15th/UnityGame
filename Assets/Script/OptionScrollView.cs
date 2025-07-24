using OptionDataNamespace;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
//using static UnityEditor.Progress;

public class OptionScrollView : MonoBehaviour
{
    public RectTransform content;
    public GameObject optionItemPrefab;

    public void ShowOptions(List<OptionData> options)
    {
        gameObject.SetActive(true);
        // Dọn dẹp cũ
        foreach (Transform child in content)
            Destroy(child.gameObject);

        for (int i = 0; i < options.Count; i++)
        {
            var opt = options[i];
            GameObject item = Instantiate(optionItemPrefab, content);
            var tmpText = item.GetComponentInChildren<TextMeshProUGUI>();

            if (tmpText != null)
            {
                if (i == 0)
                {
                    string name = opt.name;
                    int upgrade = 0;

                    // Giả định upgrade nằm ở param của option đầu tiên
                    if (options.Count > 1)
                    {
                        upgrade = options[0].param;
                        if (upgrade > 0)
                            name += $" +{upgrade}";
                    }

                    tmpText.text = name;
                    tmpText.fontSize = 12f;
                    tmpText.fontStyle = FontStyles.Italic;

                    if (upgrade > 0)
                    {
                        Color upgradeColor = GetUpgradeColor(upgrade);
                        StartCoroutineDelayedBlink(tmpText, Color.white, upgradeColor, 0.4f);
                    }
                    else
                    {
                        tmpText.color = Color.white;
                    }
                }
                else
                {
                    tmpText.text = $"{opt.name} {opt.param}";
                    tmpText.fontSize = 10f;
                    tmpText.fontStyle = FontStyles.Normal;
                    tmpText.color = GetColorFromOption(opt.color);
                }
            }
        }

        gameObject.SetActive(true);
    }
    private void StartCoroutineDelayedBlink(TextMeshProUGUI text, Color color1, Color color2, float interval)
    {
        // Nếu object đang inactive, delay cho đến khi nó được active
        if (!gameObject.activeInHierarchy)
        {
            StartCoroutine(WaitAndStart(text, color1, color2, interval));
        }
        else
        {
            StartCoroutine(BlinkTextColor(text, color1, color2, interval));
        }
    }

    private IEnumerator WaitAndStart(TextMeshProUGUI text, Color color1, Color color2, float interval)
    {
        // Chờ đến khi object này active
        while (!gameObject.activeInHierarchy)
            yield return null;

        yield return null; // đợi thêm 1 frame

        StartCoroutine(BlinkTextColor(text, color1, color2, interval));
    }

    private IEnumerator BlinkTextColor(TextMeshProUGUI text, Color color1, Color color2, float interval)
    {
        while (text != null)
        {
            text.color = color1;
            yield return new WaitForSeconds(interval);
            if (text == null) yield break;
            text.color = color2;
            yield return new WaitForSeconds(interval);
        }
    }
    private Color GetUpgradeColor(int upgrade)
    {
        int group = (upgrade - 1) / 4;
        return group switch
        {
            0 => Color.green,
            1 => Color.yellow,
            2 => Color.cyan,
            3 => Color.red,
            4 => Color.magenta,
            _ => Color.white
        };
    }


    public void Hide()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);

            Debug.Log("🚫 Ẩn ScrollView.");
        }
    }

    private Color GetColorFromOption(int colorCode)
    {
        switch (colorCode)
        {
            case 1: return Color.green;
            case 2: return Color.blue;
            case 3: return Color.magenta;
            case 4: return Color.yellow;
            default: return Color.white;
        }
    }
}
