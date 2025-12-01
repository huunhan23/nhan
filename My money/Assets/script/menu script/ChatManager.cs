using Fusion;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ChatManager : NetworkBehaviour
{
    [Header("UI References")]
    private TMP_InputField chatInput;
    private Transform chatContent;
    private ScrollRect chatScrollRect;
    
    [Header("Assets")]
    public GameObject chatMessagePrefab;

    private PlayerController _playerController;
    
    // BIẾN MỚI ĐỂ SỬA LỖI
    private bool _justSubmitted = false; 

    public override void Spawned()
    {
        _playerController = GetComponent<PlayerController>();

        GameObject contentObj = GameObject.Find("Content");
        if (contentObj != null)
        {
            chatContent = contentObj.transform;
            chatScrollRect = contentObj.GetComponentInParent<ScrollRect>();
        }

        if (Object.HasInputAuthority)
        {
            GameObject inputObj = GameObject.Find("ChatInput");
            if (inputObj != null)
            {
                chatInput = inputObj.GetComponent<TMP_InputField>();
                chatInput.onSubmit.AddListener(OnSubmitChat);
                chatInput.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (!Object.HasInputAuthority || chatInput == null) return;

        // Kiểm tra phím Enter
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            // === SỬA LỖI TẠI ĐÂY ===
            // Nếu vừa mới Gửi tin nhắn (Submit) trong frame này -> Bỏ qua, không bật lại
            if (_justSubmitted) 
            {
                _justSubmitted = false; // Reset lại cho lần sau
                return;
            }
            // =======================

            // Nếu chat đang TẮT -> BẬT lên
            if (chatInput.gameObject.activeSelf == false)
            {
                chatInput.gameObject.SetActive(true);
                chatInput.ActivateInputField();
                
                // Xóa ký tự Enter thừa
                StartCoroutine(ClearEnterChar());
            }
        }
        
        // Phím ESC để hủy chat
        if (Input.GetKeyDown(KeyCode.Escape) && chatInput.gameObject.activeSelf)
        {
            CloseChat();
        }
    }

    IEnumerator ClearEnterChar()
    {
        yield return null;
        chatInput.text = "";
    }

    // Hàm này chạy khi nhấn Enter trong ô chat
    private void OnSubmitChat(string message)
    {
        // 1. Đánh dấu là "Vừa mới gửi" để hàm Update không mở lại chat
        _justSubmitted = true;

        // 2. Đóng Chat trước
        CloseChat();

        // 3. Gửi tin nhắn (nếu có chữ)
        if (!string.IsNullOrWhiteSpace(message))
        {
            string name = "Unknown";
            if (_playerController != null) name = _playerController.PlayerName.ToString();
            Rpc_SendChatMessage(message, name);
        }
    }

    private void CloseChat()
    {
        if (chatInput != null)
        {
            chatInput.text = ""; 
            chatInput.DeactivateInputField(); 
            chatInput.gameObject.SetActive(false); // Tắt UI
            
            // Trả quyền điều khiển cho Game
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    // ================= RPCs =================
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_SendChatMessage(string message, string senderName)
    {
        Rpc_BroadcastChatMessage(message, senderName);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_BroadcastChatMessage(string message, string senderName)
    {
        if (chatContent != null && chatMessagePrefab != null)
        {
            GameObject msgObj = Instantiate(chatMessagePrefab, chatContent);
            TextMeshProUGUI textData = msgObj.GetComponent<TextMeshProUGUI>();
            textData.text = $"<color=yellow><b>{senderName}:</b></color> {message}";
            StartCoroutine(AutoScrollToBottom());
        }
    }

    IEnumerator AutoScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)chatContent);
        }
    }
}