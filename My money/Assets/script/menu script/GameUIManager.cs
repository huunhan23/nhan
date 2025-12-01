using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

    // ================== CÁC BIẾN KHAI BÁO (CHỈ KHAI BÁO 1 LẦN Ở ĐÂY) ==================

    [Header("Result Screens")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    
    public GameObject statsPanel;
    public TextMeshProUGUI p1DamageText;
    public TextMeshProUGUI p2DamageText;
    public Button menuButton;

    [Header("Timer")]
    public TextMeshProUGUI timerText;

    [Header("Player 1 (Host - Left)")]
    public Image p1Avatar;
    public Slider p1HealthSlider;
    public Image p1PetAvatar;
    public Slider p1PetHealthSlider;

    [Header("Player 2 (Client - Right)")]
    public Image p2Avatar;
    public Slider p2HealthSlider;
    public Image p2PetAvatar;
    public Slider p2PetHealthSlider;

    [Header("Guide UI")]
    public GameObject guidePanel;        // <--- Khai báo 1 lần duy nhất
    public Button closeGuideButton;      // <--- Khai báo 1 lần duy nhất

    // =================================================================================

    void Awake()
    {
        Instance = this;
        
        // Đảm bảo trạng thái ban đầu
        if(resultPanel) resultPanel.SetActive(false);
        if(statsPanel) statsPanel.SetActive(false);
        if(guidePanel) guidePanel.SetActive(false); // Tắt bảng hướng dẫn lúc đầu

        if(menuButton) menuButton.onClick.AddListener(OnBackToMenu);
        if(closeGuideButton) closeGuideButton.onClick.AddListener(CloseGuidePanel);
    }

    // === CÁC HÀM XỬ LÝ THANH MÁU ===

    private void SetHealthColor(Slider slider, float ratio)
    {
        if (slider == null) return;
        Image fillImage = slider.fillRect.GetComponent<Image>();
        if (fillImage != null)
        {
            if (ratio > 0.8f) fillImage.color = Color.green;
            else if (ratio > 0.3f) fillImage.color = Color.yellow;
            else fillImage.color = Color.red;
        }
    }

    public void UpdatePlayerHealth(bool isPlayer1, float current, float max)
    {
        float ratio = current / max;
        if (isPlayer1)
        {
            if(p1HealthSlider) { p1HealthSlider.value = ratio; SetHealthColor(p1HealthSlider, ratio); }
        }
        else
        {
            if(p2HealthSlider) { p2HealthSlider.value = ratio; SetHealthColor(p2HealthSlider, ratio); }
        }
    }

    public void UpdatePetHealth(bool isPlayer1Owner, float current, float max)
    {
        float ratio = current / max;
        if (isPlayer1Owner)
        {
            if(p1PetHealthSlider) { p1PetHealthSlider.value = ratio; SetHealthColor(p1PetHealthSlider, ratio); }
        }
        else
        {
            if(p2PetHealthSlider) { p2PetHealthSlider.value = ratio; SetHealthColor(p2PetHealthSlider, ratio); }
        }
    }

    public void UpdateTimer(float timeRemaining)
    {
        if (timerText)
        {
            timerText.text = Mathf.CeilToInt(timeRemaining).ToString();
            if (timeRemaining < 10) timerText.color = Color.red;
            else timerText.color = Color.white;
        }
    }

    // === CÁC HÀM XỬ LÝ KẾT QUẢ TRẬN ĐẤU ===

    public void ProcessGameOver(int winnerId, float p1Dmg, float p2Dmg)
    {
        var runner = FindObjectOfType<NetworkRunner>();
        // Kiểm tra null để tránh lỗi nếu runner chưa kịp khởi tạo
        if (runner == null || !runner.IsRunning) return;

        bool amIPlayer1 = runner.LocalPlayer.PlayerId == 1; 

        string text = "";
        Color color = Color.white;

        if (winnerId == 3) { text = "DRAW"; color = Color.yellow; }
        else if ((winnerId == 1 && amIPlayer1) || (winnerId == 2 && !amIPlayer1)) { text = "VICTORY"; color = Color.green; }
        else { text = "DEFEAT"; color = Color.red; }

        if(resultPanel) 
        {
            resultPanel.SetActive(true);
            if(resultText) 
            {
                resultText.text = text;
                resultText.color = color;
            }
        }

        StartCoroutine(ShowStatsRoutine(p1Dmg, p2Dmg));
    }

    IEnumerator ShowStatsRoutine(float p1Dmg, float p2Dmg)
    {
        yield return new WaitForSeconds(3.0f);

        if(resultPanel) resultPanel.SetActive(false);
        if(statsPanel) statsPanel.SetActive(true);

        if(p1DamageText) p1DamageText.text = "P1 Damage: " + Mathf.RoundToInt(p1Dmg);
        if(p2DamageText) p2DamageText.text = "P2 Damage: " + Mathf.RoundToInt(p2Dmg);
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnBackToMenu()
    {
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null) runner.Shutdown();
        
        // Hủy luôn object Network Manager cũ để tránh trùng lặp
        GameObject networkManager = GameObject.Find("_NetworkManager");
        if(networkManager) Destroy(networkManager);

        SceneManager.LoadScene(0);
    }

    // === CÁC HÀM HƯỚNG DẪN (GUIDE) ===

   public void OpenGuide()
    {
        if(guidePanel) guidePanel.SetActive(true);
        
        // Mở chuột để người chơi bấm nút đóng
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ToggleGuidePanel()
    {
        if(guidePanel == null) return;

        bool isActive = !guidePanel.activeSelf;
        guidePanel.SetActive(isActive);

        if (isActive) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        else { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }

    public void CloseGuidePanel()
    {
        if(guidePanel) guidePanel.SetActive(false);
        
        // Khóa chuột lại để chơi tiếp
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Hàm kiểm tra xem có bảng UI nào đang bật không (để chặn di chuyển)
   public bool IsUIOpen()
    {
        if (guidePanel != null && guidePanel.activeSelf) return true;
        // ... (các panel khác giữ nguyên)
        if (resultPanel != null && resultPanel.activeSelf) return true;
        if (statsPanel != null && statsPanel.activeSelf) return true;
        
        return false;
    }
}