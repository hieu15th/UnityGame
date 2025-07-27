using UnityEngine;
using UnityEngine.UI;

public class InventoryOpenButton : MonoBehaviour
{
    [SerializeField] private InventoryToggle inventoryToggle;
    private Button btn;

    void Awake()
    {
        GameObject btnObj = GameObject.FindWithTag("UI_Bag");
        if (btnObj == null)
        {
            Debug.LogError("❌ Không tìm thấy GameObject với tag 'UI_Bag'");
            return;
        }

        btn = btnObj.GetComponent<Button>();
        if (btn == null)
        {
            Debug.LogError("❌ GameObject với tag 'UI_Bag' không có component Button");
        }
    }

    void Start()
    {
        if (inventoryToggle == null)
        {
            inventoryToggle = FindFirstObjectByType<InventoryToggle>();
            if (inventoryToggle == null)
            {
                Debug.LogError("❌ Không tìm thấy InventoryToggle.");
            }
        }

        if (btn != null)
        {
            btn.onClick.AddListener(() =>
            {
                inventoryToggle.OpenInventoryFromButton();
            });
        }
    }
}
