using UnityEngine;
using UnityEngine.UI;

public class ButtonClick : MonoBehaviour
{
    public Animator animator;
    public string triggerName = "2_Attack";

    void OnMouseDown()
    {
        if (animator == null)
        {
            Debug.LogWarning("⚠️ Chưa gán Animator!");
            return;
        }

        animator.SetTrigger(triggerName);
        //Debug.Log("🖱️ Clicked on object. Trigger sent: " + triggerName);
    }
}
