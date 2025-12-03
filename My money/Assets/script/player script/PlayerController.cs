using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic; // Cần cho List<>
using TMPro; // Cần cho TextMeshPro

public class PlayerController : NetworkBehaviour
{
    // =================================================================================================
    // 1. CÁC BIẾN ĐỒNG BỘ (SERVER -> CLIENT)
    // =================================================================================================
    
    // Trạng thái Pet & Cưỡi
    [Networked] public NetworkBool isMounted { get; set; }
    [Networked] public NetworkBool isMounting { get; set; }
    [Networked] public PetAI currentPet { get; set; }
    
    // Input & Aim
    [Networked] public Vector3 aimPoint { get; set; }
    [Networked] private NetworkButtons ButtonsPrev { get; set; }
    
    // Dữ liệu người chơi
    [Networked] public int SelectedPetIndex { get; set; } 
    [Networked] public int SelectedAvatarIndex { get; set; } 
    [Networked] public NetworkString<_16> PlayerName { get; set; }

    // Item System
    [Networked] public int itemCount { get; set; } 
    [Networked] public int SelectedItemID { get; set; } // 0: Máu, 1: Đá Mưa

    // Cooldowns & Timers
    [Networked] private TickTimer SummonTimer { get; set; }
    [Networked] private TickTimer ShieldCooldownTimer { get; set; }
    [Networked] private TickTimer ItemCooldownTimer { get; set; }
    
    // Animation Sync
    [Networked] private float _netAnimSpeed { get; set; }

    // Xoay Ngang Đồng Bộ (Sửa lỗi camera giật)
    [Networked] public float NetworkRotationY { get; set; }

    private ChangeDetector _changes;

    // =================================================================================================
    // 2. CÀI ĐẶT TRONG INSPECTOR
    // =================================================================================================

    [Header("UI")]
    public TextMeshProUGUI nameTextDisplay; // Kéo Text tên trên đầu vào

    [Header("Movement")]
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.0f;
    public float sprintSpeed = 10.0f;
    public float jumpForce = 8.0f;
    public float gravity = -20.0f;

    [Header("Camera")]
    public float mouseSensitivity = 2.0f;
    public float cameraPitchMin = -45.0f; 
    public float cameraPitchMax = 80.0f;  
    public Transform cameraTransform; // Kéo Camera con vào (nếu có)

    [Header("Pet Management")]
    public List<NetworkPrefabRef> petOptions; // Kéo các Prefab Pet vào đây
    
    [Header("Item System")]
    public List<NetworkPrefabRef> itemPrefabs; // Element 0: NULL, Element 1: RainStone Prefab
    public float itemCooldownTime = 15f;
    public Sprite rainStoneIcon; 
    public Sprite potionIcon;

    [Header("Cooldowns")]
    public float summonCooldownTime = 2.0f;
    public float shieldCooldownTime = 60.0f;

    [Header("Audio & Visuals")]
    public AudioClip summonSound;   
    public AudioClip mountSound;    
    public AudioClip commandSound;  
    public Sprite playerAvatar; 

    // Biến nội bộ (Local)
    private CharacterController controller;
    private Animator animator;
    private float verticalVelocity = 0.0f; 
    private float cameraPitch = 0.0f;
    private Camera _localCamera; 
    private AudioSource _audioSource;


    // =================================================================================================
    // 3. KHỞI TẠO
    // =================================================================================================
    public override void Spawned()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>(); 
        _localCamera = GetComponentInChildren<Camera>();
        _audioSource = GetComponent<AudioSource>();
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

        // Setup Camera nếu chưa gán
        if (cameraTransform == null && Camera.main != null) 
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
            cameraPitch = cameraTransform.localEulerAngles.x;

