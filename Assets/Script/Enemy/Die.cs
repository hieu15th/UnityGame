using Unity.VisualScripting;
using UnityEngine;

public class Die : MonoBehaviour
{
    private MobData mobData;
    private Animator animator;
    private GameObject hp;

    void Start()
    {
        if (mobData == null)
        {
            mobData = gameObject.GetComponent<MobData>();
        }
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        if(hp == null)
        {
            hp = transform.Find("Heath_Bar")?.gameObject;

            // Kiểm tra nếu đối tượng không tồn tại
            if (hp == null)
            {
                Debug.LogError("❌ Không tìm thấy đối tượng 'Heath_Bar' trong các đối tượng con.");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (mobData.current_hp == 0)
        {
            animator.SetBool("Dead", true);
            Destroy(hp);
            Destroy(gameObject, 3);
        }
    }
}
