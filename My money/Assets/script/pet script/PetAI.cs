using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic; // Đảm bảo có dòng này
using Fusion;

[RequireComponent(typeof(NavMeshAgent))]
public class PetAI :NetworkBehaviour
{
    // === CÁC BIẾN ===
    public enum PetState { Following, Attacking, MovingToPlayer, UsingSkill, Mounted, Idle, Dead } 
    
    [Header("Trạng thái hiện tại")]
    public PetState currentState;
[Header("Skill Cooldowns")]
    public float skill1CooldownTime = 3.0f; // Hồi chiêu T
    public float skill2CooldownTime = 5.0f; // Hồi chiêu Y

    // Timer cho từng skill
    [Networked] private TickTimer Skill1Timer { get; set; }
    [Networked] private TickTimer Skill2Timer { get; set; }
    [Header("Các đối tượng")]
    public Transform player; 
    private Transform currentTarget; 
    [Header("Mounting")]
    public Transform mountPoint; 
    [Header("Thông số")]
    public float attackRange = 2.0f;     
    public float followDistance = 3.0f;  
    public float leashDistance = 15.0f;  
    public float targetScanRadius = 20f; 
    public float arrivalDistance = 1.5f;


    // === BIẾN NỘI BỘ ===
    private NavMeshAgent agent;
    private AudioSource audioSource;
    [HideInInspector] public Animator animator;
    private float currentSpeed = 0f; 
    public float turnSpeed = 10f; 
[Header("Skill Slots")]
    public PetSkillBase skill1;
    public PetSkillBase skill2;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        
        animator = GetComponentInChildren<Animator>(); 
        if(animator == null)
        {
            Debug.LogError("Pet không tìm thấy Animator!");
        }

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }
        currentState = PetState.Following; 

        if (skill1 != null) skill1.Initialize(this);
        if (skill2 != null) skill2.Initialize(this);
    }

    void Update()
    {
        if (player == null || currentState == PetState.Dead) return; // Nếu chết thì không làm gì cả

        // Đây là bộ não (State Machine) của Pet
        switch (currentState)
        {
            case PetState.Following:
                HandleFollowing();
                break;
            case PetState.Attacking:
                HandleAttacking();
                break;
            case PetState.MovingToPlayer:
                HandleMovingToPlayer();
                break;
            case PetState.Mounted:
                agent.isStopped = true; // Đảm bảo dừng lại
                break;
            case PetState.Idle:
                agent.isStopped = true;
                break;
            // (Các state khác)
        }

        // === PHẦN SỬA ĐỔI ĐỂ SỬA LỖI ANIMATION ===
        
        // 1. Lấy tốc độ mong muốn (ví dụ: 3.5)
        float currentRealSpeed = agent.velocity.magnitude; // Lấy vận tốc thực
        float maxSpeed = agent.speed;
        float normalizedSpeed = (maxSpeed > 0) ? (currentRealSpeed / maxSpeed) : 0;
        
        animator.SetFloat("speed", normalizedSpeed, 0.1f, Time.deltaTime);
    }
    // === CÁC HÀNH VI ===

    private void HandleFollowing()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > followDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;
        }
    }

    private void HandleAttacking()
    {
        if (currentTarget == null)
        {
            currentState = PetState.Following;
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        
        // Debug Log (nếu cần)
        // Debug.Log("Đang đuổi " + currentTarget.name + ". Khoảng cách: " + distanceToTarget + " | Tầm đánh: " + attackRange);

        if (distanceToTarget <= attackRange)
        {
            // Đã đến tầm, dừng lại và tấn công
            agent.isStopped = true;
            
            animator.SetTrigger("Attack"); // Kích hoạt animation Attack
            
            // Sửa lỗi "Cắm đầu"
            Vector3 lookPosition = new Vector3(currentTarget.position.x, 
                                               transform.position.y, 
                                               currentTarget.position.z);
            // Xoay mượt
            Quaternion targetRotation = Quaternion.LookRotation(lookPosition - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
        else
        {
            // Nếu ngoài tầm, đuổi theo
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
        }
    }

    private void HandleMovingToPlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);

        if (Vector3.Distance(transform.position, player.position) < arrivalDistance)
        {
            currentState = PetState.Following;
        }
    }

    // === CÁC LỆNH (COMMAND) GỌI TỪ PLAYER ===
    // (Đây là các hàm đang bị thiếu)

    // Lệnh R (Tấn công thường)
    public void CommandAttack()
    {
        if (currentState == PetState.Dead) return;
        FindNearestTarget(); // <<<< LỖI CỦA BẠN CÓ THỂ BẮT ĐẦU TỪ ĐÂY
        if (currentTarget != null)
        {
            currentState = PetState.Attacking; 
        }
    }

    // Lệnh T (Skill 1)
    public void CommandSkill1()
    {
        if (currentState == PetState.Dead) return;

        // === CHECK COOLDOWN ===
        if (Skill1Timer.ExpiredOrNotRunning(Runner))
        {
            // Nếu skill tồn tại và thực hiện thành công
            if (skill1 != null)
            {
                 Rpc_PlayCastSound(1);
                skill1.ExecuteSkill();
                Rpc_PlaySoundEffect(1);
               
                
                // Bắt đầu đếm ngược
                Skill1Timer = TickTimer.CreateFromSeconds(Runner, skill1CooldownTime);
            }
            else
            {
                Debug.LogWarning("Pet không có Skill 1!");
            }
        }
        else
        {
            Debug.Log("Skill 1 đang hồi! Còn: " + Skill1Timer.RemainingTime(Runner).Value.ToString("F1"));
        }
    }

    // Lệnh Y (Skill 2)
    public void CommandSkill2()
    {
        if (currentState == PetState.Dead) return;

        // === CHECK COOLDOWN ===
        if (Skill2Timer.ExpiredOrNotRunning(Runner))
        {
            if (skill2 != null)
            {
                Rpc_PlayCastSound(2);
                skill2.ExecuteSkill();
                Rpc_PlaySoundEffect(2);
                // Bắt đầu đếm ngược
                Skill2Timer = TickTimer.CreateFromSeconds(Runner, skill2CooldownTime);
            }
            else
            {
                Debug.LogWarning("Pet không có Skill 2!");
            }
        }
        else
        {
            Debug.Log("Skill 2 đang hồi! Còn: " + Skill2Timer.RemainingTime(Runner).Value.ToString("F1"));
        }
    }

    // Lệnh E (Đến gần Player)
    public void CommandMoveToPlayer() // <<<< HÀM BỊ THIẾU
    {
        if (currentState == PetState.Dead) return;
        currentState = PetState.MovingToPlayer;
    }
    
    // Lệnh F (Chuẩn bị được cưỡi)
    public void CommandMount() // <<<< HÀM BỊ THIẾU
    {
        currentState = PetState.Mounted;
        agent.isStopped = true; 
        agent.enabled = true; 
    }

    // Lệnh F (Thả Pet ra)
    public void CommandUnmount() // <<<< HÀM BỊ THIẾU
    {
        agent.isStopped = false; 
        currentState = PetState.Following;
    }

    // === CÁC HÀM TIỆN ÍCH ===

    // Hàm MỚI để PlayerController gọi
    public void MovePet(Vector3 moveVelocity)
    {
        // agent.Move cho phép di chuyển Agent mà không cần SetDestination
        // moveVelocity ở đây là: Hướng * Tốc Độ * DeltaTime
        if (agent.enabled)
        {
            agent.Move(moveVelocity);
            
            // Xoay Pet theo hướng di chuyển (nếu có di chuyển)
            if (moveVelocity.sqrMagnitude > 0.1f)
            {
                // Chỉ lấy hướng, bỏ qua độ lớn
                Quaternion lookRotation = Quaternion.LookRotation(moveVelocity.normalized);
                // Xoay mượt mà
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);
            }
        }
    }

    // Hàm MỚI để Player kiểm tra
    public bool IsPetGrounded() // <<<< HÀM BỊ THIẾU
    {
        if (agent == null) return false;
        return agent.isOnNavMesh;
    }
