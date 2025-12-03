using UnityEngine;
using UnityEngine.UI;
using TMPro; // Nếu dùng TextMeshPro
using Fusion;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelStart;
    public GameObject panelSelection;
    
    [Header("UI Elements")]
    public TMP_InputField nameInput; // Hoặc InputField thường
    public TextMeshProUGUI petNameText; // Hoặc Text thường
    public Image petIconDisplay;
    
    [Header("Data")]
    public string[] petNames = { "Tê Giác Điện", "Nhện Lửa" }; // Tên hiển thị
    private int selectedPetIndex = 0;

    [Header("Network Reference")]
    public NetworkRunnerHandler networkRunner; // Kéo object _NetworkManager vào đây
[Header("Pet 3D Preview")]
    public GameObject[] petPrefabs; // Kéo các Prefab Pet (Rhino, Spider...) vào đây
    public Transform petPreviewSpot; // Kéo cái vị trí "Sân khấu" vào đây
    
    private GameObject _currentPetModel; // Biến lưu con pet đang hiện
    public Sprite[] petIcons;
    void Start()
    {
        // Mặc định hiện Start, ẩn Selection
      panelStart.SetActive(true);
        panelSelection.SetActive(false);
        
        // === SỬA Ở ĐÂY: ĐẢM BẢO KHÔNG CÓ PET NÀO HIỆN RA LÚC ĐẦU ===
        HidePetVisuals(); 
        
        // Tuyệt đối KHÔNG gọi UpdatePetUI() ở đây nhé!
    }

    // === CÁC HÀM CHO NÚT BẤM ===

    // Nút "Start" ở màn hình đầu
    public void OnClick_GoToSelection()
    {
        panelStart.SetActive(false);
        panelSelection.SetActive(true);
        UpdatePetUI();
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
        // 1. Cập nhật Tên
        if(petNameText) petNameText.text = petNames[selectedPetIndex];

        // 2. Cập nhật Mô Hình 3D
        ShowPetModel(selectedPetIndex);

        // 3. Cập nhật Hình ảnh 2D (=== MỚI ===)
        if (petIconDisplay != null && petIcons != null)
        {
            // Đảm bảo index hợp lệ
            if (selectedPetIndex >= 0 && selectedPetIndex < petIcons.Length)
            {
                // Gán hình ảnh tương ứng
                petIconDisplay.sprite = petIcons[selectedPetIndex];
                
                // Nếu có hình thì bật lên, không có (null) thì tắt đi để tránh hiện ô trắng
                petIconDisplay.enabled = (petIcons[selectedPetIndex] != null);
            }
        }
    }
    void HidePetVisuals()
    {
        // 1. Xóa model 3D
        if (_currentPetModel != null)
        {
            Destroy(_currentPetModel);
            _currentPetModel = null;
        }

        // 2. Tắt hình 2D (Icon)
        if (petIconDisplay != null)
        {
            petIconDisplay.enabled = false;
        }
        
        // 3. Xóa chữ tên Pet (cho sạch)
        if (petNameText != null)
        {
            petNameText.text = "";
        }
    }
void ShowPetModel(int index)
    {
        // A. Xóa con cũ đi (nếu đang có)
        if (_currentPetModel != null)
        {
            Destroy(_currentPetModel);
        }

        // B. Kiểm tra dữ liệu
        if (petPrefabs == null || index < 0 || index >= petPrefabs.Length) return;
        if (petPreviewSpot == null) return;

        // C. Tạo con mới (Instantiate bình thường, không phải qua mạng)
        _currentPetModel = Instantiate(petPrefabs[index], petPreviewSpot.position, petPreviewSpot.rotation);

        // D. QUAN TRỌNG: BIẾN NÓ THÀNH "TƯỢNG" (VÔ HIỆU HÓA LOGIC)
        // Vì đây chỉ là hình ảnh minh họa ở Menu, nó không được phép có AI hay Network
        
        // 1. Xóa NetworkObject (để Fusion không báo lỗi)
        var netObj = _currentPetModel.GetComponent<NetworkObject>();
        if (netObj) Destroy(netObj);

        // 2. Xóa PetAI (để nó không tìm Player hay chạy lung tung)
        var ai = _currentPetModel.GetComponent<PetAI>();
        if (ai) Destroy(ai);

        // 3. Xóa NavMeshAgent (để nó không bị lỗi NavMesh)
        var agent = _currentPetModel.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent) Destroy(agent);
        
        // 4. (Tùy chọn) Chỉnh Scale to lên cho đẹp nếu cần
        // _currentPetModel.transform.localScale = Vector3.one * 1.5f;
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