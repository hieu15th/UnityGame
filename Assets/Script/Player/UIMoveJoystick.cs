using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class UIMoveJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform background;            // vòng ngoài
    public RectTransform handle;                // núm
    [Range(0f, 1f)] public float deadZone = 0.1f;
    public bool snapTo4Directions = false;
    public bool autoCenterPivot = true;

    [Header("Ổn định cạnh")]
    [Min(0f)] public float padding = 0f;        // chừa mép (px, theo local background)
    [Min(0f)] public float edgeEpsilon = 1f;    // chừa thêm để tránh rung ở mép (px)
    [Range(0f, 1f)] public float smooth = 0f;   // 0 = lập tức, >0 = mượt (lerp)

    public Vector2 Direction { get; private set; }
    public bool IsPressed { get; private set; }

    float allowedR; // bán kính cho tâm núm (local của background)

    void Awake()
    {
        if (!background) background = transform as RectTransform;
        if (!handle && background && background.childCount > 0)
            handle = background.GetChild(0) as RectTransform;

        if (autoCenterPivot) { Center(background); Center(handle); }
        RecalcRadii();
        ResetStick();
    }

    void OnRectTransformDimensionsChange() => RecalcRadii();

    static void Center(RectTransform rt)
    {
        if (!rt) return;
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.pivot = new Vector2(.5f, .5f);
    }
    void Update()
    {
        if (!IsPressed) // chỉ dùng phím khi joystick không bị chạm
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector2 dir = new Vector2(h, v);

            if (dir.magnitude > 0.01f)
            {
                dir = dir.normalized;
                Direction = dir;

                if (handle)
                    handle.anchoredPosition = dir * allowedR;
            }
            else
            {
                ResetStick();
            }
        }
    }

    void RecalcRadii()
    {
        if (!background) return;

        // bán kính vòng ngoài (local của background)
        float outerR = 0.5f * Mathf.Min(background.rect.width, background.rect.height);

        // bán kính núm quy đổi về local của background (tính cả scale chéo cha/con)
        float handleR = 0f;
        if (handle)
        {
            Vector2 hSize = handle.rect.size;
            float sx = handle.lossyScale.x / background.lossyScale.x;
            float sy = handle.lossyScale.y / background.lossyScale.y;
            handleR = 0.5f * Mathf.Min(hSize.x * sx, hSize.y * sy);
        }

        allowedR = Mathf.Max(0f, outerR - handleR - padding - edgeEpsilon);

        // nếu đang ở ngoài phạm vi mới tính, kéo lại vào trong
        if (handle) handle.anchoredPosition = Vector2.ClampMagnitude(handle.anchoredPosition, allowedR);
    }

    public void OnPointerDown(PointerEventData e) => OnDrag(e);

    public void OnDrag(PointerEventData e)
    {
        if (!background) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, e.position, e.pressEventCamera, out var local))
            return;

        IsPressed = true;

        // clamp tâm núm vào vòng bán kính "allowedR"
        Vector2 target = Vector2.ClampMagnitude(local, allowedR);

        // di chuyển núm
        if (handle)
        {
            if (smooth > 0f)
            {
                // mượt bất biến theo frame: smooth≈0.2 ~ nhanh, 0.05 ~ rất mượt
                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(smooth), Time.unscaledDeltaTime * 60f);
                handle.anchoredPosition = Vector2.Lerp(handle.anchoredPosition, target, t);
            }
            else handle.anchoredPosition = target;
        }

        // hướng - chuẩn hóa theo allowedR (1.0 ở mép)
        Vector2 p = handle ? handle.anchoredPosition : target;
        Vector2 dir = (allowedR > 1e-4f) ? (p / allowedR) : Vector2.zero;

        if (dir.magnitude < deadZone) dir = Vector2.zero;

        if (snapTo4Directions && dir != Vector2.zero)
        {
            dir = (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) ? new Vector2(Mathf.Sign(dir.x), 0f)
                                                        : new Vector2(0f, Mathf.Sign(dir.y));
            if (handle) handle.anchoredPosition = dir * allowedR;
        }

        Direction = Vector2.ClampMagnitude(dir, 1f);
    }

    public void OnPointerUp(PointerEventData e) => ResetStick();

    void ResetStick()
    {
        IsPressed = false;
        Direction = Vector2.zero;
        if (handle) handle.anchoredPosition = Vector2.zero;
    }
}
