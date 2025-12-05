using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using System.Collections.Generic;
using TMPro; // Để dùng Text UI

public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager Instance;

    [Header("Leaderboard UI")]
    public GameObject rowPrefab;   // Prefab 1 dòng trong bảng
    public Transform rowsParent;   // Cái Content của ScrollView
    public GameObject leaderboardPanel; // Cái bảng to chứa tất cả

    void Awake()
    {
        Instance = this;
        Login();
    }

    // === 1. ĐĂNG NHẬP (Tự động) ===
    void Login()
    {
        // Tạo ID độc nhất bằng cách ghép: ID Máy + Tên Người Chơi (để test 2 người trên 1 máy)
        string savedName = PlayerPrefs.GetString("PlayerName", "Unknown");
        string uniqueID = SystemInfo.deviceUniqueIdentifier + "_" + savedName;

        var request = new LoginWithCustomIDRequest
        {
            CustomId = uniqueID, 
            CreateAccount = true, // Yêu cầu tạo nick nếu chưa có
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnError);
    }

    void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Đăng nhập PlayFab thành công!");
        
        // Cập nhật tên hiển thị (nếu chưa có)
        string name = PlayerPrefs.GetString("PlayerName", "UnknownPlayer");
        UpdateDisplayName(name);
    }

    // === 2. CẬP NHẬT TÊN ===
    public void UpdateDisplayName(string newName)
    {
        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = newName
        };
        PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnNameUpdate, OnError);
    }
    void OnNameUpdate(UpdateUserTitleDisplayNameResult result) { }

    // === 3. GỬI ĐIỂM DAME ===
    public void SendLeaderboard(int score)
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = "MaxDamage", // Phải TRÙNG tên trên Web
                    Value = score
                }
            }
        };
        PlayFabClientAPI.UpdatePlayerStatistics(request, OnLeaderboardUpdate, OnError);
    }

    void OnLeaderboardUpdate(UpdatePlayerStatisticsResult result)
    {
        Debug.Log("Đã gửi điểm lên Server!");
        // Gửi xong thì tải bảng về xem luôn
        GetLeaderboard(); 
    }

    // === 4. LẤY BẢNG XẾP HẠNG ===
    public void GetLeaderboard()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = "MaxDamage",
            StartPosition = 0,
            MaxResultsCount = 10 // Lấy top 10
        };
        PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardGet, OnError);
    }

   void OnLeaderboardGet(GetLeaderboardResult result)
    {
        Debug.Log($"Lấy được {result.Leaderboard.Count} dòng xếp hạng."); // <--- KIỂM TRA DÒNG NÀY

        foreach (Transform child in rowsParent) Destroy(child.gameObject);

        foreach (var item in result.Leaderboard)
        {
            GameObject newRow = Instantiate(rowPrefab, rowsParent);
            TextMeshProUGUI[] texts = newRow.GetComponentsInChildren<TextMeshProUGUI>();
            
            // In ra để kiểm tra
            // Debug.Log($"Row: {item.Position} - {item.DisplayName} - {item.StatValue}");

            if (texts.Length >= 3)
            {
                texts[0].text = (item.Position + 1).ToString(); 
                texts[1].text = item.DisplayName ?? item.PlayFabId; // Nếu ko có tên thì hiện ID
                texts[2].text = item.StatValue.ToString(); 
            }
        }
        
        if(leaderboardPanel) leaderboardPanel.SetActive(true);
    }

    // === HÀM XỬ LÝ LỖI (CHỈ CÓ 1 HÀM DUY NHẤT Ở ĐÂY) ===
    void OnError(PlayFabError error)
    {
        Debug.LogWarning("Lỗi PlayFab: " + error.GenerateErrorReport());
    }
}