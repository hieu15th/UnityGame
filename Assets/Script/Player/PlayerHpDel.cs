using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerHpDel : MonoBehaviour
{
    private Player player;
    [SerializeField] private GameObject m_gameObject;      // GameObject chứa chữ
    [SerializeField] private GameObject textPrefab;        // Prefab chứa chữ
    [SerializeField] private Vector3 textPositionOffset;   // Vị trí lệch của chữ so với m_gameObject
    public TMP_SpriteAsset hpSpriteAsset;
    private float moveDuration = 1f; // Thời gian di chuyển lên (1 giây)
    private int hp_old;

    void Start()
    {
        player = GetComponent<Player>();
        if (player != null)
        {
            hp_old = player.hp_now;
        }
    }

    private void UpdateHpText(GameObject textObj, string text)
    {
        TextMeshPro tmpText = textObj.GetComponent<TextMeshPro>();
        if (tmpText != null)
        {
            tmpText.spriteAsset = hpSpriteAsset;
            tmpText.fontSize = 12;
            tmpText.text = "- <size=80%><sprite=0></size>    " + text; // chỉ scale icon
        }
    }

    private IEnumerator MoveTextUp(GameObject textObj)
    {
        if (textObj == null) yield break;

        float elapsedTime = 0f;
        Vector3 initialPosition = textObj.transform.localPosition;

        textObj.SetActive(true);

        while (elapsedTime < moveDuration)
        {
            if (textObj == null) yield break;

            textObj.transform.localPosition =
                initialPosition + Vector3.up * Mathf.Lerp(0, 1, elapsedTime / moveDuration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (textObj != null)
        {
            textObj.transform.localPosition = initialPosition + Vector3.up;
            Destroy(textObj, 0.2f); // ✅ xoá text sau khi hoàn thành
        }
    }

    void Update()
    {
        if (player != null)
        {
            if (player.hp_now < hp_old)
            {
                if (textPrefab != null && m_gameObject != null)
                {
                    GameObject newText = Instantiate(
                        textPrefab,
                        m_gameObject.transform.position + textPositionOffset,
                        Quaternion.identity,
                        m_gameObject.transform // parent trực tiếp khi spawn
                    );

                    newText.transform.localPosition = textPositionOffset;
                    newText.transform.localScale = textPrefab.transform.localScale;

                    // Update nội dung chữ
                    UpdateHpText(newText, (hp_old - player.hp_now).ToString());

                    // Coroutine riêng cho text đó
                    StartCoroutine(MoveTextUp(newText));
                }
                hp_old = player.hp_now;
            }
            else if (player.hp_now > hp_old)
            {
                hp_old = player.hp_now;
            }
        }
    }
}
