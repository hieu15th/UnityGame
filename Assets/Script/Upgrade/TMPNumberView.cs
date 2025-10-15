using UnityEngine;
using TMPro;
using System.Collections;

public class TMPNumberView : MonoBehaviour
{
    public TextMeshProUGUI tmp;

    public void View(int value)
    {
        if (value == 0 || tmp == null)
            return;

        StopAllCoroutines();
        StartCoroutine(ShowNumber(value));
    }

    private IEnumerator ShowNumber(int value)
    {
        Debug.LogError("1231231231");
        tmp.fontSize = 12;
        tmp.gameObject.SetActive(true);

        string text = (value == 1) ? "Thành công" : "Thất bại";
        tmp.SetText(text);
        tmp.ForceMeshUpdate(true);

        yield return new WaitForSeconds(1f);

        // Clear & ẩn chắc chắn
        tmp.SetText(string.Empty);
        tmp.ForceMeshUpdate(true);
        tmp.gameObject.SetActive(false);

    }
}
