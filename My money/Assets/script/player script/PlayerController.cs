using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic; // <--- QUAN TRỌNG: Để dùng List<>
using TMPro; // <--- QUAN TRỌNG: Để dùng TextMeshPro

public class PlayerController : NetworkBehaviour
{
    // =================================================================================================
    // 1. CÁC BIẾN ĐỒNG BỘ (NETWORKED VARIABLES)
    // =================================================================================================
    
    [Networked] public NetworkBool isMounted { get; set; }
    [Networked] public NetworkBool isMounting { get; set; }
    [Networked] public PetAI currentPet { get; set; }
    [Networked] public Vector3 aimPoint { get; set; }
    
    // Lưu trạng thái nút bấm frame trước để kiểm tra "vừa mới bấm"
    [Networked] private NetworkButtons ButtonsPrev { get; set; }
    
    // Lưu Index của Pet đã chọn (gửi từ Menu)
    [Networked] public int SelectedPetIndex { get; set; } 
    
    // Lưu tên người chơi
    [Networked] public NetworkString<_16> PlayerName { get; set; }

    // Timer hồi chiêu triệu hồi
    [Networked] private TickTimer SummonTimer { get; set; }

    // Số lượng Item (Đồng bộ) - CHO RADIAL MENU
    [Networked] public int itemCount { get; set; } 


    // =================================================================================================
    // 2. CÁC BIẾN CÀI ĐẶT (INSPECTOR)
    // =================================================================================================

    [Header("UI")]
    public TextMeshProUGUI nameTextDisplay; // Kéo Text tên trên đầu vào đây

    [Header("Player Movement")]
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.0f;
    public float sprintSpeed = 10.0f;
    public float jumpForce = 8.0f;
    public float gravity = -20.0f;

    [Header("Cooldown Settings")]
    public float summonCooldownTime = 2.0f;

    [Header("Camera Control")]
    public float mouseSensitivity = 10.0f;
    public Transform cameraTransform; 
    public float cameraPitchMin = -45.0f; 
    public float cameraPitchMax = 80.0f;  

    [Header("Pet Management")]
    // DANH SÁCH CÁC PET (Thay vì 1 con)
    public List<NetworkPrefabRef> petOptions; 
    public Transform petSpawnPoint; 

    [Header("Player Audio")]
    public AudioClip summonSound;   
    public AudioClip mountSound;    
    public AudioClip commandSound;  
    
    // Biến nội bộ (Local)
    private CharacterController controller;
    private Animator animator;
    private float verticalVelocity = 0.0f; 
    private float cameraPitch = 0.0f;
    private Camera _localCamera; 
    private ChangeDetector _changes;
    private AudioSource _audioSource;


    // =================================================================================================
    // 3. KHỞI TẠO (SPAWNED)
    // =================================================================================================
    public override void Spawned()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>(); 
        _localCamera = GetComponentInChildren<Camera>();
        _audioSource = GetComponent<AudioSource>();
        
        // Khởi tạo bộ phát hiện thay đổi
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

        // Tự tìm camera nếu chưa gán
        if (cameraTransform == null && Camera.main != null) 
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
            cameraPitch = cameraTransform.localEulerAngles.x;

        // Cấu hình riêng cho máy của người chơi (Input Authority)
        if (Object.HasInputAuthority)
        {
            // === MÁY CỦA MÌNH ===
            if (_localCamera) 
            {
                _localCamera.enabled = true;
                var listener = _localCamera.GetComponent<AudioListener>();
                if(listener) listener.enabled = true;
            }

            // --- TỰ ĐỘNG LOAD DỮ LIỆU TỪ MENU KHI VÀO GAME ---
            string savedName = PlayerPrefs.GetString("PlayerName", "Player");
            int savedPetIndex = PlayerPrefs.GetInt("SelectedPet", 0);
            
            // Load Item
            int savedItemCount = PlayerPrefs.GetInt("Selected_Item_Count", 3); // Mặc định 3 cái

            // Gửi lệnh lên Server để cập nhật tên và pet cho nhân vật này
            Rpc_SetPlayerData(savedName, savedPetIndex);
            Rpc_SetItemCount(savedItemCount); // Cập nhật số lượng item
        }
        else
        {
            // === MÁY NGƯỜI KHÁC ===
            if (_localCamera) 
            {
                _localCamera.enabled = false;
                var listener = _localCamera.GetComponent<AudioListener>();
                if(listener) listener.enabled = false;
            }
        }
        
