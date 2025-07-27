using UnityEngine;

public class MoveToTarget : MonoBehaviour
{
    public Transform objA;         // Điểm bắt đầu
    public Transform objB;         // Điểm đích
    public GameObject objectToEnable; // Đối tượng cần kiểm tra và bật khi đến nơi
    public float duration = 2f;    // Thời gian di chuyển (giây)
    public float stopDistance = 0.01f; // Khoảng cách gần đủ để coi là tới nơi

    private Vector3 targetPosition;
    private float speed;
    private bool arrived = false; // Đảm bảo chỉ xử lý 1 lần

    void Start()
    {
        if (objA != null && objB != null)
        {
            transform.position = objA.position;
            targetPosition = objB.position;

            float distance = Vector3.Distance(objA.position, objB.position);
            speed = distance / duration;
        }
    }

    void Update()
    {
        if (objB != null && !arrived)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) <= stopDistance)
            {
                arrived = true; // Đánh dấu đã đến để tránh thực hiện nhiều lần
                gameObject.SetActive(false); // Ẩn chính nó

                if (objectToEnable != null && !objectToEnable.activeSelf)
                {
                    objectToEnable.SetActive(true); // Bật đối tượng khác lên nếu đang tắt
                }
            }
        }
    }
}
