using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 1f;
    private Rigidbody2D rb;
    private Vector2 movement;
    private Vector2 lastSentPosition;
    private Animator animator;
    private Transform nameTransform, healthTransform;
    private Vector3 healthOriginalLocalPos;
    private float attackCooldown = 0f;
    public float attackDelay = 1f;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        nameTransform = transform.Find("Name");

        var transforms = GetComponentsInChildren<Transform>(true);
        healthTransform = transforms.FirstOrDefault(t => t.CompareTag("Health"));
        if (healthTransform == null)
        {
            Debug.LogWarning("[DEBUG] Health not found with tag 'Health' in children.");
        }
        else
        {
            healthOriginalLocalPos = healthTransform.localPosition;
        }
    }

    void Update()
    {
        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        if ((Mouse.current.leftButton.wasPressedThisFrame || Input.GetMouseButtonDown(0)) && CanMove() && attackCooldown <= 0f)
        {
            if (animator != null)
                animator.SetTrigger("2_Attack");

            Debug.Log("🗡️ Attack triggered!");
            attackCooldown = attackDelay;
        }
        // ❌ Nếu có UI_Bag bật thì không cho di chuyển
        if (!CanMove())
        {
            movement = Vector2.zero;
            if (animator != null)
                animator.SetBool("1_Move", false);
            return;
        }

        // ✅ Cho phép di chuyển nếu không bị chặn bởi UI
        movement = Vector2.zero;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            movement.y += 1;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            movement.y -= 1;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            movement.x -= 1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            movement.x += 1;

        if (animator != null)
        {
            bool isMoving = movement.magnitude > 0;
            animator.SetBool("1_Move", isMoving);
        }

        if (movement.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(-movement.x);
            transform.localScale = scale;
        }

        if (nameTransform != null)
        {
            Vector3 scale = nameTransform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(transform.localScale.x);
            nameTransform.localScale = scale;
            nameTransform.localRotation = Quaternion.identity;
        }

        if (healthTransform != null)
        {
            // Scale luôn dương
            Vector3 scale = healthTransform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(transform.localScale.x);
            healthTransform.localScale = scale;
            healthTransform.localRotation = Quaternion.identity;
            Vector3 pos = healthOriginalLocalPos;
            pos.x = pos.x * Mathf.Sign(transform.localScale.x);
            healthTransform.localPosition = pos;
        }

    }

    void FixedUpdate()
    {
        if (movement == Vector2.zero) return;

        Vector2 newPosition = rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        float distance = Vector2.Distance(newPosition, lastSentPosition);
        float speed = distance / Time.fixedDeltaTime;

        if (distance > 0.05f && speed > 0.5f)
        {
            lastSentPosition = newPosition;
            SendPositionToServer(newPosition.x, newPosition.y);
        }
    }

    private void SendPositionToServer(float x, float y)
    {
        try
        {
            var writer = SocketManager.Instance.Writer;
            if (writer == null) return;

            writer.Write((byte)0x84);
            ushort payloadSize = 8;
            writer.Write((byte)((payloadSize >> 8) & 0xFF));
            writer.Write((byte)(payloadSize & 0xFF));

            byte[] xBytes = BitConverter.GetBytes(x);
            byte[] yBytes = BitConverter.GetBytes(y);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(xBytes);
                Array.Reverse(yBytes);
            }

            writer.Write(xBytes);
            writer.Write(yBytes);
            writer.Flush();
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ Không gửi được tọa độ: " + ex.Message);
        }
    }

    /// <summary>
    /// ✅ Kiểm tra nếu có UI nào có tag "ui_bag" đang bật thì được phép di chuyển.
    /// Nếu không có UI nào bật → chặn di chuyển.
    /// </summary>
    private bool CanMove()
    {
        var uiBags = GameObject.FindGameObjectsWithTag("UI_Bag");

        foreach (var ui in uiBags)
        {
            if (ui.activeInHierarchy)
            {
                return true; 
            }
        }

        return false;
    }
}
