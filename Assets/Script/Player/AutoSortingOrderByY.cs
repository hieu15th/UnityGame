using UnityEngine;
using UnityEngine.Rendering;

public class AutoSortingWithOffset : MonoBehaviour
{
    private SpriteRenderer sr;
    private SortingGroup sg;
    public int sortingOffset = 10000; // Giữ cho sortingOrder luôn dương

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sg = GetComponent<SortingGroup>();

        if (sr == null && sg == null)
        {
            Debug.LogWarning($"⚠️ {gameObject.name} không có SpriteRenderer hoặc SortingGroup!");
        }
    }

    void LateUpdate()
    {
        int order = sortingOffset - Mathf.RoundToInt(transform.position.y * 100);

        if (sr != null)
        {
            sr.sortingOrder = order;
        }
        else if (sg != null)
        {
            sg.sortingOrder = order;
        }
    }
}
