using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;

public class NoteAddItem : MonoBehaviour
{
    [SerializeField] private GameObject alert; // GameObject alert
    [SerializeField] private GameObject alertPrefab; // Prefab sẽ được thêm vào alert
    [SerializeField] private TextMeshProUGUI item; // TextMeshProUGUI để hiển thị tên item

    private bool isScrolling = false;
    private bool isProcessing = false; // đang hiển thị alert 1?
    private bool isShowingItem = false; // đang hiển thị alert 0?
    private Queue<byte[]> alertQueue = new Queue<byte[]>(); // hàng chờ cho identifier=1

    void Start()
    {
        alert.SetActive(false);
    }

    public void handleAlert(byte[] data)
    {
        using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
        {
            int identifier = ReadInt32BigEndian(reader);

            if (identifier == 0)
            {
                HandleItemAlert(reader);
            }
            else if (identifier == 2)
            {
                if (isShowingItem || isProcessing) // nếu đang hiển thị 0 hoặc 1 thì cho vào queue
                {
                    alertQueue.Enqueue(data);
                }
                else
                {
                    HandleMessageAlert(reader);
                }
            }
        }
    }

    private void HandleItemAlert(BinaryReader reader)
    {
        int nameLength = ReadInt32BigEndian(reader);
        byte[] msgBytes = reader.ReadBytes(nameLength);
        string itemName = Encoding.UTF8.GetString(msgBytes);

        int itemId = ReadInt32BigEndian(reader);

        TextMeshProUGUI alertText = alert.GetComponentInChildren<TextMeshProUGUI>();
        GameObject alertInstance = null;

        if (alertText == null)
        {
            alertInstance = Instantiate(alertPrefab, alert.transform);
            alertText = alertInstance.GetComponent<TextMeshProUGUI>();
            alertInstance.SetActive(true);
        }
        else
        {
            alertInstance = alertText.gameObject;
            alertText.text += ",";
        }

        switch (itemId)
        {
            case -2: alertText.text += $" {itemName} Vàng"; break;
            case -1: alertText.text += $" {itemName} Kim cương"; break;
            default: alertText.text += $" {itemName}"; break;
        }

        alert.SetActive(true);
        isShowingItem = true; // đánh dấu đang hiển thị loại 0

        if (alertText.preferredWidth > alertText.rectTransform.rect.width)
        {
            if (!isScrolling)
            {
                isScrolling = true;
                StartCoroutine(ScrollText(alertText, 100f, isItem: true));
            }
        }
        else
        {
            StartCoroutine(HideAlertAfterTime(5f, alertText.gameObject, isItem: true));
        }
    }

    private void HandleMessageAlert(BinaryReader reader)
    {
        int nameLength = ReadInt32BigEndian(reader);
        byte[] msgBytes = reader.ReadBytes(nameLength);
        string alertMessage = Encoding.UTF8.GetString(msgBytes);

        GameObject alertInstance = Instantiate(alertPrefab, alert.transform);
        TextMeshProUGUI alertText = alertInstance.GetComponent<TextMeshProUGUI>();
        alertInstance.SetActive(true);
        alertText.text = alertMessage;

        alert.SetActive(true);
        isProcessing = true;

        if (alertText.preferredWidth > alertText.rectTransform.rect.width)
        {
            if (!isScrolling)
            {
                isScrolling = true;
                StartCoroutine(ScrollText(alertText, 100f, isItem: false));
            }
        }
        else
        {
            StartCoroutine(HideAlertAfterTime(5f, alertText.gameObject, isItem: false));
        }
    }

    private IEnumerator ScrollText(TextMeshProUGUI alertText, float speed, bool isItem)
    {
        RectTransform rectTransform = alertText.rectTransform;
        Vector2 startPos = rectTransform.anchoredPosition;
        float totalWidth = alertText.preferredWidth;
        float containerWidth = alertText.rectTransform.rect.width;

        alertText.enableWordWrapping = false;

        float currentPosition;

        if (!isItem)
        {
            // ✅ identifier = 0 → bắt đầu hiển thị từ bên trái (trong container)
            currentPosition = containerWidth;
        }
        else
        {
            // ✅ identifier = 1 → bắt đầu từ ngoài bên phải
            currentPosition = 0f;
        }

        while (currentPosition > -totalWidth)
        {
            if (alertText == null || alertText.gameObject == null) yield break;

            if (alertText.text.Length > 0)
                totalWidth = alertText.preferredWidth;

            // luôn chạy từ phải → trái
            currentPosition -= speed * Time.deltaTime;
            rectTransform.anchoredPosition = new Vector2(currentPosition, startPos.y);

            yield return null;
        }

        isScrolling = false;
        StartCoroutine(HideAlertAfterTime(0f, alertText.gameObject, isItem));
    }


    private IEnumerator HideAlertAfterTime(float time, GameObject alertInstance, bool isItem)
    {
        yield return new WaitForSeconds(time);
        if (!isScrolling)
        {
            alert.SetActive(false);
            Destroy(alertInstance);

            if (isItem) isShowingItem = false;
            else isProcessing = false;

            // Sau khi xong thì xử lý queue nếu có
            if (!isShowingItem && !isProcessing && alertQueue.Count > 0)
            {
                byte[] next = alertQueue.Dequeue();
                using (BinaryReader reader = new BinaryReader(new MemoryStream(next)))
                {
                    int identifier = ReadInt32BigEndian(reader);
                    if (identifier == 1)
                        HandleMessageAlert(reader);
                }
            }
        }
    }

    private int ReadInt32BigEndian(BinaryReader reader)
    {
        if (reader.BaseStream.Length - reader.BaseStream.Position < 4)
        {
            Debug.LogError($"Not enough data to read an Int32. Current Position: {reader.BaseStream.Position}, Stream Length: {reader.BaseStream.Length}");
            return 0;
        }
        byte[] bytes = reader.ReadBytes(4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }
}
