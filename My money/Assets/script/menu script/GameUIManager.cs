using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

    // ================== 1. CÁC BIẾN UI ==================

    [Header("Result Screens")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public GameObject statsPanel;
    public TextMeshProUGUI p1DamageText;
    public TextMeshProUGUI p2DamageText;
    public Button menuButton;
    [Header("Status Icons (Mới)")]
    public Sprite slowIcon; // Kéo hình icon (ví dụ hình giọt nước/rùa) vào đây
    
    // Vị trí hiển thị icon trên UI
    public Image p1StatusImage; // Kéo StatusIcon của P1 vào
    public Image p2StatusImage; // Kéo StatusIcon của P2 vào

    [Header("Timer")]
    public TextMeshProUGUI timerText;

    [Header("Guide UI")]
    public GameObject guidePanel;
    public Button closeGuideButton;
    [Header("Emote UI")]
    public Image p1EmoteDisplay; // Kéo Image P1_EmoteDisplay vào đây
    public Image p2EmoteDisplay; // Kéo Image P2_EmoteDisplay vào đây
    
    public Sprite[] emoteSprites; // Kéo các hình Emote (Cười, Khóc...) vào đây theo thứ tự ID

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

    [Header("Skill UI (Local Player)")]
    public Image skill1IconDisplay;
    public Image skill1Overlay; 
    public TextMeshProUGUI skill1Text;

    public Image skill2IconDisplay;
    public Image skill2Overlay;
    public TextMeshProUGUI skill2Text;

    [Header("Item UI")]
    public Image itemIconDisplay;

    [Header("Avatar Database")]
    public Sprite[] avatarList; // Danh sách ảnh Avatar để đồng bộ

    // ====================================================

    void Awake()
    {
        Instance = this;
        
        if(resultPanel) resultPanel.SetActive(false);
        if(statsPanel) statsPanel.SetActive(false);
        if(guidePanel) guidePanel.SetActive(false);

        if(menuButton) menuButton.onClick.AddListener(OnBackToMenu);
        if(closeGuideButton) closeGuideButton.onClick.AddListener(CloseGuidePanel);
    }

    // ================== 2. XỬ LÝ AVATAR (ĐÃ THÊM LẠI) ==================

    // Hàm này gọi khi Local Player sinh ra (gửi ảnh trực tiếp)
    public void SetupPlayerAvatar(Sprite avatar)
    {
        // Mặc định gán vào P1 (Bên trái) cho máy mình
        if (p1Avatar) p1Avatar.sprite = avatar;
    }

    // Hàm này gọi khi đồng bộ qua mạng (dựa trên index)
    public void UpdateAvatarUI(bool isPlayer1, int avatarIndex)
    {
        if (avatarList == null || avatarIndex < 0 || avatarIndex >= avatarList.Length) return;

        Sprite selectedSprite = avatarList[avatarIndex];

        if (isPlayer1)
        {
            if (p1Avatar) p1Avatar.sprite = selectedSprite;
        }
        else
        {
            if (p2Avatar) p2Avatar.sprite = selectedSprite;
        }
    }

    // ================== 3. XỬ LÝ SKILL & ITEM & PET UI ==================

    public void SetupPetUI(Sprite avatar, Sprite s1, Sprite s2)
    {
        if (p1PetAvatar) p1PetAvatar.sprite = avatar;
        if (skill1IconDisplay) skill1IconDisplay.sprite = s1;
        if (skill2IconDisplay) skill2IconDisplay.sprite = s2;

        if (skill1Overlay) skill1Overlay.fillAmount = 0;
        if (skill1Text) skill1Text.text = "";
        if (skill2Overlay) skill2Overlay.fillAmount = 0;
        if (skill2Text) skill2Text.text = "";
    }

    public void SetupItemUI(Sprite icon)
    {
        if (itemIconDisplay != null) itemIconDisplay.sprite = icon;
    }

    public void UpdateSkillCooldown(int skillID, float current, float max)
    {
        Image overlay = (skillID == 1) ? skill1Overlay : skill2Overlay;
        TextMeshProUGUI text = (skillID == 1) ? skill1Text : skill2Text;

        if (overlay == null) return;

        if (current > 0)
        {
            float ratio = current / max;
            overlay.fillAmount = ratio;
            if (text) text.text = current.ToString("F1");
        }
        else
        {
            overlay.fillAmount = 0;
            if (text) text.text = "";
        }
    }

    // ================== 4. XỬ LÝ THANH MÁU (ĐỔI MÀU) ==================

    private void SetHealthColor(Slider slider, float ratio)
    {
        if (slider == null) return;
        if (slider.fillRect == null) return; // Kiểm tra null an toàn

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

    // ================== 5. XỬ LÝ GAME OVER & TIMER ==================

    public void UpdateTimer(float timeRemaining)
    {
        if (timerText)
        {
            timerText.text = Mathf.CeilToInt(timeRemaining).ToString();
            if (timeRemaining < 10) timerText.color = Color.red;
            else timerText.color = Color.white;
        }
    }

    public void ProcessGameOver(int winnerId, float p1Dmg, float p2Dmg)
    {
        var runner = FindObjectOfType<NetworkRunner>();
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
            if(resultText) { resultText.text = text; resultText.color = color; }
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
        
        GameObject networkManager = GameObject.Find("_NetworkManager");
        if(networkManager) Destroy(networkManager);

        SceneManager.LoadScene(0);
    }

    // ================== 6. XỬ LÝ GUIDE ==================

    public void OpenGuide()
    {
        if(guidePanel) guidePanel.SetActive(true);
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
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool IsUIOpen()
    {
        if (guidePanel != null && guidePanel.activeSelf) return true;
        if (resultPanel != null && resultPanel.activeSelf) return true;
        if (statsPanel != null && statsPanel.activeSelf) return true;
        return false;
    }
    public void UpdateStatusUI(bool isPlayer1, bool isWet)
    {
        // Chọn đúng Image của Phe
        Image targetImage = isPlayer1 ? p1StatusImage : p2StatusImage;
        
        if (targetImage == null) return;

        if (isWet)
        {
            targetImage.gameObject.SetActive(true); // Bật lên
            targetImage.sprite = slowIcon; // Gán hình (ví dụ hình làm chậm)
        }
        else
        {
            targetImage.gameObject.SetActive(false); // Tắt đi
        }
    }
    public void ShowEmote(bool isPlayer1, int emoteID)
    {
        // 1. Kiểm tra dữ liệu
        if (emoteSprites == null || emoteID < 0 || emoteID >= emoteSprites.Length) return;

        // 2. Chọn đúng Image của Phe
        Image targetImage = isPlayer1 ? p1EmoteDisplay : p2EmoteDisplay;
        if (targetImage == null) return;

        // 3. Gán hình và Bật lên
        targetImage.sprite = emoteSprites[emoteID];
        targetImage.gameObject.SetActive(true);

        // 4. Chạy Coroutine để tắt sau 5 giây
        // (Lưu ý: Phải dừng Coroutine cũ nếu đang chạy để tránh bị tắt nhầm)
        StartCoroutine(HideEmoteRoutine(targetImage));
    }

    IEnumerator HideEmoteRoutine(Image targetImage)
    {
        yield return new WaitForSeconds(5.0f); // Hiện trong 5 giây
        
        if (targetImage != null)
        {
            targetImage.gameObject.SetActive(false); // Tắt đi
        }
    }
}