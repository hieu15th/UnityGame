using UnityEngine;

public class HealthBarController : MonoBehaviour
{
    public float targetHP = 1f; // HP mục tiêu của thanh máu
    public float lerpSpeed = 5f; // Tốc độ chuyển đổi (Lerp speed)

    private void Update()
    {
        // Lấy dữ liệu mob và player để cập nhật thanh máu
        MobData mobData = GetComponentInParent<MobData>();
        if (mobData != null)
        {
            // Cập nhật targetHP với tỷ lệ HP hiện tại so với HP tối đa của mob
            SetHP((float)mobData.current_hp / Mathf.Max(1, mobData.hp));
        }

        Player player = GetComponentInParent<Player>();
        if (player != null)
        {
            // Cập nhật targetHP với tỷ lệ HP hiện tại so với HP tối đa của player
            SetHP((float)player.hp_now / Mathf.Max(1, player.hp_max));
        }

        // Lerp để giảm thanh máu mượt mà
        float currentAbsX = transform.localScale.x; // Lấy chiều rộng hiện tại của thanh máu
        float lerpedX = Mathf.Lerp(currentAbsX, targetHP, Time.deltaTime * lerpSpeed); // Tính toán chiều rộng mới

        // Cập nhật lại chiều rộng thanh máu
        Vector3 finalScale = transform.localScale;
        finalScale.x = lerpedX; // Đặt chiều rộng mới của thanh máu
        transform.localScale = finalScale;
    }

    // Cập nhật HP mục tiêu
    public void SetHP(float hp)
    {
        targetHP = Mathf.Clamp01(hp); // Giới hạn HP trong khoảng [0, 1] để không vượt quá 100% hoặc 0%
    }
}
