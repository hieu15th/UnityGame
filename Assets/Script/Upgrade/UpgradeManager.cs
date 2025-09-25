using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    [Header("Danh sách 5 Object")]
    public GameObject[] objects = new GameObject[6]; // Cho sẵn slot 5 obj
    public bool check=false;
    // Lưu vị trí ban đầu
    private Vector3[] savedPositions;

    void Start()
    {
        savedPositions = new Vector3[objects.Length];

        for (int i = 0; i < objects.Length-1; i++)
        {
            if (objects[i] != null)
            {
                savedPositions[i] = objects[i].transform.position;
                Debug.Log($"📌 Object {i + 1} vị trí ban đầu: {savedPositions[i]}");
            }
        }
    }
    void Update()
    {
        // So sánh: vị trí hiện tại của obj[4] với savedPositions[5]
        if (objects[4] != null)
        {
            if (Vector3.Distance(objects[4].transform.position, savedPositions[5]) < 0.01f)
            {
                if (!check)
                {
                    check = true;
                    Debug.Log("✅ Object[4] đã về đúng vị trí ban đầu của Object[5]");
                }
            }
        }
    }

    public void ResetAllObjects()
    {
        check =false;
        for (int i = 4; i >= 0; i--)
        {
            if (objects[i] != null)
            {
                objects[i].transform.position = savedPositions[i]; 

                if (i != 4)
                {
                    objects[i].SetActive(true);
                }

                Debug.Log($"🔄 Object {i} reset về {savedPositions[i]} (Active={objects[i].activeSelf})");
            }
        }
    }
}
