using System.Collections;
using TMPro;
using UnityEngine;

public class DisplayChat : MonoBehaviour
{
    public string mess;
    public GameObject chat;
    public TextMeshPro text;
    private Coroutine hideCoroutine;

    void Update()
    {
        if(mess != string.Empty)
        {
            Open();
        }
    }
    void Open()
    {
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        chat.SetActive(true);
        text.text = mess;
        mess = string.Empty;
        // 🔁 Tự động ẩn sau 5 giây
        hideCoroutine = StartCoroutine(HideAfterDelay(5f));
    }

    IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        chat.SetActive(false);
        text.text = string.Empty;
    }
}
