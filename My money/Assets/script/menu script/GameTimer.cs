using Fusion;
using UnityEngine;
using System.Linq; // Để dùng FindObjectsOfType

public class GameTimer : NetworkBehaviour
{
    [Header("Time Settings")]
    public float matchDuration = 99f; 

    [Networked] private TickTimer MatchTimer { get; set; }
    [Networked] public bool IsGameStarted { get; set; } 

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            IsGameStarted = false;
        }
    }

    // === LOGIC MẠNG (Chỉ Server tính toán) ===
    public override void FixedUpdateNetwork()
    {
        if (Runner.IsServer)
        {
            if (!IsGameStarted)
            {
                // Kiểm tra đủ 2 người
                if (Runner.SessionInfo.PlayerCount >= 2)
                {
                    StartMatch();
                }
            }
            else 
            {
                if (MatchTimer.Expired(Runner))
                {
                    // Hết giờ -> Có thể thêm logic kết thúc game
                    // IsGameStarted = false;
                }
            }
        }
    }

    // === LOGIC HIỂN THỊ (Chạy trên TẤT CẢ máy để cập nhật UI) ===
    public override void Render()
    {
        // Luôn cập nhật UI dựa trên biến mạng IsGameStarted và MatchTimer
        if (GameUIManager.Instance != null)
        {
            if (IsGameStarted && MatchTimer.IsRunning)
            {
                float remainingTime = MatchTimer.RemainingTime(Runner) ?? 0;
                GameUIManager.Instance.UpdateTimer(remainingTime);
            }
            else
            {
                if (GameUIManager.Instance.timerText != null)
                    GameUIManager.Instance.timerText.text = "Waiting...";
            }
        }
    }

    private void StartMatch()
    {
        Debug.Log("Đủ người! Bắt đầu đếm giờ!");
        IsGameStarted = true;
        MatchTimer = TickTimer.CreateFromSeconds(Runner, matchDuration);
        
        // Lưu ý: Đã xóa đoạn ForceSpawnPet để pet không tự ra
    }
}