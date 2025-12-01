using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Player Prefab")]
    public NetworkPrefabRef playerPrefab;

    // === BIẾN CÀI ĐẶT SCENE ===
    [Header("Scene Settings")]
    public int gameplaySceneIndex = 1; // Index của Scene Game trong Build Settings

    [Header("UI References")]
    public GameObject menuCanvas;
    public GameObject menuCamera;
    private TMP_InputField _chatInput;

    private NetworkRunner _runner;
    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();
    private Camera _cam;
    private float _accumulatedRotationY = 0f;

    void Awake()
    {
        // Giữ object này tồn tại khi chuyển Scene
        DontDestroyOnLoad(gameObject);

        _cam = Camera.main;
        
        // (Đoạn code bật tắt menu cũ giữ nguyên...)
        if (menuCanvas) menuCanvas.SetActive(true);
        if (menuCamera) menuCamera.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartHost() { StartGame(GameMode.Host); }
    public void StartClient() { StartGame(GameMode.Client); }

    private async void StartGame(GameMode mode)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        // Lấy Scene Ref từ Index
        var sceneRef = SceneRef.FromIndex(gameplaySceneIndex);

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "MyFusionRoom",
            Scene = sceneRef, 
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            
            // --- ĐÃ XÓA DÒNG AppVersion ĐỂ SỬA LỖI ---
        });
    }

    // === FUSION CALLBACKS ===

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            Vector3 spawnPosition = new Vector3(0, 5, 0);
            GameObject spawnPointObj = GameObject.FindGameObjectWithTag("Respawn");
            
            if (spawnPointObj != null)
            {
                spawnPosition = spawnPointObj.transform.position;
                spawnPosition.x += UnityEngine.Random.Range(-2f, 2f);
            }

            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
            _spawnedPlayers.Add(player, networkPlayerObject);
        }

        if (runner.LocalPlayer == player)
        {
            if (menuCanvas) menuCanvas.SetActive(false);
            if (menuCamera) menuCamera.SetActive(false);
        }
    }

public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        // ------------------------------------------------------------
        // 1. KIỂM TRA CHAT
        // ------------------------------------------------------------
        if (_chatInput == null)
        {
            GameObject chatObj = GameObject.Find("ChatInput");
            if (chatObj != null) _chatInput = chatObj.GetComponent<TMPro.TMP_InputField>();
        }

        if (_chatInput != null && _chatInput.isFocused)
        {
            // Đang chat -> Gửi data rỗng (Input mặc định = 0)
            // Giữ lại AimPoint hiện tại để chuột không bị giật về (0,0,0)
            data.aimPoint = GetCurrentAimPoint(); 
            input.Set(data);
            return;
        }

        // ------------------------------------------------------------
        // 2. KIỂM TRA UI CHẶN GAME (Kết quả, Hướng dẫn...)
        // ------------------------------------------------------------
        if (GameUIManager.Instance != null && GameUIManager.Instance.IsUIOpen())
        {
            // Đang mở bảng hướng dẫn -> Gửi data rỗng
            data.aimPoint = GetCurrentAimPoint();
            input.Set(data); 
            return; 
        }

        // ------------------------------------------------------------
        // 3. XỬ LÝ DI CHUYỂN BÌNH THƯỜNG
        // ------------------------------------------------------------
        if (Keyboard.current != null)
        {
            Vector2 moveInput = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
            if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;
            if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;
            if (Keyboard.current.dKey.isPressed) moveInput.x += 1;
            data.moveDirection = moveInput;

            data.buttons.Set(EInputButtons.JUMP, Keyboard.current.spaceKey.isPressed);
            data.buttons.Set(EInputButtons.SPRINT, Keyboard.current.leftShiftKey.isPressed);
            data.buttons.Set(EInputButtons.WALK, Keyboard.current.leftCtrlKey.isPressed);
            
            data.buttons.Set(EInputButtons.ATTACK, Keyboard.current.rKey.isPressed);
            data.buttons.Set(EInputButtons.SKILL1, Keyboard.current.tKey.isPressed);
            data.buttons.Set(EInputButtons.SKILL2, Keyboard.current.yKey.isPressed);
            
            data.buttons.Set(EInputButtons.SUMMON, Keyboard.current.hKey.isPressed);
            data.buttons.Set(EInputButtons.CALL_PET, Keyboard.current.eKey.isPressed);
            data.buttons.Set(EInputButtons.MOUNT, Keyboard.current.fKey.isPressed);
        }

        // Xử lý góc xoay
        if (Mouse.current != null)
        {
            if (Mouse.current.rightButton.isPressed)
            {
                float mouseX = Mouse.current.delta.x.ReadValue();
                _accumulatedRotationY += mouseX * 2.0f * Time.deltaTime * 50f; 
            }
            data.rotationY = _accumulatedRotationY;
        }

        // Lấy AimPoint
        data.aimPoint = GetCurrentAimPoint();

        input.Set(data);
    }

    // === HÀM PHỤ TRỢ ĐỂ LẤY VỊ TRÍ CHUỘT (Dùng chung) ===
    private Vector3 GetCurrentAimPoint()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam != null && Mouse.current != null)
        {
            Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
                return hit.point;
            else
                return ray.GetPoint(200f);
        }
        return Vector3.zero;
    }
            public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer && _spawnedPlayers.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedPlayers.Remove(player);
        }
    }

    // === CÁC HÀM BẮT BUỘC KHÁC ===
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}