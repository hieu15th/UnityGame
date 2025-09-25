using UnityEngine;
using UnityEngine.EventSystems;

public class SlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    private ISlotSelectable layout;
    private int index;

    public void Init(ISlotSelectable layout, int i)
    {
        this.layout = layout;
        index = i;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        layout.SelectSlot(index);
    }
}
