using UnityEngine;
using UnityEngine.EventSystems;

public class SlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    private BagLayoutAdjuster adjuster;
    private int index;

    public void Init(BagLayoutAdjuster adjuster, int index)
    {
        this.adjuster = adjuster;
        this.index = index;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        adjuster.choose = true;
        adjuster.SelectSlot(index);
    }

}
