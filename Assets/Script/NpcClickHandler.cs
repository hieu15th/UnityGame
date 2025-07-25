using System;
using System.Net;
using UnityEngine;

public class NpcClickHandler : MonoBehaviour
{
    private Transform player; // lưu vị trí player
    private const float maxDistance = 0.8f;
    private const sbyte CMD_SEND_NPC = -114;

    private void Start()
    {
        // Giả sử Player có tag "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("❌ Không tìm thấy Player!");
        }
    }

   

    private void Update()
    {
        if (player == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null && hit.collider.CompareTag("Npc"))
            {
                float distance = Vector2.Distance(hit.transform.position, player.position);
                if (distance > maxDistance)
                {
                    Debug.Log($"🚫 NPC quá xa (distance = {distance:F2}), không chọn được.");
                    return;
                }

                Transform chooseTransform = hit.transform.Find("Choose");

                // 🔍 Nếu Choose đang bật (tức là người dùng click lại chính NPC đang chọn)
                if (chooseTransform != null && chooseTransform.gameObject.activeSelf)
                {
                    var npcInfo = hit.transform.GetComponent<NpcInfo>();
                    if (npcInfo != null)
                    {
                        SendMessageToServer(npcInfo.id);
                    }
                    return; // Không xử lý bật lại nữa
                }

                // Tắt tất cả choose (sau khi kiểm tra trên)
                foreach (var npc in GameObject.FindGameObjectsWithTag("Npc"))
                {
                    Transform ch = npc.transform.Find("Choose");
                    if (ch != null) ch.gameObject.SetActive(false);
                }

                // Bật choose của NPC mới
                if (chooseTransform != null)
                {
                    chooseTransform.gameObject.SetActive(true);
                    Debug.Log($"✅ Bật choose của NPC: {hit.transform.name}");
                }
            }
        }


        // Gửi khi nhấn Enter nếu có NPC đang được chọn (choose đang bật)
        if (Input.GetKeyDown(KeyCode.Return))
        {
            foreach (var npc in GameObject.FindGameObjectsWithTag("Npc"))
            {
                Transform chooseTransform = npc.transform.Find("Choose");
                if (chooseTransform != null && chooseTransform.gameObject.activeSelf)
                {
                    var npcInfo = npc.GetComponent<NpcInfo>();
                    if (npcInfo != null)
                    {
                        SendMessageToServer(npcInfo.id);
                    }
                }
            }
        }

        // Tắt Choose nếu đi xa
        foreach (var npc in GameObject.FindGameObjectsWithTag("Npc"))
        {
            Transform chooseTransform = npc.transform.Find("Choose");
            if (chooseTransform != null && chooseTransform.gameObject.activeSelf)
            {
                float distance = Vector2.Distance(npc.transform.position, player.position);
                if (distance > maxDistance)
                {
                    chooseTransform.gameObject.SetActive(false);
                    Debug.Log($"❌ Tắt choose NPC {npc.name} do quá xa (distance = {distance:F2})");
                }
            }
        }
    }

    private void SendMessageToServer(int id)
    {
        try
        {
            var writer = SocketManager.Instance.Writer;
            if (writer == null)
            {
                Debug.LogError("Writer chưa khởi tạo.");
                return;
            }

            writer.Write(CMD_SEND_NPC);

            ushort length = 4;
            writer.Write((byte)(length >> 8)); // byte cao
            writer.Write((byte)(length & 0xFF)); // byte thấp

            // Ghi ID (4 byte)
            writer.Write(IPAddress.HostToNetworkOrder(id)); // đảm bảo big-endian

            writer.Flush();
            Debug.Log($"📨 Đã gửi CMD_SEND_NPC với ID = {id}");
        }
        catch (Exception ex)
        {
            Debug.LogError("Lỗi khi gửi CMD_SEND_NPC: " + ex.Message);
        }
    }

}
