using UnityEngine;

public class HealthBarController : MonoBehaviour
{
    public float targetHP = 1f;  // HP mục tiêu của thanh máu
    public float lerpSpeed = 5f; // Tốc độ chuyển đổi (Lerp speed)
    public bool die = false;

    private bool isHpZero = false; // Đánh dấu khi hp thực tế = 0

    private void Update()
    {
        // Kiểm tra mob
        MobData mobData = GetComponentInParent<MobData>();
        if (mobData != null)
        {
            SetHP((float)mobData.current_hp / Mathf.Max(1, mobData.hp));
            isHpZero = mobData.current_hp <= 0;
        }

        // Nếu không phải mob thì check player
        Player player = GetComponentInParent<Player>();
        if (player != null)
        {
            SetHP((float)player.hp_now / Mathf.Max(1, player.hp_max));
            isHpZero = player.hp_now <= 0;
        }

        // Lerp mượt mà
        float currentAbsX = transform.localScale.x;
        float lerpedX = Mathf.Lerp(currentAbsX, targetHP, Time.deltaTime * lerpSpeed);

        Vector3 finalScale = transform.localScale;
        finalScale.x = lerpedX;
        transform.localScale = finalScale;

        // Chỉ set die khi hp thực tế = 0 và thanh máu đã chạy xong
        if (!die && isHpZero && lerpedX <= 0.01f)
        {
            die = true;
        }
    }

    public void SetHP(float hp)
    {
        targetHP = Mathf.Clamp01(hp);
    }
}
