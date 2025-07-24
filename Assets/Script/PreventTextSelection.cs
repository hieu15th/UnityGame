using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class PreventSelectAll : MonoBehaviour
{
    private TMP_InputField inputField;

    void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
    }

    void Start()
    {
        inputField.onSelect.AddListener(_ => DeselectImmediately());
        inputField.onValueChanged.AddListener(_ => ForceCaretToEnd());
    }

    void LateUpdate()
    {
        // Luôn ép caret ở cuối và xóa vùng chọn nếu người dùng cố gắng kéo chọn
        ForceCaretToEnd();
    }

    void DeselectImmediately()
    {
        ForceCaretToEnd();
    }

    void ForceCaretToEnd()
    {
        int len = inputField.text.Length;
        inputField.caretPosition = len;
        inputField.selectionAnchorPosition = len;
        inputField.selectionFocusPosition = len;
    }
}
