using UnityEngine;
using Fusion;
using TMPro; 
using UnityEngine.EventSystems; // <--- 1. QUAN TRỌNG: THÊM DÒNG NÀY

public class RadialMenuBridge : MonoBehaviour
{
    private PlayerController _localPlayer;

    [Header("UI Managers")]
    public PiUIManager piManager; 
    public string mainMenuName = "PlayerMenu"; 
    public string emoteMenuName = "EmoteMenu"; 

    [Header("UI Elements")]
    public TextMeshProUGUI itemCountText;
    public GameUIManager gameUIManager;

    void Update()
    {
        // 1. Tìm Player
        if (_localPlayer == null)
        {
            var players = FindObjectsOfType<PlayerController>();
            foreach (var p in players)
            {
                if (p.Object != null && p.Object.HasInputAuthority)
                {
                    _localPlayer = p;
                    break;
                }
            }
        }

        // 2. Cập nhật số lượng Item
        if (_localPlayer != null && itemCountText != null)
        {
             itemCountText.text = "x" + _localPlayer.itemCount;
        }

        // 3. Bật Menu Chính bằng Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Đảm bảo không có UI nào khác đang chặn (ví dụ bảng Guide)
            if (gameUIManager != null && gameUIManager.IsUIOpen()) return;

            // Reset về menu chính
            if(piManager.PiOpened(emoteMenuName)) piManager.ChangeMenuState(emoteMenuName); // Tắt menu con nếu có
            
            // Bật menu chính
            piManager.ChangeMenuState(mainMenuName, new Vector2(Screen.width / 2, Screen.height / 2));
            
            UnlockCursor(); // Hiện chuột để chọn
        }
        
    }

    // ========================================================
    // CÁC HÀM HÀNH ĐỘNG (GỌI TỪ PI UI)
    // ========================================================

    // --- NHÓM 1: CÁC HÀNH ĐỘNG XONG LÀ QUAY VỀ GAME (ẨN CHUỘT) ---

    public void Action_UseItem()
    {
        if (_localPlayer) _localPlayer.TryUseItem(); 
        CloseAllMenus();
        // Xong việc -> Khóa chuột
    }

    public void Action_Heal()
    {
        if (_localPlayer) _localPlayer.Rpc_UseHeal(); 
        CloseAllMenus();
         // Xong việc -> Khóa chuột
    }

    public void Action_Cancel()
    {
        CloseAllMenus(); // Hủy -> Khóa chuột
    }

    // Các hàm Emote con
    public void Action_Emote_Smile() { PlayEmote(0); }
    public void Action_Emote_Cry()   { PlayEmote(1); }
    public void Action_Emote_Angry() { PlayEmote(2); }

    void PlayEmote(int emoteID)
    {
        if (_localPlayer) _localPlayer.Rpc_PlayEmote(emoteID);
        CloseAllMenus();
        // Chọn emote xong -> Khóa chuột
    }


    // --- NHÓM 2: CÁC HÀNH ĐỘNG MỞ MENU KHÁC (GIỮ CHUỘT) ---

    public void Action_OpenGuide()
    {
        // Tắt menu tròn (nhưng không khóa chuột)
        if(piManager.PiOpened(mainMenuName)) piManager.ChangeMenuState(mainMenuName);
        if(piManager.PiOpened(emoteMenuName)) piManager.ChangeMenuState(emoteMenuName);
        
        // Mở bảng hướng dẫn
        if(gameUIManager) gameUIManager.OpenGuide();
    }

    public void Action_OpenEmoteMenu()
    {
        // Tắt menu chính
        if(piManager.PiOpened(mainMenuName)) piManager.ChangeMenuState(mainMenuName);
        
        // Mở menu Emote
        piManager.ChangeMenuState(emoteMenuName, new Vector2(Screen.width / 2, Screen.height / 2));
        
        // Vẫn giữ chuột để chọn tiếp
        UnlockCursor(); 
    }

    public void Action_BackToMain()
    {
        // Tắt menu Emote
        if(piManager.PiOpened(emoteMenuName)) piManager.ChangeMenuState(emoteMenuName);
        
        // Mở lại menu Chính
        piManager.ChangeMenuState(mainMenuName, new Vector2(Screen.width / 2, Screen.height / 2));
        
        // Vẫn giữ chuột
        UnlockCursor();
    }

    // ========================================================
    // HÀM QUẢN LÝ CHUỘT (QUAN TRỌNG)
    // ========================================================

    // Hàm đóng tất cả và KHÓA CHUỘT (Về chế độ bắn súng)
   void CloseAllMenus()
    {
        // 1. Đóng các menu PiUI
        if(piManager.PiOpened(mainMenuName)) piManager.ChangeMenuState(mainMenuName);
        if(piManager.PiOpened(emoteMenuName)) piManager.ChangeMenuState(emoteMenuName);

        // 2. Bỏ chọn nút UI (Để tránh lỗi kẹt phím Enter/Space vào nút vừa bấm)
        if (UnityEngine.EventSystems.EventSystem.current != null) 
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        // 3. TUYỆT ĐỐI KHÔNG CÓ DÒNG KHÓA CHUỘT Ở ĐÂY
        // Xóa các dòng: Cursor.lockState = ...; Cursor.visible = ...;
    }

    // Hàm mở chuột (Để chọn menu)
    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}