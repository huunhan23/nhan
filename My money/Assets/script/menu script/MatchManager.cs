using Fusion;
using UnityEngine;

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance;

    [Header("Settings")]
    public float matchTime = 99f;

    // === BIẾN ĐỒNG BỘ SỐ LIỆU ===
    [Networked] public float P1_DamageDealt { get; set; }
    [Networked] public float P2_DamageDealt { get; set; }
    [Networked] public float P1_CurrentHP { get; set; }
    [Networked] public float P2_CurrentHP { get; set; }
    
    [Networked] public TickTimer MatchTimer { get; set; }
    [Networked] public bool IsGameEnded { get; set; }
    
    // 0: Chưa ai thắng, 1: P1 Thắng, 2: P2 Thắng, 3: Hòa
    [Networked] public int WinnerID { get; set; } 

    private void Awake() { Instance = this; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            MatchTimer = TickTimer.CreateFromSeconds(Runner, matchTime);
            IsGameEnded = false;
            P1_CurrentHP = 100; // Máu mặc định
            P2_CurrentHP = 100;
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Cập nhật UI Timer
        if (!IsGameEnded && GameUIManager.Instance != null)
        {
            float time = MatchTimer.RemainingTime(Runner) ?? 0;
            GameUIManager.Instance.UpdateTimer(time);
        }

       if (Runner.IsServer)
        {
            if (!IsGameEnded)
            {
                // Kiểm tra nếu Timer chưa chạy (nghĩa là trận chưa bắt đầu)
                if (MatchTimer.IsRunning == false)
                {
                    // Đếm số lượng PlayerController thực tế trong Scene
                    int playersInScene = 0;
                    foreach(var p in Runner.ActivePlayers)
                    {
                        // Kiểm tra xem người chơi này đã có Object nhân vật chưa
                        if(Runner.GetPlayerObject(p) != null) playersInScene++;
                    }

                    // Nếu đủ 2 người ĐÃ CÓ NHÂN VẬT -> Bắt đầu
                    if (playersInScene >= 2)
                    {
                        Debug.Log("Đủ 2 người! Bắt đầu đếm giờ.");
                        MatchTimer = TickTimer.CreateFromSeconds(Runner, matchTime);
                        
                        // (Tùy chọn) Gọi RPC để hiện thông báo "FIGHT!"
                    }
                }
                // Nếu Timer đang chạy và hết giờ -> Kết thúc
                else if (MatchTimer.Expired(Runner))
                {
                    EndGameByTimeout();
                }
            }
        }
    }

    // === HÀM GHI NHẬN SÁT THƯƠNG (Gọi từ HealthComponent) ===
    public void RecordDamage(bool isP1Attacker, float amount)
    {
        if (!Runner.IsServer || IsGameEnded) return;

        if (isP1Attacker) P1_DamageDealt += amount;
        else P2_DamageDealt += amount;
    }

    // === HÀM CẬP NHẬT MÁU (Gọi từ HealthComponent) ===
    public void UpdateHP(bool isP1, float hp)
    {
        if (!Runner.IsServer) return;

        if (isP1) P1_CurrentHP = hp;
        else P2_CurrentHP = hp;

        // Kiểm tra chết
        if (hp <= 0 && !IsGameEnded)
        {
            // Nếu P1 chết -> P2 thắng (ID 2), và ngược lại
            EndGame(isP1 ? 2 : 1); 
        }
    }

    // === LOGIC KẾT THÚC GAME ===
    private void EndGameByTimeout()
    {
        // So sánh máu để tìm người thắng
        if (P1_CurrentHP > P2_CurrentHP) EndGame(1);
        else if (P2_CurrentHP > P1_CurrentHP) EndGame(2);
        else EndGame(3); // Hòa
    }

    private void EndGame(int winnerId)
    {
        IsGameEnded = true;
        WinnerID = winnerId;
        
        // Gọi RPC để báo cho tất cả Client hiển thị kết quả
        Rpc_ShowGameOver(winnerId, P1_DamageDealt, P2_DamageDealt);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_ShowGameOver(int winnerId, float p1Dmg, float p2Dmg)
    {
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ProcessGameOver(winnerId, p1Dmg, p2Dmg);
        }
    }
}