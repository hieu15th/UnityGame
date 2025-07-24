using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class ClientVersionDisplay : MonoBehaviour
{

    private void Start()
    {
        TMP_Text text = GetComponent<TMP_Text>();
        if (text != null)
        {
            text.text ="Version: "+Application.version;
        }
    }
}