public void SetAgentEnabled(bool status)
    {
        agent.enabled = status;
    }

    // Hàm này để các skill ra lệnh Dừng/Tiếp tục
    public void SetAgentStopped(bool status)
    {
        agent.isStopped = status;
    }
    private void FindNearestTarget() // <<<< HÀM BỊ THIẾU
    {
        // 1. Tạo một danh sách rỗng
        List<GameObject> potentialTargets = new List<GameObject>();

        // 2. Tìm "Enemy"
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        potentialTargets.AddRange(enemies);

        // 3. Tìm "Player"
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        potentialTargets.AddRange(players);

        // 4. Bắt đầu tìm
        float closestDistance = Mathf.Infinity;
        GameObject nearestTarget = null;

        foreach (GameObject target in potentialTargets)
        {
            // BỎ QUA 1: Nếu là người chơi (chủ)
            if (target.transform == this.player)
            {
                continue; 
            }

            // BỎ QUA 2: Nếu là BẢN THÂN PET
            if (target == this.gameObject)
            {
                continue; 
            }

            // 5. Tính toán
            float distance = Vector3.Distance(transform.position, target.transform.position);
            
            // 6. So sánh
            if (distance < targetScanRadius && distance < closestDistance)
            {
                closestDistance = distance;
                nearestTarget = target;
            }
        }

        // 7. Gán mục tiêu
        if (nearestTarget != null)
        {
            currentTarget = nearestTarget.transform;
        }
        else
        {
            currentTarget = null;
        }
    }

    // === HÀM MỚI (ĐỂ DÙNG SAU) ===

    public void TakeDamage(int damage)
    {
        if (currentState == PetState.Dead) return;
        animator.SetTrigger("GetHit");
    }

    public void Die()
    {
        currentState = PetState.Dead;
        agent.isStopped = true;
        agent.enabled = false; 
        animator.SetBool("isDead", true);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_PlaySoundEffect(int soundType)
    {
        // soundType: 1 = Skill 1, 2 = Skill 2, 3 = Attack Lệnh
        AudioClip clipToPlay = null;

        if (soundType == 1 && skill1 != null) clipToPlay = skill1.castSound;
        if (soundType == 2 && skill2 != null) clipToPlay = skill2.castSound;
        // Bạn có thể thêm soundType 3 cho tiếng gầm khi tấn công thường

        if (audioSource != null && clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_PlayCastSound(int skillIndex)
    {
        // skillIndex: 1 = Skill 1, 2 = Skill 2
        AudioClip clip = null;

        if (skillIndex == 1 && skill1 != null) clip = skill1.castSound;
        if (skillIndex == 2 && skill2 != null) clip = skill2.castSound;

        if (audioSource != null && clip != null)
        {
            // PlayOneShot để có thể đè lên các âm thanh khác nếu cần
            audioSource.PlayOneShot(clip);
        }
    }
}