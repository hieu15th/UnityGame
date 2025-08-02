using System;
using System.Collections;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class NoteAddItem : MonoBehaviour
{
    [SerializeField] private GameObject alert; // GameObject alert
    [SerializeField] private GameObject alertPrefab; // Prefab sẽ được thêm vào alert
    [SerializeField] private TextMeshProUGUI item; // TextMeshProUGUI để hiển thị tên item
    private bool isScrolling = false;  // Cờ để theo dõi trạng thái lướt văn bản

    void Start()
    {
        alert.SetActive(false);       
    }

    public void handleAlert(byte[] data)
    {
        using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
        {
            // Đọc mã loại (identifier) (4 byte)
            int identifier = ReadInt32BigEndian(reader);

            // Đọc độ dài tên item (4 byte)
            int nameLength = ReadInt32BigEndian(reader);

            // Đọc nội dung chuỗi tên item
            byte[] msgBytes = reader.ReadBytes(nameLength);

            string itemName = Encoding.UTF8.GetString(msgBytes);  // Chuyển mảng byte thành chuỗi

            // Đọc ID của item (4 byte)
            int itemId = ReadInt32BigEndian(reader);

            // Kiểm tra xem đã có alertInstance trong alert chưa
            TextMeshProUGUI alertText = alert.GetComponentInChildren<TextMeshProUGUI>();
            GameObject alertInstance = null;
            if (alertText == null)
            {
                // Instantiate alertPrefab và lấy TextMeshProUGUI
                alertInstance = Instantiate(alertPrefab, alert.transform);
                alertText = alertInstance.GetComponent<TextMeshProUGUI>();
                alertInstance.SetActive(true);  // Hiển thị thông báo
            }
            else
            {
                alertInstance = alertText.gameObject;  // Nếu đã có, lấy gameObject hiện tại
                alertText.text += ",";
            }

            // Hiển thị tên item dựa trên itemId
            switch (itemId)
            {
                case -2:
                    alertText.text += $" {itemName} Vàng";  // Hiển thị tên item với "Vàng"
                    break;
                case -1:
                    alertText.text += $" {itemName} Kim cương";  // Hiển thị tên item với "Kim cương"
                    break;
                default:
                    alertText.text += $" {itemName}";  // Hiển thị tên item bình thường
                    break;
            }

            alert.SetActive(true);  // Hiển thị thông báo

            // Kiểm tra xem độ dài văn bản có vượt quá chiều rộng không
            if (alertText.preferredWidth > alertText.rectTransform.rect.width)
            {
                // Nếu văn bản dài hơn chiều rộng, bắt đầu lướt văn bản
                if (!isScrolling)
                {
                    isScrolling = true;
                    StartCoroutine(ScrollText(alertText, 100f));  // Lướt với vận tố
                }
            }
            else
            {
                StartCoroutine(HideAlertAfterTime(5f, alertText.gameObject));  // Hủy ngay lập tức
            }
        }
    }

    // Coroutine lướt văn bản với vận tốc cố định
    private IEnumerator ScrollText(TextMeshProUGUI alertText, float speed)
    {
        RectTransform rectTransform = alertText.rectTransform;
        Vector2 startPos = rectTransform.anchoredPosition;
        float totalWidth = alertText.preferredWidth;  // Chiều rộng của văn bản ban đầu

        // Lấy chiều rộng của vùng hiển thị (container của TextMeshProUGUI)
        float containerWidth = 0;

        // Đảm bảo không xuống dòng
        alertText.enableWordWrapping = false;

        // Tính toán vị trí bắt đầu (văn bản sẽ bắt đầu từ ngoài bên phải)
        float currentPosition = containerWidth;

        // Lướt văn bản từ phải sang trái
        while (currentPosition > -totalWidth)
        {
            // Kiểm tra nếu alertText đã bị destroy (ngừng di chuyển nếu đã bị hủy)
            if (alertText == null || alertText.gameObject == null)
            {
                yield break;  // Nếu alertText đã bị hủy, kết thúc Coroutine
            }

            // Cập nhật văn bản mới nếu có
            // (Ở đây, bạn có thể thay đổi điều kiện để phù hợp với yêu cầu của bạn)
            if (alertText.text.Length > 0)
            {
                totalWidth = alertText.preferredWidth;
            }

            currentPosition -= speed * Time.deltaTime;
            rectTransform.anchoredPosition = new Vector2(currentPosition, startPos.y);

            // Chờ một frame
            yield return null;
        }
        isScrolling = false; 
        StartCoroutine(HideAlertAfterTime(0f, alertText.gameObject));  // Hủy ngay lập tức
    }



    // Coroutine hủy thông báo sau một khoảng thời gian
    private IEnumerator HideAlertAfterTime(float time, GameObject alertInstance)
    {
        yield return new WaitForSeconds(time);  // Chờ trong thời gian 'time' giây\
        if (!isScrolling)
        {
            alert.SetActive(false);
            Destroy(alertInstance);
        }
    }


    // Hàm đọc Int32 theo định dạng Big Endian
    private int ReadInt32BigEndian(BinaryReader reader)
    {
        // Kiểm tra xem có đủ dữ liệu để đọc một Int32 (4 byte) không
        if (reader.BaseStream.Length - reader.BaseStream.Position < 4)
        {
            Debug.LogError($"Not enough data to read an Int32. Current Position: {reader.BaseStream.Position}, Stream Length: {reader.BaseStream.Length}");
            return 0;  // Trả về giá trị mặc định nếu không đủ dữ liệu
        }

        byte[] bytes = reader.ReadBytes(4);  // Đọc 4 byte từ stream
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);  // Đảo ngược mảng byte nếu hệ thống là Little Endian

        return BitConverter.ToInt32(bytes, 0);  // Chuyển mảng byte thành Int32
    }
}