        if (Object.HasInputAuthority)
        {
            // === MÁY CỦA MÌNH ===
            if (_localCamera) { _localCamera.enabled = true; var l = _localCamera.GetComponent<AudioListener>(); if(l) l.enabled = true; }

            // 1. Load Dữ liệu từ PlayerPrefs
            string savedName = PlayerPrefs.GetString("PlayerName", "Player");
            int savedPetIndex = PlayerPrefs.GetInt("SelectedPet", 0);
            int savedAvatarIndex = PlayerPrefs.GetInt("SelectedAvatar", 0);
            int savedItemCount = PlayerPrefs.GetInt("Selected_Item_Count", 3);
            int savedItemID = PlayerPrefs.GetInt("Selected_Item_ID", 0);

            // 2. Gửi lên Server
            Rpc_SetPlayerData(savedName, savedPetIndex, savedAvatarIndex);
            Rpc_SetItemCount(savedItemCount);
            Rpc_SetItemData(savedItemID);

            // 3. Setup UI Local (Avatar & Item Icon)
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.SetupPlayerAvatar(playerAvatar);
                Sprite icon = (savedItemID == 1) ? rainStoneIcon : potionIcon;
                GameUIManager.Instance.SetupItemUI(icon);
            }
        }
        else
        {
            // === MÁY NGƯỜI KHÁC ===
            if (_localCamera) { _localCamera.enabled = false; var l = _localCamera.GetComponent<AudioListener>(); if(l) l.enabled = false; }
        }
        
        UpdateNameUI();
        UpdateAvatarVisual(); 
    }


    // =================================================================================================
    // 4. LOGIC MẠNG (SERVER CHẠY CHÍNH)
    // =================================================================================================
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            // --- A. XỬ LÝ NÚT BẤM ---
            if (data.buttons.WasPressed(ButtonsPrev, EInputButtons.SUMMON))
            {
                if (SummonTimer.ExpiredOrNotRunning(Runner))
                {
                    SummonTimer = TickTimer.CreateFromSeconds(Runner, summonCooldownTime);
                    Rpc_ToggleSummonPet();
                }
            }
            if (data.buttons.WasPressed(ButtonsPrev, EInputButtons.MOUNT)) Rpc_ToggleMount();

            if (currentPet != null && !isMounted && !isMounting)
            {
                if (data.buttons.WasPressed(ButtonsPrev, EInputButtons.ATTACK))   Rpc_CommandAttack();
                if (data.buttons.WasPressed(ButtonsPrev, EInputButtons.SKILL1))   Rpc_CommandSkill1();
                if (data.buttons.WasPressed(ButtonsPrev, EInputButtons.SKILL2))   Rpc_CommandSkill2();
                if (data.buttons.WasPressed(ButtonsPrev, EInputButtons.CALL_PET)) Rpc_CommandMoveToPlayer();
            }

            // --- B. ĐỒNG BỘ ---
            aimPoint = data.aimPoint;

            // Xoay Ngang (Server cập nhật, Client sync theo)
            // Chỉ xoay khi KHÔNG cưỡi (Khi cưỡi thì xoay Pet)
            if (!isMounted)
            {
                transform.rotation = Quaternion.Euler(0, data.rotationY, 0);
            }
            else if (currentPet != null)
            {
                currentPet.transform.rotation = Quaternion.Euler(0, data.rotationY, 0);
            }

            // --- C. DI CHUYỂN (SERVER ONLY) ---
            if (Runner.IsServer)
            {
                if (isMounting)
                {
                    HandleMountingProcess(data); 
                }
                else if (isMounted)
                {
                    // Khi Cưỡi: Ép vị trí Player vào Pet
                    if (currentPet != null && currentPet.mountPoint != null)
                    {
                        controller.enabled = false; 
                        transform.position = currentPet.mountPoint.position;
                        transform.rotation = currentPet.mountPoint.rotation;
                        HandlePetMovement(data);
                        _netAnimSpeed = 0; 
                    }
                }
                else
                {
                    // Khi Đi Bộ
                    controller.enabled = true;
                    HandleMovement(data);
                    HandleGravityAndJump(data);
                    
                    // Tính tốc độ để đồng bộ animation
                    float currentSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
                    _netAnimSpeed = currentSpeed / runSpeed;
                }
            }
            ButtonsPrev = data.buttons;
        }
        
        HandleAnimation();
    }

    // =================================================================================================
    // 5. LOGIC CLIENT (RENDER)
    // =================================================================================================
    public override void Render()
    {
        // 1. Xoay Camera (Chỉ máy mình)
        if (Object.HasInputAuthority)
        {
            HandleMouseAndCamera();
        }

        // 2. Cập nhật Tên và Avatar (Khi có thay đổi)
        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(PlayerName)) UpdateNameUI();
            if (change == nameof(SelectedAvatarIndex)) UpdateAvatarVisual();
        }

        // 3. Billboard Tên (Luôn quay về camera)
        if (nameTextDisplay != null && Camera.main != null)
        {
            nameTextDisplay.transform.parent.rotation = Camera.main.transform.rotation;
        }
    }

    private void UpdateNameUI()
    {
        if (nameTextDisplay != null) nameTextDisplay.text = PlayerName.ToString();
    }

    private void UpdateAvatarVisual()
    {
        if (GameUIManager.Instance != null)
        {
            bool isP1 = (Object.InputAuthority.PlayerId == 1) || (Object.InputAuthority == Runner.LocalPlayer && Runner.IsServer);
            GameUIManager.Instance.UpdateAvatarUI(isP1, SelectedAvatarIndex);
        }
    }


    // =================================================================================================
    // 6. CÁC HÀM LOGIC CHI TIẾT
    // =================================================================================================

    private void HandleMovement(NetworkInputData data)
    {
        // Tính toán tốc độ (bao gồm làm chậm từ Item)
        float speedMult = 1f;
        var hp = GetComponent<HealthComponent>();
        if (hp) speedMult = hp.SpeedMultiplier;

        float currentSpeed = runSpeed * speedMult; 
        if (data.buttons.IsSet(EInputButtons.SPRINT)) currentSpeed = sprintSpeed * speedMult;
        else if (data.buttons.IsSet(EInputButtons.WALK)) currentSpeed = walkSpeed * speedMult;

        Vector3 moveDirection = (transform.forward * data.moveDirection.y) + (transform.right * data.moveDirection.x);
        moveDirection.Normalize(); 
        Vector3 finalMove = (moveDirection * currentSpeed) + new Vector3(0, verticalVelocity, 0);
        controller.Move(finalMove * Runner.DeltaTime);
    }

    private void HandleGravityAndJump(NetworkInputData data)
    {
        if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
        if (data.buttons.WasPressed(ButtonsPrev, EInputButtons.JUMP) && controller.isGrounded)
            verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
        verticalVelocity += gravity * Runner.DeltaTime;
    }

    private void HandlePetMovement(NetworkInputData data)
    {
        if (currentPet == null) return;
        
        float speedMult = 1f;
        var hp = currentPet.GetComponent<HealthComponent>();
        if (hp) speedMult = hp.SpeedMultiplier;

        float currentPetSpeed = runSpeed * speedMult;
        if (data.buttons.IsSet(EInputButtons.SPRINT)) currentPetSpeed = sprintSpeed * speedMult;

        Vector3 moveDirection = (currentPet.transform.forward * data.moveDirection.y) + (currentPet.transform.right * data.moveDirection.x);
        moveDirection.Normalize();
        currentPet.MovePet(moveDirection * currentPetSpeed * Runner.DeltaTime); 
    }

    private void HandleMountingProcess(NetworkInputData data) { }

    private void HandleMouseAndCamera()
    {
        // Nếu mở Menu -> Không xoay camera
        if (RadialMenuBridge.IsMenuOpen) 
        {
            if(Cursor.lockState != CursorLockMode.None) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            return; 
        }

        if (Mouse.current == null) return;

        // Chỉ xoay khi GIỮ CHUỘT PHẢI
        if (Mouse.current.rightButton.isPressed)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            float mouseY = mouseDelta.y * mouseSensitivity * 0.05f;

            // CHỈ XOAY CAMERA (TRỤC X)
            // Việc xoay nhân vật (Trục Y) đã được xử lý trong FixedUpdateNetwork dựa trên Input
            if (_localCamera != null)
            {
                cameraPitch -= mouseY;
                cameraPitch = Mathf.Clamp(cameraPitch, cameraPitchMin, cameraPitchMax);
                _localCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandleAnimation()
    {
        if (animator == null) return;
        animator.SetFloat("MovementSpeed", _netAnimSpeed, 0.1f, Time.deltaTime);
        animator.SetBool("isMounted", isMounted);
    }


    // =================================================================================================
    // 7. RPCs & ACTIONS
    // =================================================================================================

    // --- ITEM SYSTEM ---
    public void TryUseItem() { if (itemCount > 0) Rpc_UseItemAction(); }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_UseItemAction()
    {
        if (!ItemCooldownTimer.ExpiredOrNotRunning(Runner)) return;
        if (itemCount > 0)
        {
            itemCount--; 
            ItemCooldownTimer = TickTimer.CreateFromSeconds(Runner, itemCooldownTime);

            // Dùng Item theo ID
            if (SelectedItemID == 0) // Máu
            {
                var hp = GetComponent<HealthComponent>();
                if (hp != null) hp.Heal(50);
            }
            else if (SelectedItemID == 1) // RainStone
            {
                if (itemPrefabs.Count > 1 && itemPrefabs[1].IsValid)
                {
                    // Spawn tại vị trí nhân vật
                    Vector3 spawnPos = transform.position; 
                    NetworkObject rainObj = Runner.Spawn(itemPrefabs[1], spawnPos, Quaternion.identity, Object.InputAuthority);
                    if(rainObj) rainObj.GetComponent<RainArea>().Init(Object);
                }
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] private void Rpc_SetItemData(int itemID) { SelectedItemID = itemID; }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] private void Rpc_SetItemCount(int count) { itemCount = count; }

    // --- SKILLS ---

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_UseShield()
    {
        if (ShieldCooldownTimer.ExpiredOrNotRunning(Runner))
        {
            var hp = GetComponent<HealthComponent>();
            if (hp != null) 
            {
                hp.ActivateShield(); 
                ShieldCooldownTimer = TickTimer.CreateFromSeconds(Runner, shieldCooldownTime);
                Rpc_PlaySound(1);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] public void Rpc_PlayEmote(int emoteID) { Rpc_ShowEmoteVisual(emoteID); }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_ShowEmoteVisual(int emoteID)
    {
        // Debug.Log($"Player {PlayerName} emote: {emoteID}");
        
        if (GameUIManager.Instance != null)
        {
            // 1. Xác định xem người thả emote là P1 hay P2
            // (Logic giống hệt thanh máu)
            bool isP1 = (Object.InputAuthority.PlayerId == 1) || (Object.InputAuthority == Runner.LocalPlayer && Runner.IsServer);
            
            // 2. Gọi UI hiện Emote
            GameUIManager.Instance.ShowEmote(isP1, emoteID);
        }
    }    [Rpc(RpcSources.StateAuthority, RpcTargets.All)] private void Rpc_PlaySound(int soundType) { AudioClip clip = null; switch (soundType) { case 0: clip = summonSound; break; case 1: clip = mountSound; break; case 2: clip = commandSound; break; } if (_audioSource != null && clip != null) _audioSource.PlayOneShot(clip); }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] private void Rpc_SetPlayerData(string name, int petIndex, int avatarIndex) 
    { 
        this.PlayerName = name; 
        this.SelectedPetIndex = petIndex; 
        this.SelectedAvatarIndex = avatarIndex; 
        gameObject.name = name; 
    }

    // --- PET ACTIONS ---

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_ToggleSummonPet()
    {
        if (currentPet == null)
        {
            if (petOptions == null || petOptions.Count == 0) return;
            if (SelectedPetIndex < 0 || SelectedPetIndex >= petOptions.Count) SelectedPetIndex = 0; 

            Vector3 spawnPos = transform.position + (transform.forward * 2f) + (Vector3.up * 0.5f);
            NetworkObject petObj = Runner.Spawn(petOptions[SelectedPetIndex], spawnPos, transform.rotation, Object.InputAuthority);
            currentPet = petObj.GetComponent<PetAI>();
            if(currentPet != null) currentPet.player = this.transform;
            isMounted = false;
            Rpc_PlaySound(0);
        }
        else
        {
            if (isMounted) Rpc_ToggleMount(); 
            NetworkObject netObj = currentPet.GetComponent<NetworkObject>();
            if(netObj != null) Runner.Despawn(netObj);
            currentPet = null;
            Rpc_PlaySound(1);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_ToggleMount()
    {
        if (currentPet == null) return;
        if (!isMounted) { isMounted = true; controller.enabled = false; currentPet.CommandMount(); }
        else { isMounted = false; controller.enabled = true; currentPet.CommandUnmount(); transform.position = currentPet.transform.position + currentPet.transform.right * 1.5f; }
        Rpc_PlaySound(1);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] private void Rpc_CommandAttack() { if (currentPet) { currentPet.CommandAttack(); Rpc_PlaySound(2); } }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] private void Rpc_CommandSkill1() { if (currentPet) { currentPet.CommandSkill1(); Rpc_PlaySound(2); } }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] private void Rpc_CommandSkill2() { if (currentPet) { currentPet.CommandSkill2(); Rpc_PlaySound(2); } }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] private void Rpc_CommandMoveToPlayer() { if (currentPet) currentPet.CommandMoveToPlayer(); }
    
    public void Server_ForceSpawnPet() { if (Runner.IsServer && currentPet == null) Rpc_ToggleSummonPet(); }
}