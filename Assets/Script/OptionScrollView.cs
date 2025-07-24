using OptionDataNamespace;
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
                    // 🛠 Ghép +X nếu upgrade > 0
                    string name = opt.name;
                    if (options.Count > 1)
                    {
                        // Giả định upgrade được lưu trong OptionData đầu tiên như param
                        int upgrade = options[0].param; // hoặc bạn có thể truyền upgrade riêng nếu cần rõ hơn
                        if (upgrade > 0)
                            name += $" +{upgrade}";
                    }

                    tmpText.text = name;
                    tmpText.fontSize = 12f;
                    tmpText.fontStyle = FontStyles.Italic;
                    tmpText.color = Color.white;
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
