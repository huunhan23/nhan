using UnityEngine;
using UnityEngine.UI;
using TMPro; // Nếu dùng TextMeshPro

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelStart;
    public GameObject panelSelection;
    
    [Header("UI Elements")]
    public TMP_InputField nameInput; // Hoặc InputField thường
    public TextMeshProUGUI petNameText; // Hoặc Text thường
    
    [Header("Data")]
    public string[] petNames = { "Tê Giác Điện", "Nhện Lửa" }; // Tên hiển thị
    private int selectedPetIndex = 0;

    [Header("Network Reference")]
    public NetworkRunnerHandler networkRunner; // Kéo object _NetworkManager vào đây

    void Start()
    {
        // Mặc định hiện Start, ẩn Selection
        panelStart.SetActive(true);
        panelSelection.SetActive(false);
        
        UpdatePetUI();
    }

    // === CÁC HÀM CHO NÚT BẤM ===

    // Nút "Start" ở màn hình đầu
    public void OnClick_GoToSelection()
    {
        panelStart.SetActive(false);
        panelSelection.SetActive(true);
    }

    // Nút "Next Pet"
    public void OnClick_NextPet()
    {
        selectedPetIndex++;
        if (selectedPetIndex >= petNames.Length) selectedPetIndex = 0;
        UpdatePetUI();
    }

    // Nút "Prev Pet"
    public void OnClick_PrevPet()
    {
        selectedPetIndex--;
        if (selectedPetIndex < 0) selectedPetIndex = petNames.Length - 1;
        UpdatePetUI();
    }

    // Nút "ENTER GAME" (Host)
    public void OnClick_StartHost()
    {
        SaveData();
        networkRunner.StartHost(); // Gọi hàm bắt đầu Fusion
        gameObject.SetActive(false); // Tắt Canvas Menu đi
    }

    // Nút "JOIN GAME" (Client)
    public void OnClick_StartClient()
    {
        SaveData();
        networkRunner.StartClient(); // Gọi hàm bắt đầu Fusion
        gameObject.SetActive(false); // Tắt Canvas Menu đi
    }

    // === LOGIC PHỤ ===

    void UpdatePetUI()
    {
        if(petNameText) petNameText.text = petNames[selectedPetIndex];
    }

    void SaveData()
    {
        string playerName = nameInput.text;
        if (string.IsNullOrEmpty(playerName)) playerName = "Player_" + Random.Range(100, 999);

        // Lưu vào PlayerPrefs để qua Scene khác vẫn nhớ
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.SetInt("SelectedPet", selectedPetIndex);
        PlayerPrefs.Save();
        
        Debug.Log($"Đã lưu: {playerName} - Pet Index: {selectedPetIndex}");
    }
}