        UpdateNameUI();
    }


    // =================================================================================================
    // 4. LOGIC MẠNG (FIXED UPDATE NETWORK) - CHẠY TRÊN SERVER & CLIENT
    // =================================================================================================
    public override void FixedUpdateNetwork()
    {
        // Lấy Input từ NetworkRunnerHandler
        if (GetInput(out NetworkInputData data))
        {
            // --- A. XỬ LÝ NÚT BẤM (Dùng ButtonsPrev để check WasPressed) ---

            // 1. Triệu hồi (H)
            if (data.buttons.WasPressed(ButtonsPrev, EInputButtons.SUMMON))
            {
                if (SummonTimer.ExpiredOrNotRunning(Runner))
                {
                    SummonTimer = TickTimer.CreateFromSeconds(Runner, summonCooldownTime);
                    Rpc_ToggleSummonPet();
                }
            }

            // 2. Cưỡi (F)
            if (data.buttons.WasPressed(ButtonsPrev, EInputButtons.MOUNT))    
                Rpc_ToggleMount();
            
            // 3. Các Skill Pet (Chỉ khi có Pet và KHÔNG cưỡi)
            if (currentPet != null && !isMounted && !isMounting)
            {
                if (data.buttons.WasPressed(ButtonsPrev, EInputButtons.ATTACK))   Rpc_CommandAttack();
                if (data.buttons.WasPressed(ButtonsPrev, EInputButtons.SKILL1))   Rpc_CommandSkill1();
                if (data.buttons.WasPressed(ButtonsPrev, EInputButtons.SKILL2))   Rpc_CommandSkill2();
                if (data.buttons.WasPressed(ButtonsPrev, EInputButtons.CALL_PET)) Rpc_CommandMoveToPlayer();
            }

            // --- B. ĐỒNG BỘ AIM POINT ---
            aimPoint = data.aimPoint;

            // --- C. XỬ LÝ DI CHUYỂN (CHỈ SERVER THỰC HIỆN) ---
            if (Runner.IsServer)
            {
                if (isMounting)
                {
                    HandleMountingProcess(data); 
                }
                else if (isMounted)
                {
                    // === SỬA LỖI KHI CƯỠI: ÉP VỊ TRÍ ===
                    if (currentPet != null && currentPet.mountPoint != null)
                    {
                        controller.enabled = false; 
                        transform.position = currentPet.mountPoint.position;
                        transform.rotation = currentPet.mountPoint.rotation;
                        HandlePetMovement(data);
                    }
                }
                else
                {
                    controller.enabled = true;
                    HandleMovement(data);
                    HandleGravityAndJump(data);
                }
            }

            // --- D. LƯU TRẠNG THÁI NÚT ĐỂ SO SÁNH LẦN SAU ---
            ButtonsPrev = data.buttons;
        }
        
        // --- E. CẬP NHẬT ANIMATION (CHẠY TRÊN MỌI MÁY) ---
        HandleAnimation();
    }

    // Logic Update cho Camera & UI (Chạy mượt mà trên Client)
    public override void Render()
    {
        // 1. Xử lý Camera (Chỉ máy mình)
        if (Object.HasInputAuthority)
        {
            HandleMouseAndCamera();
        }

        // 2. Xử lý Name Tag (Tất cả máy đều chạy để thấy tên nhau)
        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(PlayerName))
            {
                UpdateNameUI(); 
            }
        }

        if (nameTextDisplay != null && Camera.main != null)
        {
            nameTextDisplay.transform.parent.rotation = Camera.main.transform.rotation;
        }
    }

    private void UpdateNameUI()
    {
        if (nameTextDisplay != null) 
            nameTextDisplay.text = PlayerName.ToString();
    }


    // =================================================================================================
    // 5. CÁC HÀM XỬ LÝ CHI TIẾT (LOGIC DI CHUYỂN)
    // =================================================================================================

    private void HandleMovement(NetworkInputData data)
    {
        float currentSpeed = runSpeed; 
        if (data.buttons.IsSet(EInputButtons.SPRINT)) currentSpeed = sprintSpeed;
        else if (data.buttons.IsSet(EInputButtons.WALK)) currentSpeed = walkSpeed;

        Vector3 moveDirection = (transform.forward * data.moveDirection.y) + (transform.right * data.moveDirection.x);
        moveDirection.Normalize(); 

        Vector3 finalMove = (moveDirection * currentSpeed) + new Vector3(0, verticalVelocity, 0);
        controller.Move(finalMove * Runner.DeltaTime);
    }

    private void HandleGravityAndJump(NetworkInputData data)
    {
        if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
        
        // Nhảy (Space)
        if (data.buttons.WasPressed(ButtonsPrev, EInputButtons.JUMP) && controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
        verticalVelocity += gravity * Runner.DeltaTime;
    }

    private void HandlePetMovement(NetworkInputData data)
    {
        if (currentPet == null) return;

        // Điều khiển Pet di chuyển
        float currentPetSpeed = runSpeed;
        if (data.buttons.IsSet(EInputButtons.SPRINT)) currentPetSpeed = sprintSpeed;

        Vector3 moveDirection = (currentPet.transform.forward * data.moveDirection.y) + (currentPet.transform.right * data.moveDirection.x);
        moveDirection.Normalize();

        currentPet.MovePet(moveDirection * currentPetSpeed * Runner.DeltaTime); 
    }

    private void HandleMountingProcess(NetworkInputData data)
    {
        // Logic leo lên (Tạm thời để trống)
    }

    private void HandleMouseAndCamera()
    {
        if (Mouse.current == null) return;

        // Chỉ xoay khi giữ chuột phải
        if (Mouse.current.rightButton.isPressed)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            float mouseX = mouseDelta.x * mouseSensitivity * 0.05f; 
            float mouseY = mouseDelta.y * mouseSensitivity * 0.05f;

            Transform objectToRotate = (isMounted && currentPet != null) ? currentPet.transform : this.transform;
            objectToRotate.Rotate(Vector3.up * mouseX);

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
        
        Vector3 velocity = controller.velocity;
        velocity.y = 0; 
        float currentSpeed = velocity.magnitude;
        float speedPercent = currentSpeed / runSpeed;
        
        animator.SetFloat("MovementSpeed", speedPercent, 0.1f, Time.deltaTime);
        animator.SetBool("isMounted", isMounted);
    }


    // =================================================================================================
    // 6. RPCs (REMOTE PROCEDURE CALLS) - CLIENT GỬI LỆNH LÊN SERVER
    // =================================================================================================

    // RPC: Phát âm thanh chung
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlaySound(int soundType)
    {
        AudioClip clipToPlay = null;
        switch (soundType)
        {
            case 0: clipToPlay = summonSound; break;
            case 1: clipToPlay = mountSound; break;
            case 2: clipToPlay = commandSound; break;
        }
        if (_audioSource != null && clipToPlay != null) _audioSource.PlayOneShot(clipToPlay);
    }

    // RPC: Cập nhật thông tin người chơi
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_SetPlayerData(string name, int petIndex)
    {
        this.PlayerName = name;
        this.SelectedPetIndex = petIndex;
        gameObject.name = name; 
    }

    // RPC: Cập nhật số lượng item
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_SetItemCount(int count)
    {
        itemCount = count;
    }

    // RPC: Triệu hồi / Thu hồi Pet
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_ToggleSummonPet()
    {
        if (currentPet == null)
        {
            if (petOptions == null || petOptions.Count == 0) return;
            if (SelectedPetIndex < 0 || SelectedPetIndex >= petOptions.Count) SelectedPetIndex = 0; 

            // Spawn trước mặt 2m
            Vector3 spawnPos = transform.position + (transform.forward * 2f) + (Vector3.up * 0.5f);
            
            NetworkObject petObj = Runner.Spawn(petOptions[SelectedPetIndex], spawnPos, transform.rotation, Object.InputAuthority);
            
            currentPet = petObj.GetComponent<PetAI>();
            if(currentPet != null) currentPet.player = this.transform;

            isMounted = false;
            
            Rpc_PlaySound(0); // Tiếng Summon
        }
        else
        {
            if (isMounted) Rpc_ToggleMount(); 
            
            NetworkObject netObj = currentPet.GetComponent<NetworkObject>();
            if(netObj != null) Runner.Despawn(netObj);
            
            currentPet = null;
            Rpc_PlaySound(1); // Tiếng Unsummon
        }
    }

    // RPC: Cưỡi / Xuống
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_ToggleMount()
    {
        if (currentPet == null) return;

        if (!isMounted)
        {
            isMounted = true;
            controller.enabled = false; 
            currentPet.CommandMount();  
        }
        else
        {
            isMounted = false;
            controller.enabled = true;  
            currentPet.CommandUnmount(); 
            transform.position = currentPet.transform.position + currentPet.transform.right * 1.5f;
        }
        Rpc_PlaySound(1); // Tiếng Mount
    }

    // --- CÁC HÀM RADIAL MENU ---

    public void TryUseItem()
    {
        if (itemCount > 0) Rpc_UseItemAction();
        else Debug.Log("Hết item!");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_UseItemAction()
    {
        if (itemCount > 0)
        {
            itemCount--; 
            var hp = GetComponent<HealthComponent>();
            if (hp != null) hp.Heal(50); 
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_UseHeal()
    {
        var hp = GetComponent<HealthComponent>();
        if (hp != null) hp.Heal(20);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_PlayEmote() { Rpc_ShowEmoteVisual(); }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_ShowEmoteVisual() { Debug.Log($"Player {PlayerName} emote!"); }

// RPC: Chơi Emote (Có tham số ID)
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_PlayEmote(int emoteID)
    {
        Rpc_ShowEmoteVisual(emoteID);
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_ShowEmoteVisual(int emoteID)
    {
        // TODO: Dựa vào ID để spawn prefab icon tương ứng trên đầu
        // Ví dụ: if (emoteID == 0) Spawn(SmileIcon)...
        Debug.Log($"Player {PlayerName} thả emote số: {emoteID}");
    }
    // --- CÁC HÀM SKILL PET ---

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] 
    private void Rpc_CommandAttack() { if (currentPet) { currentPet.CommandAttack(); Rpc_PlaySound(2); } }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] 
    private void Rpc_CommandSkill1() { if (currentPet) { currentPet.CommandSkill1(); Rpc_PlaySound(2); } }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] 
    private void Rpc_CommandSkill2() { if (currentPet) { currentPet.CommandSkill2(); Rpc_PlaySound(2); } }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] 
    private void Rpc_CommandMoveToPlayer() { if (currentPet) currentPet.CommandMoveToPlayer(); }
    
    // Hàm cho GameTimer gọi
    public void Server_ForceSpawnPet()
    {
        if (Runner.IsServer && currentPet == null) Rpc_ToggleSummonPet();
    }
}