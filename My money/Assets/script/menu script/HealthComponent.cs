using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class HealthComponent : NetworkBehaviour
{
    [Header("Cài đặt Máu")]
    public float maxHealth = 100f;
    
    [Header("Loại Đối Tượng")]
    public bool isPet = false; // Tích vào nếu đây là Pet

    // Biến máu đồng bộ qua mạng
    [Networked] public float CurrentHealth { get; set; }
    
    private ChangeDetector _changes;

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        
        // Chỉ Server mới được set máu ban đầu
        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
        }
        
        UpdateUI();
    }

    public override void Render()
    {
        // Lắng nghe sự thay đổi của máu
        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(CurrentHealth))
            {
                UpdateUI();
                // ĐÃ XÓA DÒNG CheckDeath() GÂY LỖI
            }
        }
    }

    // === HÀM CẬP NHẬT UI ===
    void UpdateUI()
    {
        if (GameUIManager.Instance == null) return;

        // Logic xác định Phe 1 hay Phe 2
        // Giả định Host (PlayerId 1) là Phe 1
        bool isPlayer1Side = (Object.InputAuthority.PlayerId == 1) || (Object.InputAuthority == Runner.LocalPlayer && Runner.IsServer);

        if (isPet)
        {
            GameUIManager.Instance.UpdatePetHealth(isPlayer1Side, CurrentHealth, maxHealth);
        }
        else
        {
            GameUIManager.Instance.UpdatePlayerHealth(isPlayer1Side, CurrentHealth, maxHealth);
        }
    }
// === THÊM HÀM NÀY VÀO HealthComponent.cs ===
    public void Heal(float amount)
    {
        if (!Object.HasStateAuthority) return; // Chỉ Server mới được hồi máu

        CurrentHealth += amount;
        
        // Không cho máu vượt quá tối đa
        if (CurrentHealth > maxHealth) 
        {
            CurrentHealth = maxHealth; 
        }
        
        // Debug.Log($"{gameObject.name} đã hồi {amount} máu.");
    }
    // === HÀM NHẬN SÁT THƯƠNG (Server Only) ===
    public void TakeDamage(float amount)
    {
        if (!Object.HasStateAuthority) return;

        if (CurrentHealth > 0)
        {
            CurrentHealth -= amount;

            // === BÁO CÁO CHO TRỌNG TÀI (MATCH MANAGER) ===
            if (MatchManager.Instance != null)
            {
                // Xác định ai là người bị đánh
                bool isP1Victim = (Object.InputAuthority.PlayerId == 1);
                
                // Ghi nhận sát thương (Nếu P1 bị đánh -> P2 gây dame)
                MatchManager.Instance.RecordDamage(!isP1Victim, amount);
                
                // Cập nhật máu lên bảng điểm (Chỉ tính Player chết, Pet chết không tính thua)
                if (!isPet)
                {
                    MatchManager.Instance.UpdateHP(isP1Victim, CurrentHealth);
                }
            }
            // =============================================
        }
        
        
        // Logic chết đơn giản: Tắt object nếu hết máu
        if (CurrentHealth <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}