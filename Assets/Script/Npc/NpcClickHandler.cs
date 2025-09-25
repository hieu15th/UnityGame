using System;
using System.Net;
using UnityEngine;
using UnityEngine.EventSystems;

public class NpcClickHandler : MonoBehaviour
{
    private Transform player;
    private const float maxDistance = 0.8f;
    private const sbyte CMD_SEND_NPC = -114;

    void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("❌ Không tìm thấy Player trong scene!");
        }

        if (Camera.main == null)
        {
            Debug.LogError("❌ Camera.main bị null. Đảm bảo có Camera trong scene.");
        }
    }

    void Update()
    {
        if (player == null || Camera.main == null) return;

        HandleMouseClick();
        HandleEnterKey();
        AutoDisableChooseWhenTooFar();
    }

    /// <summary>
    /// Kiểm tra menu có đang bật không
    /// </summary>
    private bool IsMenuOpen()
    {
        var menu = GameObject.FindObjectOfType<MenuManager>();
        return menu != null && menu.gameObject.activeSelf;
    }

    private void HandleNpcHit(Transform npcTransform)
    {
        float distance = Vector2.Distance(npcTransform.position, player.position);

        if (distance > maxDistance)
        {
            //Debug.Log($"🚫 NPC quá xa (distance = {distance:F2}), không chọn được.");
            return;
        }

        Transform chooseTransform = npcTransform.Find("Choose");

        // Click lại chính NPC đang được chọn
        if (chooseTransform != null && chooseTransform.gameObject.activeSelf)
        {
            var npcInfo = npcTransform.GetComponent<NpcInfo>();
            if (npcInfo != null)
            {
                SendNpcRequest(npcInfo.id);
            }
            return;
        }

        // Tắt tất cả choose
        foreach (var npc in GameObject.FindGameObjectsWithTag("Npc"))
        {
            var ch = npc.transform.Find("Choose");
            if (ch != null) ch.gameObject.SetActive(false);
        }

        // Bật choose cho NPC mới
        if (chooseTransform != null)
        {
            chooseTransform.gameObject.SetActive(true);
            //Debug.Log($"✅ Bật choose của NPC: {npcTransform.name}");
        }
    }

    private void HandleMouseClick()
    {
        // 🚫 Nếu menu đang mở thì không xử lý click NPC
        if (IsMenuOpen()) return;

        // 🚫 Nếu click trúng UI thì bỏ qua
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (!Input.GetMouseButtonDown(0)) return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Collider2D[] hits = Physics2D.OverlapPointAll(mousePos);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Npc"))
            {
                HandleNpcHit(hit.transform);
                return;
            }
        }
    }

    private void HandleEnterKey()
    {
        // 🚫 Nếu menu đang mở thì bỏ qua Enter
        if (IsMenuOpen()) return;

        if (!Input.GetKeyDown(KeyCode.Return)) return;

        foreach (var npc in GameObject.FindGameObjectsWithTag("Npc"))
        {
            Transform choose = npc.transform.Find("Choose");
            if (choose != null && choose.gameObject.activeSelf)
            {
                var info = npc.GetComponent<NpcInfo>();
                if (info != null)
                {
                    SendNpcRequest(info.id);
                }
            }
        }
    }

    private void AutoDisableChooseWhenTooFar()
    {
        foreach (var npc in GameObject.FindGameObjectsWithTag("Npc"))
        {
            Transform choose = npc.transform.Find("Choose");
            if (choose != null && choose.gameObject.activeSelf)
            {
                float distance = Vector2.Distance(npc.transform.position, player.position);
                if (distance > maxDistance)
                {
                    choose.gameObject.SetActive(false);
                    Debug.Log($"❌ Tắt choose NPC {npc.name} do quá xa (distance = {distance:F2})");
                }
            }
        }
    }

    private void SendNpcRequest(int npcId)
    {
        try
        {
            var writer = SocketManager.Instance.Writer;
            if (writer == null)
            {
                Debug.LogError("❌ Writer chưa khởi tạo.");
                return;
            }

            writer.Write(CMD_SEND_NPC);

            ushort length = 4;
            writer.Write((byte)(length >> 8));
            writer.Write((byte)(length & 0xFF));

            writer.Write(IPAddress.HostToNetworkOrder(npcId)); // int 4 bytes
            writer.Flush();

            //Debug.Log($"📤 Gửi CMD_SEND_NPC: npcId = {npcId}");
        }
        catch (Exception ex)
        {
            Debug.LogError("Lỗi khi gửi CMD_SEND_NPC: " + ex.Message);
        }
    }
}
