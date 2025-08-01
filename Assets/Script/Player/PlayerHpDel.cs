using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerHpDel : MonoBehaviour
{
    private Player player;
    [SerializeField] private GameObject m_gameObject;  // GameObject chứa chữ
    [SerializeField] private GameObject textPrefab;    // Prefab chứa chữ
    [SerializeField] private Vector3 textPositionOffset; // Vị trí lệch của chữ so với m_gameObject
    private GameObject currentText;
    private float moveDuration = 1f; // Thời gian di chuyển lên (1 giây)
    private int hp_old;
    void Start()
    {
        player = GetComponent<Player>();
        if(player != null)
        {
            hp_old = player.hp_now;
        }
    }
    public void UpdateHpText(string text)
    {
        if (currentText != null)
        {
            // Cập nhật nội dung chữ (TextMeshPro)
            TextMeshPro tmpText = currentText.GetComponent<TextMeshPro>(); // Nếu dùng TextMeshPro
            if (tmpText != null)
            {
                tmpText.text = "-" + text;
            }
        }
    }

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
    // Update is called once per frame
    void Update()
    {
        if (player != null)
        {
            if (player.hp_now < hp_old)
            {
                if (textPrefab != null && m_gameObject != null)
                {
                    // Tạo chữ tại vị trí của m_gameObject với độ lệch từ m_gameObject
                    currentText = Instantiate(textPrefab, m_gameObject.transform.position + textPositionOffset, Quaternion.identity);

                    // Đặt đối tượng chữ là con của m_gameObject để di chuyển cùng
                    currentText.transform.SetParent(m_gameObject.transform);

                    // Đảm bảo rằng chữ luôn ở vị trí chính xác so với m_gameObject
                    currentText.transform.localPosition = textPositionOffset;

                    // Giữ nguyên scale của prefab
                    currentText.transform.localScale = textPrefab.transform.localScale;

                    // Cập nhật nội dung chữ, chuyển hp_del thành string
                    UpdateHpText((hp_old-player.hp_now).ToString());

                    StartCoroutine(MoveTextUp());
                }
                hp_old = player.hp_now;
            } else if (player.hp_now > hp_old)
            {
                hp_old=player.hp_now;
            }
        }
    }
}
