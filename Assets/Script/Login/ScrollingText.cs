using UnityEngine;
using TMPro;

public class ScrollingText : MonoBehaviour
{
    [SerializeField] private TMP_Text textMeshPro;
    [SerializeField] private float scrollSpeed = 50f;
    [SerializeField] private float resetDelay = 1f;
    [SerializeField] private GameObject message_box;

    private string originalText;
    private string fullText;
    private float textWidth;
    private float containerWidth;
    private float scrollOffset = 0f;
    private float characterWidth;
    private bool isScrolling = false;
    private bool hasAddedSpaces = false;
    private bool hasShownMessageBox = false;

    private const int SPACE_COUNT = 5;

    void Start()
    {
        if (textMeshPro == null)
        {
            textMeshPro = GetComponent<TMP_Text>();
            if (textMeshPro == null)
            {
                Debug.LogError("ScrollingText: Cần TMP_Text component!");
                enabled = false;
                return;
            }
        }

        textMeshPro.overflowMode = TextOverflowModes.Overflow;

        originalText = textMeshPro.text;
        fullText = originalText;

        CalculateTextWidth();

        if (textWidth > containerWidth && fullText.Length > 0)
        {
            characterWidth = textWidth / fullText.Length;
            if (characterWidth <= 0) characterWidth = 1f;
            isScrolling = true;
        }

        ShowMessageBoxIfNeeded(); // ✅ gọi ở cuối Start
    }

    void Update()
    {
        if (!isScrolling) return;

        scrollOffset -= scrollSpeed * Time.deltaTime;

        int startIndex = Mathf.FloorToInt(Mathf.Abs(scrollOffset) / characterWidth);
        startIndex = Mathf.Max(0, startIndex);

        int visibleChars = Mathf.CeilToInt(containerWidth / characterWidth);
        if (visibleChars <= 0) visibleChars = 1;

        int endIndex = Mathf.Min(startIndex + visibleChars, fullText.Length);

        if (!hasAddedSpaces && endIndex >= originalText.Length)
        {
            fullText = originalText + new string(' ', SPACE_COUNT);
            hasAddedSpaces = true;
        }

        if (startIndex < fullText.Length && endIndex > startIndex)
        {
            string displayedText = fullText.Substring(startIndex, endIndex - startIndex);
            textMeshPro.text = displayedText;
        }
        else
        {
            scrollOffset = 0f;
            Invoke("ResetScroll", resetDelay);
            return;
        }

        textMeshPro.ForceMeshUpdate();

        if (endIndex >= fullText.Length)
        {
            scrollOffset = 0f;
            Invoke("ResetScroll", resetDelay);
        }
    }

    private void CalculateTextWidth()
    {
        textMeshPro.text = originalText;
        textMeshPro.ForceMeshUpdate();
        textWidth = textMeshPro.preferredWidth;
        containerWidth = textMeshPro.rectTransform.rect.width;
    }

    private void ResetScroll()
    {
        scrollOffset = 0f;
        fullText = originalText;
        textMeshPro.text = fullText;
        hasAddedSpaces = false;
        hasShownMessageBox = false;
        isScrolling = textWidth > containerWidth;
    }

    public void UpdateText(string newText)
    {
        originalText = string.IsNullOrEmpty(newText) ? " " : newText;
        fullText = originalText;
        textMeshPro.text = fullText;

        CalculateTextWidth();

        characterWidth = fullText.Length > 0 ? textWidth / fullText.Length : 1f;
        if (characterWidth <= 0) characterWidth = 1f;

        scrollOffset = 0f;
        hasAddedSpaces = false;
        hasShownMessageBox = false;
        isScrolling = textWidth > containerWidth;

        ShowMessageBoxIfNeeded(); // ✅ gọi ở cuối UpdateText
    }
    private void ShowMessageBoxIfNeeded()
    {
        if (!hasShownMessageBox && !string.IsNullOrWhiteSpace(originalText) && message_box != null)
        {
            message_box.SetActive(true);
            hasShownMessageBox = true;
        }
    }
    public void ClearText()
    {
        originalText = "";
        fullText = "";
        textMeshPro.text = "";

        scrollOffset = 0f;
        hasAddedSpaces = false;
        hasShownMessageBox = false;
        isScrolling = false;

        if (message_box != null)
        {
            message_box.SetActive(false);
        }
    }


}
