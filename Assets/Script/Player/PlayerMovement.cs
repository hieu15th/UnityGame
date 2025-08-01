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
    private Transform player;
    private float attackCooldown = 0f;
    public float attackDelay = 1f;
    [SerializeField] private MessageManager mess;

    void Start()
    {
        if (mess == null)
        {
            mess = FindFirstObjectByType<MessageManager>();

            if (mess == null)
            {
                Debug.LogError("❌ Không tìm thấy MessageManager trong scene.");
                return;
            }
        }
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        player = transform.Find("UnitRoot");
    }

    void Update()
    {
        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

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
            Vector3 scale = player.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(-movement.x);
            player.localScale = scale;
        }
    }

    void FixedUpdate()
    {

        Vector2 newPosition = rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        if (movement != Vector2.zero)
        {
            lastSentPosition = newPosition;
            SendPositionToServer(newPosition.x, newPosition.y);
        }

    }


    private void SendPositionToServer(float x, float y)
    {
        try
        {
            mess.SendRequest(-115);
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

    private bool CanMove()
    {
        bool isBagOpen = false;
        bool isMenuOpen = false;

        var bagUIs = GameObject.FindGameObjectsWithTag("UI_Bag");
        foreach (var ui in bagUIs)
        {
            if (ui.activeInHierarchy)
            {
                isBagOpen = true;
                break;
            }
        }

        var menuUIs = GameObject.FindGameObjectsWithTag("Menu");
        foreach (var ui in menuUIs)
        {
            if (ui.activeInHierarchy)
            {
                isMenuOpen = true;
                break;
            }
        }

        // ✅ Chỉ được di chuyển nếu: Bag mở & Menu đóng
        return isBagOpen && !isMenuOpen;
    }


}
