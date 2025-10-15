using System.Collections.Generic;
using UnityEngine;

public class ZoneAttack : MonoBehaviour
{
    public string enemyTag = "Enemy";
    public float attackInterval = 0.2f; // ⏱️ Tần suất gửi attack request (200ms)

    private Animator animator;
    private Transform playerTransform;
    private float lastAttackTime = -999f;
    private MessageManager message;
    private List<Transform> enemiesInZone = new List<Transform>();

    private void Start()
    {
        if (message == null)
        {
            message = FindAnyObjectByType<MessageManager>();
        }
        playerTransform = transform.parent;
        animator = playerTransform.GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogWarning("⚠️ Không tìm thấy Animator!");
        }
    }

    private void Update()
    {
        if (enemiesInZone.Count > 0 && Time.time - lastAttackTime >= attackInterval)
        {
            Transform targetEnemy = enemiesInZone[0]; // 🎯 Luôn lấy enemy đầu tiên
            if (targetEnemy != null)
            {
                if (!GetEnemyActive(targetEnemy))
                {
                    enemiesInZone.Remove(targetEnemy);
                    return;
                }

                float dir = targetEnemy.position.x - playerTransform.position.x;
                Vector3 scale = playerTransform.Find("UnitRoot").localScale;

                // Đảo ngược hướng nhân vật nếu cần thiết
                if ((dir > 0 && scale.x > 0) || (dir < 0 && scale.x < 0))
                {
                    scale.x *= -1;
                    playerTransform.Find("UnitRoot").localScale = scale;
                }

                // ⚡ Gửi yêu cầu tấn công (server sẽ quyết định có hợp lệ không)
                MobData data = targetEnemy.GetComponent<MobData>();
                if (transform.parent.CompareTag("Player") && data != null)
                {
                    message.SendAttack(-111, data.id, transform.parent.GetComponent<SkillUse>().id_skill);
                }

                // Cập nhật thời gian gửi lần cuối
                lastAttackTime = Time.time;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsEnemy(other))
        {
            Transform enemyRoot = GetEnemyRoot(other);
            if (!enemiesInZone.Contains(enemyRoot))
            {
                enemiesInZone.Add(enemyRoot);
            }
        }
    }
    // Gọi khi server xác nhận attack hợp lệ
    public void PlayAttackAnimation(int enemyId, int id_skill)
    {
        // 🔍 Tìm enemy theo ID
        Transform targetEnemy = enemiesInZone.Find(e =>
        {
            MobData mob = e.GetComponent<MobData>();
            return mob != null && mob.id == enemyId;
        });

        if (targetEnemy == null)
        {
            return;
        }

        // 🔎 Lấy tất cả Animator của enemy
        var animators = targetEnemy.GetComponentsInChildren<Animator>(includeInactive: true);
        if (animators == null || animators.Length == 0)
            return;

        foreach (var anim in animators)
        {
            if (anim == null || anim.runtimeAnimatorController == null)
                continue;

            // ✅ Kiểm tra xem có trigger "Hit" trong Animator không
            if (HasParameter(anim, "Hit", AnimatorControllerParameterType.Trigger))
            {
                anim.SetTrigger("Hit");
            }
            var skill = "skill_0" + id_skill;
            if (HasParameter(anim, skill, AnimatorControllerParameterType.Trigger))
            {
                anim.SetTrigger(skill);
            }
        }
    }

    private bool HasParameter(Animator animator, string paramName, AnimatorControllerParameterType type)
    {
        foreach (var param in animator.parameters)
        {
            if (param.type == type && param.name == paramName)
                return true;
        }
        return false;
    }


    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsEnemy(other))
        {
            Transform enemyRoot = GetEnemyRoot(other);
            if (enemiesInZone.Contains(enemyRoot))
            {
                enemiesInZone.Remove(enemyRoot);
            }
        }
    }

    private bool IsEnemy(Collider2D other)
    {
        Transform current = other.transform;
        while (current != null)
        {
            if (current.CompareTag(enemyTag))
                return true;

            current = current.parent;
        }
        return false;
    }

    private bool GetEnemyActive(Transform other)
    {
        Transform current = other.transform;
        while (current != null)
        {
            MobData mob = current.GetComponent<MobData>();
            if (mob != null && mob.current_hp > 0)
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }

    private Transform GetEnemyRoot(Collider2D other)
    {
        Transform current = other.transform;
        while (current != null)
        {
            if (current.CompareTag(enemyTag))
                return current;

            current = current.parent;
        }
        return other.transform;
    }
}
