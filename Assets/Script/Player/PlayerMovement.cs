using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 1f;
    public bool keyboardTakesPriority = true;  // Bàn phím ưu tiên hơn joystick

    [Header("Combat (nếu dùng)")]
    public float attackDelay = 1f;
    private float attackCooldown = 0f;

    [Header("Refs")]
    [SerializeField] private MessageManager mess;
    [SerializeField] private UIMoveJoystick joy;

    // Privates
    private Rigidbody2D rb;
    private Vector2 movement;              // HƯỚNG đã chuẩn hoá (unit vector) hoặc (0,0)
    private Vector2 lastSentPosition;
    private Animator animator;
    private Transform player;              // "UnitRoot" hoặc chính transform

    void Start()
    {
        // Tìm MessageManager nếu chưa gán
        if (mess == null)
        {
            mess = FindFirstObjectByType<MessageManager>();
            if (mess == null)
            {
                Debug.LogError("❌ Không tìm thấy MessageManager trong scene.");
                // Không return để vẫn cho phép chạy local không network
            }
        }

        // Tìm Joystick nếu chưa gán (cẩn thận scene có nhiều joystick)
        if (joy == null)
        {
            joy = FindFirstObjectByType<UIMoveJoystick>();
            if (joy == null)
            {
                Debug.LogWarning("⚠️ Không tìm thấy UIMoveJoystick trong scene. Vẫn có thể chơi bằng bàn phím.");
            }
        }

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        player = transform.Find("UnitRoot");
        if (player == null) player = transform; // fallback
    }

    void Update()
    {
        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        if (!joy.gameObject.activeSelf)
        {
            movement = Vector2.zero;
            if (animator) animator.SetBool("1_Move", false);
            return;
        }

        // ---- 1) Bàn phím
        Vector2 kb = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) kb.y += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) kb.y -= 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) kb.x -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) kb.x += 1;
        }

        // ---- 2) Joystick (UIMoveJoystick đã có deadZone nội bộ)
        Vector2 js = Vector2.zero;
        if (joy != null)
        {
            js = joy.Direction; // -1..1 theo từng trục; length <= 1
        }

        // ---- 3) Hợp nhất input
        Vector2 rawMove = keyboardTakesPriority
            ? (kb != Vector2.zero ? kb : js)
            : (js != Vector2.zero ? js : kb);

        // ✅ Luôn CHUẨN HOÁ để tốc độ = moveSpeed, kể cả kéo nhẹ joy
        movement = rawMove.sqrMagnitude > 0f ? rawMove.normalized : Vector2.zero;

        // ---- 4) Animator + lật hướng
        if (animator) animator.SetBool("1_Move", movement.sqrMagnitude > 0f);

        if (movement.x != 0f && player != null)
        {
            var scale = player.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(-movement.x);
            player.localScale = scale;
        }
    }

    void FixedUpdate()
    {
        // movement đã normalized ở Update()
        Vector2 newPosition = rb.position + movement * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        if (movement != Vector2.zero)
        {
            lastSentPosition = newPosition;
            SendPositionToServer(newPosition.x, newPosition.y);
        }
    }

    private void SendPositionToServer(float x, float y)
    {
        if (mess == null) return; // Cho phép chạy offline
        try
        {
            mess.SendRequest(-115);
            var writer = SocketManager.Instance?.Writer;
            if (writer == null) return;

            writer.Write((byte)0x84);
            const ushort payloadSize = 8;
            writer.Write((byte)((payloadSize >> 8) & 0xFF));
            writer.Write((byte)(payloadSize & 0xFF));

            byte[] xBytes = BitConverter.GetBytes(x);
            byte[] yBytes = BitConverter.GetBytes(y);
            if (!BitConverter.IsLittleEndian) { Array.Reverse(xBytes); Array.Reverse(yBytes); }

            writer.Write(xBytes);
            writer.Write(yBytes);
            writer.Flush();
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ Không gửi được tọa độ: " + ex.Message);
        }
    }
}
