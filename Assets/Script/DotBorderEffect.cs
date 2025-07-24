using UnityEngine;
using UnityEngine.UI;

public class DotBorderEffect : MonoBehaviour
{
    public RectTransform[] dots; // Kéo thả dot vào đây (1-4)
    public float speed = 50f;    // Tốc độ di chuyển (pixels/sec)
    public Color dotColor = Color.red;

    private RectTransform rect;
    private float width, height, perimeter;
    private bool initialized = false;
    private int activeDotCount = 1;

    void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    public void Init(Color color, int count)
    {
        if (dots == null || dots.Length == 0) return;

        rect = GetComponent<RectTransform>();
        if (rect == null) return;

        dotColor = color;
        activeDotCount = Mathf.Clamp(count, 0, dots.Length);
        initialized = true;

        width = rect.rect.width;
        height = rect.rect.height;
        perimeter = 2f * (width + height);

        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] == null) continue;

            dots[i].gameObject.SetActive(i < activeDotCount);
            SetDotColor(dots[i], dotColor);
        }
    }

    private void SetDotColor(RectTransform dot, Color color)
    {
        if (dot == null) return;
        Image img = dot.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    void Update()
    {
        if (!initialized || rect == null) return;

        width = rect.rect.width;
        height = rect.rect.height;
        perimeter = 2f * (width + height);

        for (int i = 0; i < activeDotCount; i++)
        {
            float offset = (perimeter / activeDotCount) * i;
            float t = Mathf.Repeat(Time.time * speed + offset, perimeter);

            Vector2 pos;
            if (t < width)
            {
                // Top: left → right
                pos = new Vector2(-width / 2f + t, height / 2f);
            }
            else if (t < width + height)
            {
                // Right: top → bottom
                pos = new Vector2(width / 2f, height / 2f - (t - width));
            }
            else if (t < width * 2 + height)
            {
                // Bottom: right → left
                pos = new Vector2(width / 2f - (t - width - height), -height / 2f);
            }
            else
            {
                // Left: bottom → top
                pos = new Vector2(-width / 2f, -height / 2f + (t - width * 2 - height));
            }

            if (dots[i] != null)
                dots[i].anchoredPosition = pos;
        }
    }
}
