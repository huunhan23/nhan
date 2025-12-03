using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class HealthComponent : NetworkBehaviour
{
    // ==================== CÀI ĐẶT ====================
    [Header("Cài đặt Máu")]
    public float maxHealth = 100f;
    public bool isPet = false; 

    [Header("Hiệu ứng Khiên")]
    public GameObject shieldPrefab; 
    public Vector3 shieldOffset = new Vector3(0, 1f, 0); 
    [Networked] public NetworkBool IsWet { get; set; } // Trạng thái bị ướt
    [Networked] public float SpeedMultiplier { get; set; } = 1.0f; // Hệ số tốc độ (1 = bình thường, 0.5 = chậm)

    // ==================== BIẾN ĐỒNG BỘ ====================
    [Networked] public float CurrentHealth { get; set; }
    [Networked] public NetworkBool HasShield { get; set; } 

    private ChangeDetector _changes;
    private GameObject _currentShieldEffect; 

    // ==================== KHỞI TẠO ====================
    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        
        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
            HasShield = false;

            IsWet = false;
            SpeedMultiplier = 1.0f;
        }
        
        
        UpdateUI();
        UpdateShieldVisual(); 
        UpdateStatusUI();
    }

    // ==================== RENDER & UI ====================
    public override void Render()
    {
        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(CurrentHealth)) UpdateUI();
            if (change == nameof(HasShield)) UpdateShieldVisual();
            if (change == nameof(IsWet)) UpdateStatusUI();
        }
    }

    private void UpdateShieldVisual()
    {
        if (HasShield)
        {
            if (_currentShieldEffect == null && shieldPrefab != null)
            {
                _currentShieldEffect = Instantiate(shieldPrefab, transform.position + shieldOffset, Quaternion.identity);
                _currentShieldEffect.transform.SetParent(transform); 
                _currentShieldEffect.transform.localPosition = shieldOffset; 
            }
        }
        else
        {
            if (_currentShieldEffect != null)
            {
                Destroy(_currentShieldEffect);
                _currentShieldEffect = null;
            }
        }
    }
void UpdateStatusUI()
    {
        if (GameUIManager.Instance == null) return;

        // Logic xác định Phe (Copy từ hàm UpdateUI cũ)
        bool isPlayer1Side = (Object.InputAuthority.PlayerId == 1) || (Object.InputAuthority == Runner.LocalPlayer && Runner.IsServer);

        // Gọi sang UI Manager
        // (Lưu ý: Hiện tại ta chỉ làm cho Player, nếu Pet bị ướt muốn hiện thì cần thêm logic UI cho Pet)
        if (!isPet)
        {
            GameUIManager.Instance.UpdateStatusUI(isPlayer1Side, IsWet);
        }
    }
    void UpdateUI()
    {
        if (GameUIManager.Instance == null) return;
        bool isPlayer1Side = (Object.InputAuthority.PlayerId == 1) || (Object.InputAuthority == Runner.LocalPlayer && Runner.IsServer);

        if (isPet) GameUIManager.Instance.UpdatePetHealth(isPlayer1Side, CurrentHealth, maxHealth);
        else GameUIManager.Instance.UpdatePlayerHealth(isPlayer1Side, CurrentHealth, maxHealth);
    }

    // ==================== LOGIC GAMEPLAY (SERVER) ====================

    // 1. HÀM BẬT KHIÊN (Dùng cho Skill)
    public void ActivateShield()
    {
        if (Object.HasStateAuthority)
        {
            HasShield = true;
        }
    }

    // 2. HÀM HỒI MÁU (Dùng cho Item - ĐÂY LÀ HÀM BẠN THIẾU)
    public void Heal(float amount)
    {
        if (!Object.HasStateAuthority) return;

        CurrentHealth += amount;
        if (CurrentHealth > maxHealth) 
        {
            CurrentHealth = maxHealth;
        }
        // Debug.Log($"{gameObject.name} hồi {amount} máu.");
    }
public void SetStatus(bool wet, float speedMult)
    {
        if (Object.HasStateAuthority)
        {
            IsWet = wet;
            SpeedMultiplier = speedMult;
        }
    }
    // 3. HÀM NHẬN SÁT THƯƠNG
    public void TakeDamage(float amount)
    {
        if (!Object.HasStateAuthority) return;

        // Nếu có khiên -> Chặn đòn
        if (HasShield)
        {
            HasShield = false; // Vỡ khiên
            return; 
        }

        // Trừ máu
        if (CurrentHealth > 0)
        {
            CurrentHealth -= amount;
            
            if (MatchManager.Instance != null)
            {
                bool isP1Victim = (Object.InputAuthority.PlayerId == 1);
                MatchManager.Instance.RecordDamage(!isP1Victim, amount);
                if (!isPet) MatchManager.Instance.UpdateHP(isP1Victim, CurrentHealth);
            }
        }

        
        
        if (CurrentHealth <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}