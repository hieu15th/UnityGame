using UnityEngine;
using UnityEngine.EventSystems;

public class HideOnClickOutside : MonoBehaviour
{
    void Update()
    {
        // Kiểm tra nếu nhấn chuột trái
        if (Input.GetMouseButtonDown(0))
        {
            // Nếu chuột KHÔNG nằm trên bất kỳ UI nào (hoặc trên UI khác)
            if (!IsPointerOverUIObject())
            {
                gameObject.SetActive(false);
            }
        }
    }

    private bool IsPointerOverUIObject()
    {
        // Lấy thông tin pointer tại vị trí chuột
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            // Nếu UI được nhấn là chính đối tượng hiện tại hoặc con của nó
            if (result.gameObject == gameObject || result.gameObject.transform.IsChildOf(transform))
            {
                return true;
            }
        }

        // Không nhấn vào chính nó hoặc con => return false
        return false;
    }
}
