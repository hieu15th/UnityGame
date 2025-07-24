using UnityEngine;

public class SpriteMover : MonoBehaviour
{
    [SerializeField] private float moveDistancePixels = 10f; // Khoảng cách di chuyển (pixel)
    [SerializeField] private float moveSpeedPixels = 50f; // Tốc độ di chuyển (pixel/giây)
    [SerializeField] private float pixelsPerUnit = 100f; // Pixels Per Unit của Sprite (kiểm tra trong Inspector)

    private Vector3 startPosition; // Vị trí ban đầu
    private bool movingRight = true; // Hướng di chuyển

    void Start()
    {
        startPosition = transform.position; // Lưu vị trí ban đầu
        if (!GetComponent<SpriteRenderer>())
        {
            Debug.LogError("SpriteMover: GameObject cần có SpriteRenderer!");
            enabled = false;
        }
    }

    void Update()
    {
        // Chuyển pixel sang unit
        float moveDistanceUnits = moveDistancePixels / pixelsPerUnit;
        float moveSpeedUnits = moveSpeedPixels / pixelsPerUnit;

        // Tính toán vị trí mục tiêu
        float targetX = movingRight ? startPosition.x + moveDistanceUnits : startPosition.x - moveDistanceUnits;
        Vector3 targetPosition = new Vector3(targetX, startPosition.y, startPosition.z);

        // Di chuyển mượt mà
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeedUnits * Time.deltaTime
        );

        // Đổi hướng khi đến đích
        if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
        {
            movingRight = !movingRight;
        }
    }
}