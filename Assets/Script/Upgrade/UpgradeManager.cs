using UnityEngine;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    [Header("Danh sách 6 Object (UI)")]
    public GameObject[] objects = new GameObject[6];
    public bool check = false;
    public int result;
    public TMPNumberView rs;

    private Vector2[] savedPositions;

    void Start()
    {
        savedPositions = new Vector2[objects.Length];

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                // Nếu object chưa có component theo dõi, thêm luôn
                var tracker = objects[i].GetComponent<UITracker>();
                if (tracker == null) tracker = objects[i].AddComponent<UITracker>();
                tracker.index = i;
                tracker.manager = this;

                // Lưu vị trí hiện tại
                RectTransform rect = objects[i].GetComponent<RectTransform>();
                savedPositions[i] = rect.anchoredPosition;

            }
        }
    }

    void Update()
    {
        if (objects[4] != null && objects[5] != null)
        {
            RectTransform rect4 = objects[4].GetComponent<RectTransform>();
            Vector2 pos4 = rect4.anchoredPosition;

            Vector2 targetPos = savedPositions[5]; // Vị trí của object[5]

            if (Vector2.Distance(pos4, targetPos) < 2.6f)
            {
                if (!check)
                {
                    rs.View(result);
                    check = true;
                }
            }
        }
    }


    public void ResetAllObjects()
    {
        check = false;
        for (int i = 4; i >= 0; i--)
        {
            if (objects[i] != null)
            {
                RectTransform rect = objects[i].GetComponent<RectTransform>();
                rect.anchoredPosition = savedPositions[i];

                if (i != 4)
                    objects[i].SetActive(true);

                Debug.Log($"🔄 Object {i} reset về {savedPositions[i]} (Active={objects[i].activeSelf})");
            }
        }
    }

    // Cập nhật vị trí khi object được bật lại
    public void UpdateSavedPosition(int index, Vector2 newPos)
    {
        if (index >= 0 && index < savedPositions.Length)
        {
            savedPositions[index] = newPos;
            Debug.Log($"💾 Cập nhật vị trí Object {index}: {newPos}");
        }
    }

    // 👇 Class con ngay trong file — tự động gọi khi SetActive(true)
    private class UITracker : MonoBehaviour
    {
        public int index;
        public UpgradeManager manager;

        void OnEnable()
        {
            if (manager != null)
            {
                RectTransform rect = GetComponent<RectTransform>();
                manager.UpdateSavedPosition(index, rect.anchoredPosition);
            }
        }
    }
}
