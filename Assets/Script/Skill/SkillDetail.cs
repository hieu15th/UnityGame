using OptionDataNamespace;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static SkillUI;
//using static UnityEditor.Progress;

public class SkillDetail : MonoBehaviour
{
    public RectTransform content;
    public GameObject optionItemPrefab;

    public void ShowSkillDetail(SkillInfo skill)
    {
        gameObject.SetActive(true);

        foreach (Transform child in content)
            Destroy(child.gameObject);

        GameObject nameItem = Instantiate(optionItemPrefab, content);
        var nameText = nameItem.GetComponentInChildren<TextMeshProUGUI>();
        nameText.text = skill.name;
        nameText.fontSize = 14f;
        nameText.color = GetColorFromOption(skill.color);

        GameObject detailItem = Instantiate(optionItemPrefab, content);
        var detailText = detailItem.GetComponentInChildren<TextMeshProUGUI>();
        detailText.text = skill.detail;
        detailText.fontSize = 10f;
        detailText.color = Color.white;
        detailText.fontStyle = FontStyles.Normal;

        GameObject cd = Instantiate(optionItemPrefab, content);
        var cdtext = cd.GetComponentInChildren<TextMeshProUGUI>();
        cdtext.text = "CD: " +skill.cd.ToString()+"s";
        cdtext.fontSize = 10f;
        cdtext.color = Color.white;
        cdtext.fontStyle = FontStyles.Normal;
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
            case 5: return Color.black;
            default: return Color.white;
        }
    }
}
