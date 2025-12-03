using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.EventSystems; // Cần để bỏ chọn nút

public class RadialMenuBridge : MonoBehaviour
{
    // Biến static để các script khác biết menu đang mở hay đóng
    public static bool IsMenuOpen = false;

    private PlayerController _localPlayer;

    [Header("UI Managers")]
    public PiUIManager piManager; 
    
    // Tên menu phải trùng trong Pi UI Manager
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

        // 3. Bật/Tắt Menu bằng Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Nếu menu đang mở (hoặc menu con đang mở) -> Đóng lại
            if (piManager.PiOpened(mainMenuName) || piManager.PiOpened(emoteMenuName))
            {
                CloseAllMenus(); 
            }
            else
            {
                // Nếu đang đóng -> Mở ra
                // (Kiểm tra xem có bảng UI nào khác đang chặn không)
                if (gameUIManager != null && gameUIManager.IsUIOpen()) return;

                OpenMenu();
            }
        }
    }

    // ========================================================
    // CÁC HÀM HÀNH ĐỘNG (GẮN VÀO PI UI)
    // ========================================================

    public void Action_UseItem()
    {
        if (_localPlayer) _localPlayer.TryUseItem(); 
        CloseAllMenus(); 
    }

    public void Action_Heal()
    {
        if (_localPlayer) _localPlayer.Rpc_UseShield(); // Đã đổi thành Shield
        CloseAllMenus(); 
    }

    public void Action_Cancel()
    {
        CloseAllMenus(); 
    }

    // --- HÀM MỞ MENU CON ---

    public void Action_OpenGuide()
    {
        CloseAllMenus(); // Đóng menu tròn
        if(gameUIManager) gameUIManager.OpenGuide(); // Mở bảng hướng dẫn
    }

    public void Action_OpenEmoteMenu()
    {
        // Tắt menu chính
        if(piManager.PiOpened(mainMenuName)) piManager.ChangeMenuState(mainMenuName);
        
        // Mở menu Emote
        piManager.ChangeMenuState(emoteMenuName, new Vector2(Screen.width / 2, Screen.height / 2));
        
        // Đảm bảo chuột vẫn mở
        UnlockCursor();
    }

    public void Action_BackToMain()
    {
        // Tắt menu Emote
        if(piManager.PiOpened(emoteMenuName)) piManager.ChangeMenuState(emoteMenuName);
        
        // Mở lại menu Chính
        OpenMenu();
    }

    // --- CÁC HÀM EMOTE ---
    public void Action_Emote_Smile() { PlayEmote(0); } 
    public void Action_Emote_Cry()   { PlayEmote(1); } 
    public void Action_Emote_Angry() { PlayEmote(2); } 
public void Action_Emote_3() { PlayEmote(3); } 
    public void Action_Emote_4() { PlayEmote(4); }
    void PlayEmote(int emoteID)
    {
        if (_localPlayer) _localPlayer.Rpc_PlayEmote(emoteID);
        CloseAllMenus(); 
    }

    // ========================================================
    // CÁC HÀM QUẢN LÝ ĐÓNG/MỞ (BẠN BỊ THIẾU PHẦN NÀY)
    // ========================================================

    void OpenMenu()
    {
        piManager.ChangeMenuState(mainMenuName, new Vector2(Screen.width / 2, Screen.height / 2));
        IsMenuOpen = true;
        UnlockCursor();
    }

    // Hàm này được gọi khi chọn xong: Đóng menu, nhưng KHÔNG khóa chuột (theo ý bạn)
    void CloseAllMenus()
    {
        // 1. Đóng các menu PiUI
        if(piManager.PiOpened(mainMenuName)) piManager.ChangeMenuState(mainMenuName);
        if(piManager.PiOpened(emoteMenuName)) piManager.ChangeMenuState(emoteMenuName);

        // 2. Bỏ chọn nút UI (Để tránh lỗi kẹt phím Enter)
        if (EventSystem.current != null) 
            EventSystem.current.SetSelectedGameObject(null);

        IsMenuOpen = false;

        // LƯU Ý: Không khóa chuột ở đây để giữ phong cách MMORPG
        // Chuột sẽ được PlayerController khóa khi bạn giữ Chuột Phải.
    }

    // Hàm hiện chuột
    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}