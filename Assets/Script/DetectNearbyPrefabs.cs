using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DetectNearbyPrefabs_Circle : MonoBehaviour, IPointerClickHandler
{
    public float detectRadius;              // Bán kính tìm
    public string prefabTag = "Player";          // Tag của đối tượng cần tìm
    public LayerMask detectionLayer;             // Layer dùng để lọc Collider2D

    private List<GameObject> nearbyList = new List<GameObject>();
    private int currentIndex = -1;
    private GameObject currentSelected;

    void Update()
    {
        UpdateDetection();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("🖱 Click UI thành công!");
        RefreshNearbyList();
        SelectNextNearby();
    }

    void RefreshNearbyList()
    {
        nearbyList.Clear();

        Vector2 center = Camera.main.transform.position; // 🎯 Tâm là camera
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, detectRadius, detectionLayer);

        Debug.Log($"🔍 Tìm thấy {hits.Length} đối tượng quanh camera bán kính {detectRadius}");

        foreach (var hit in hits)
        {
            GameObject obj = hit.gameObject;

            if (!obj.CompareTag(prefabTag)) continue;
            nearbyList.Add(obj);
        }

        // Nếu current không còn trong danh sách thì tắt nó
        if (!nearbyList.Contains(currentSelected))
        {
            if (currentSelected != null)
            {
                ToggleChoose(currentSelected, false);
                Debug.Log($"🚫 Tắt Choose của: {currentSelected.name}");
            }

            currentSelected = null;
            currentIndex = -1;
        }
    }

    void SelectNextNearby()
    {
        if (nearbyList.Count == 0)
        {
            Debug.Log("❌ Không có object nào để chọn.");
            return;
        }

        currentIndex = (currentIndex + 1) % nearbyList.Count;
        GameObject next = nearbyList[currentIndex];

        // Tắt đối tượng hiện tại nếu khác
        if (currentSelected != null && currentSelected != next)
        {
            ToggleChoose(currentSelected, false);
            Debug.Log($"🔁 Chuyển từ {currentSelected.name} sang {next.name}");
        }

        ToggleChoose(next, true);
        Debug.Log($"✅ Bật Choose của: {next.name}");

        currentSelected = next;
    }

    void UpdateDetection()
    {
        if (currentSelected == null) return;

        float dist = Vector2.Distance(Camera.main.transform.position, currentSelected.transform.position);
        if (dist > detectRadius)
        {
            ToggleChoose(currentSelected, false);
            Debug.Log($"🚫 Tắt Choose vì rời khỏi bán kính: {currentSelected.name}");
            currentSelected = null;
            currentIndex = -1;
            nearbyList.Clear();
        }
    }

    void ToggleChoose(GameObject obj, bool active)
    {
        Transform choose = obj.transform.Find("Choose");
        if (choose != null)
        {
            choose.gameObject.SetActive(active);
        }
        else
        {
            Debug.LogWarning($"⚠️ Không tìm thấy child 'Choose' trong {obj.name}");
        }
    }

    // Vẽ vùng quét ra trong Scene view
    void OnDrawGizmosSelected()
    {
        if (Camera.main != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Camera.main.transform.position, detectRadius);
        }
    }
}
