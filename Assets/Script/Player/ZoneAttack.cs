using System.Collections.Generic;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

public class ZoneAttack : MonoBehaviour
{
    public string enemyTag = "Enemy";
    public float attackCooldown = 5f;

    private Animator animator;
    private Transform playerTransform;
    private float lastAttackTime = -999f;
    private MessageManager message;
    private List<Transform> enemiesInZone = new List<Transform>();

    private void Start()
    {
        if(message == null)
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

            if (enemiesInZone.Count > 0 && Time.time - lastAttackTime >= attackCooldown)
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

                    // Kích hoạt trigger tấn công
                    animator?.SetTrigger("2_Attack");

                    // Cập nhật thời gian tấn công
                    lastAttackTime = Time.time;
                    var animatorEnemy = targetEnemy.GetComponentInChildren<Animator>();
                    MobData data = targetEnemy.GetComponent<MobData>();
                    animatorEnemy?.SetTrigger("Hit");
                    if (transform.parent.CompareTag("Player"))  // Kiểm tra nếu đối tượng là "Player"
                    {
                        message.SendAttack(-111, data.id);
                    }
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

    // Kiểm tra tag Enemy trong hierarchy
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

    // Trả về object có tag Enemy (dù là cha)
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
