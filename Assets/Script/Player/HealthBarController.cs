using UnityEngine;

public class HealthBarController : MonoBehaviour
{
    public float targetHP = 1f; // HP mục tiêu của thanh máu
    public float lerpSpeed = 5f; // Tốc độ chuyển đổi (Lerp speed)
    public bool die = false;

    private void Update()
    {
        // Lấy dữ liệu mob và player để cập nhật thanh máu
        MobData mobData = GetComponentInParent<MobData>();
        if (mobData != null)
        {
            SetHP((float)mobData.current_hp / Mathf.Max(1, mobData.hp));
        }

        Player player = GetComponentInParent<Player>();
        if (player != null)
        {
            SetHP((float)player.hp_now / Mathf.Max(1, player.hp_max));
        }

        // Lerp để giảm thanh máu mượt mà
        float currentAbsX = transform.localScale.x;
        float lerpedX = Mathf.Lerp(currentAbsX, targetHP, Time.deltaTime * lerpSpeed);

        Vector3 finalScale = transform.localScale;
        finalScale.x = lerpedX;
        transform.localScale = finalScale;

        // Chỉ đánh dấu die khi thanh máu thực sự giảm về 0
        if (!die && lerpedX <= 0.01f) // 0.01f để tránh float precision
        {
            die = true;
            // Có thể thêm sự kiện hoặc hành động khác ở đây
            // Debug.Log("Entity died!");
        }
    }

    public void SetHP(float hp)
    {
        targetHP = Mathf.Clamp01(hp);
    }
}
