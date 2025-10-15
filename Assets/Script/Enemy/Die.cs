using UnityEngine;

public class Die : MonoBehaviour
{
    private MobData mobData;
    private Animator animator;
    private GameObject hp;
    private AudioSource[] hpSource;
    public HealthBarController healthBarController;

    private bool isDead = false; // ✅ flag để tránh gọi nhiều lần

    void Start()
    {
        hpSource = GetComponents<AudioSource>();
        if (mobData == null)
        {
            mobData = gameObject.GetComponent<MobData>();
        }
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        if (hp == null)
        {
            hp = transform.Find("Heath_Bar")?.gameObject;

            if (hp == null)
            {
                Debug.LogError("❌ Không tìm thấy đối tượng 'Heath_Bar' trong các đối tượng con.");
            }
        }
    }

    void Update()
    {
        if (healthBarController.die && !isDead) // ✅ chỉ chạy 1 lần
        {
            isDead = true;

            if (hpSource.Length > 1)
            {
                hpSource[1].Play();
            }

            animator.SetBool("Dead", true);

            if (hp != null)
            {
                Destroy(hp);
            }

            Destroy(gameObject, 3); // hủy sau 3s
        }
    }
}
