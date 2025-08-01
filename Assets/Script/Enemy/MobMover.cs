using UnityEngine;

public class MobMover : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float moveDuration = 0.5f;
    private float moveTimer = 0f;
    private bool isMoving = false;
    private bool facingRight = true;

    public void SetTargetPosition(float x, float y)
    {
        startPosition = transform.position;
        targetPosition = new Vector3(x, y, 0);
        moveTimer = 0f;
        isMoving = true;

        float dx = targetPosition.x - transform.position.x;
        if (dx < -0.01f && facingRight) Flip();
        else if (dx > 0.01f && !facingRight) Flip();
    }

    private void Update()
    {
        MobData mob = GetComponent<MobData>();
        if (mob != null)
        {
            if (mob.current_hp != mob.hp)
            {
                GameObject e = transform.Find("Heath_Bar")?.gameObject;
                if (e != null)
                {
                    if (!e.activeSelf)
                    {
                        e.SetActive(true);
                    }
                }
            }
        }

        if (!isMoving) return;

        moveTimer += Time.deltaTime;
        float t = Mathf.Clamp01(moveTimer / moveDuration);
        transform.position = Vector3.Lerp(startPosition, targetPosition, t);

        if (t >= 1f)
        {
            isMoving = false;
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.Find("Enemy").localScale;
        scale.x *= -1;
        transform.Find("Enemy").localScale = scale;
    }
}
