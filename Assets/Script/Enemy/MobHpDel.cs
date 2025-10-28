using UnityEngine;
using TMPro; // Nếu bạn sử dụng TextMeshPro
using System.Collections; // Nếu bạn sử dụng Coroutine

public class MobHpDel : MonoBehaviour
{
    [SerializeField] private GameObject m_gameObject;  // GameObject chứa chữ
    [SerializeField] private GameObject textPrefab;    // Prefab chứa chữ
    [SerializeField] private Vector3 textPositionOffset; // Vị trí lệch của chữ so với m_gameObject
    private GameObject currentText;
    private float moveDuration = 1f; // Thời gian di chuyển lên (1 giây)
    // Coroutine để di chuyển chữ lên trên trong 1 giây
    private IEnumerator MoveTextUp()
    {
        float elapsedTime = 0f;
        Vector3 initialPosition = currentText.transform.localPosition;

        // Kiểm tra nếu currentText đã bị hủy
        if (currentText == null) yield break;

        // Đảm bảo text được hiển thị trước khi di chuyển
        currentText.SetActive(true);

        while (elapsedTime < moveDuration)
        {
            // Kiểm tra nếu currentText đã bị hủy trong quá trình di chuyển
            if (currentText == null)
            {
                yield break; // Dừng coroutine nếu currentText đã bị hủy
            }

            // Tính toán sự dịch chuyển theo thời gian
            currentText.transform.localPosition = initialPosition + Vector3.up * Mathf.Lerp(0, 1, elapsedTime / moveDuration);

            elapsedTime += Time.deltaTime;
            yield return null;  // Đợi frame tiếp theo
        }

        // Đảm bảo rằng chữ đã di chuyển đến vị trí cuối cùng
        if (currentText != null)
        {
            currentText.transform.localPosition = initialPosition + Vector3.up;
        }
    }



    public void UpdateHpText(string text, int type)
    {
        if (currentText != null)
        {
            TextMeshPro tmpText = currentText.GetComponent<TextMeshPro>();
            if (tmpText != null)
            {

                // Xác định màu dựa vào type
                Color color = Color.white;
                if (type == 1)
                    color = Color.red;
                else if (type == 2)
                    color = Color.green;

                string colorHex = ColorUtility.ToHtmlStringRGB(color);

                // Luôn in đậm
                tmpText.fontSize = 25;
                tmpText.fontStyle = FontStyles.Italic;

                // Dấu '-' luôn trắng, phần sau có màu riêng — toàn bộ đều Bold
                tmpText.text =
                    $"<b><color=#{colorHex}>-{text}</color></b>";
            }

        }
    }

    // Update is called once per frame
    void Update()
    {
        MobData mob = GetComponentInParent<MobData>();
        if (mob.hp_del != 0)
        {
            if (textPrefab != null && m_gameObject != null)
            {
                // Gọi coroutine để delay 0.5s trước khi tạo text
                StartCoroutine(SpawnHpTextWithDelay(mob.hp_del, mob.type));
            }
            mob.type = -1;
            mob.hp_del = 0;  // Reset hp_del sau khi đã sử dụng
        }
    }

    private IEnumerator SpawnHpTextWithDelay(int damage, int type)
    {
        // ⏳ Chờ 0.5 giây
        yield return new WaitForSeconds(0.5f);

        // Tạo chữ tại vị trí của m_gameObject với độ lệch từ m_gameObject
        currentText = Instantiate(textPrefab, m_gameObject.transform.position + textPositionOffset, Quaternion.identity);

        // Đặt đối tượng chữ là con của m_gameObject để di chuyển cùng
        currentText.transform.SetParent(m_gameObject.transform);

        // Đảm bảo rằng chữ luôn ở vị trí chính xác so với m_gameObject
        currentText.transform.localPosition = textPositionOffset;

        // Giữ nguyên scale của prefab
        currentText.transform.localScale = textPrefab.transform.localScale;

        Canvas textCanvas = currentText.GetComponentInChildren<Canvas>();
        if (textCanvas != null)
        {
            textCanvas.overrideSorting = true;
            textCanvas.sortingOrder = 10;
        }
        else
        {
            var renderer = currentText.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
                renderer.sortingOrder = 10;
        }
        UpdateHpText(damage.ToString(), type);

        // Tính toán tốc độ di chuyển (di chuyển lên trong 1 giây)
        StartCoroutine(MoveTextUp());
    }


}
