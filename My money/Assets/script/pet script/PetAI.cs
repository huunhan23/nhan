using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Fusion;

[RequireComponent(typeof(NavMeshAgent))]
public class PetAI : NetworkBehaviour 
{
    // === CÁC BIẾN TRẠNG THÁI ===
    public enum PetState { Following, Attacking, MovingToPlayer, UsingSkill, Mounted, Idle, Dead } 
    
    [Header("Trạng thái hiện tại")]
    public PetState currentState;

    // === CÁC BIẾN CÀI ĐẶT ===
    [Header("Các đối tượng")]
    public Transform player; 
    private Transform currentTarget; 
    [Header("Mounting")]
    public Transform mountPoint; 
    
    [Header("Thông số AI")]
    public float attackRange = 2.0f;     
    public float followDistance = 3.0f;  
    public float leashDistance = 15.0f;  
    public float targetScanRadius = 20f; 
    public float arrivalDistance = 1.5f;

    // === HÌNH ẢNH UI ===
    [Header("UI Images")]
    public Sprite petAvatar;    // Ảnh đại diện Pet
    public Sprite skill1Icon;   // Icon chiêu 1
    public Sprite skill2Icon;   // Icon chiêu 2

    // === SKILL & COOLDOWN ===
    [Header("Skill Slots")]
    public PetSkillBase skill1;
    public PetSkillBase skill2;

    [Header("Skill Cooldowns")]
    public float skill1CooldownTime = 3.0f; 
    public float skill2CooldownTime = 5.0f; 

    // Timer đồng bộ qua mạng
    [Networked] private TickTimer Skill1Timer { get; set; }
    [Networked] private TickTimer Skill2Timer { get; set; }

    // === BIẾN NỘI BỘ ===
    private NavMeshAgent agent;
    [HideInInspector] public Animator animator; 
    private AudioSource audioSource;
    public float turnSpeed = 10f; 

    // ========================================================================
    // 1. KHỞI TẠO (DÙNG SPAWNED THAY CHO START)
    // ========================================================================
    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null) player = foundPlayer.transform;
        }

        currentState = PetState.Following; 

        if (skill1 != null) skill1.Initialize(this);
        if (skill2 != null) skill2.Initialize(this);

        // === GỬI ẢNH CHO UI ===
        if (Object.HasInputAuthority)
        {
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.SetupPetUI(petAvatar, skill1Icon, skill2Icon);
            }
        }
    }

    // ========================================================================
    // 2. CẬP NHẬT LOGIC (FIXED UPDATE NETWORK)
    // ========================================================================
    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsServer) return; // Chỉ Server chạy AI

        if (player == null || currentState == PetState.Dead) return; 

        switch (currentState)
        {
            case PetState.Following: HandleFollowing(); break;
            case PetState.Attacking: HandleAttacking(); break;
            case PetState.MovingToPlayer: HandleMovingToPlayer(); break;
            case PetState.Mounted: agent.isStopped = true; break;
            case PetState.Idle: agent.isStopped = true; break;
        }
    }

    // ========================================================================
    // 3. CẬP NHẬT VISUAL & UI (RENDER)
    // ========================================================================
    public override void Render()
    {
        // Cập nhật Animation
        if (animator != null && agent != null)
        {
            float currentRealSpeed = agent.velocity.magnitude; 
            float maxSpeed = agent.speed;
            float normalizedSpeed = (maxSpeed > 0) ? (currentRealSpeed / maxSpeed) : 0;
            animator.SetFloat("speed", normalizedSpeed, 0.1f, Time.deltaTime);
        }

        // Cập nhật UI Cooldown (Chỉ trên máy chủ nhân)
        if (Object.HasInputAuthority && GameUIManager.Instance != null)
        {
            float remaining1 = 0;
            if (Skill1Timer.IsRunning && !Skill1Timer.Expired(Runner))
                remaining1 = (float)Skill1Timer.RemainingTime(Runner);
            
            GameUIManager.Instance.UpdateSkillCooldown(1, remaining1, skill1CooldownTime);

            float remaining2 = 0;
            if (Skill2Timer.IsRunning && !Skill2Timer.Expired(Runner))
                remaining2 = (float)Skill2Timer.RemainingTime(Runner);

            GameUIManager.Instance.UpdateSkillCooldown(2, remaining2, skill2CooldownTime);
        }
    }

    // ========================================================================
    // CÁC HÀNH VI AI
    // ========================================================================

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
        if (currentTarget == null) { currentState = PetState.Following; return; }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        if (distanceToTarget <= attackRange)
        {
            agent.isStopped = true;
            animator.SetTrigger("Attack"); 
            Vector3 lookPos = new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z);
            transform.LookAt(lookPos);
        }
        else
        {
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

    // ========================================================================
    // CÁC LỆNH COMMAND
    // ========================================================================

    public void CommandAttack()
    {
        if (currentState == PetState.Dead) return;
        FindNearestTarget(); 
        if (currentTarget != null) currentState = PetState.Attacking; 
        Rpc_PlaySoundEffect(3); // Tiếng gầm tấn công
    }

    public void CommandSkill1()
    {
        if (currentState == PetState.Dead) return;

        if (Skill1Timer.ExpiredOrNotRunning(Runner))
        {
            if (skill1 != null)
            {
                Rpc_PlayCastSound(1); // Tiếng skill
                skill1.ExecuteSkill();
                Skill1Timer = TickTimer.CreateFromSeconds(Runner, skill1CooldownTime);
            }
        }
    }

    public void CommandSkill2()
    {
        if (currentState == PetState.Dead) return;
        
        if (Skill2Timer.ExpiredOrNotRunning(Runner))
        {
            if (skill2 != null)
            {
                Rpc_PlayCastSound(2); 
                skill2.ExecuteSkill();
                Skill2Timer = TickTimer.CreateFromSeconds(Runner, skill2CooldownTime);
            }
        }
    }

    public void CommandMoveToPlayer()
    {
        if (currentState == PetState.Dead) return;
        currentState = PetState.MovingToPlayer;
    }
    
    public void CommandMount() { currentState = PetState.Mounted; agent.isStopped = true; agent.enabled = true; }
    public void CommandUnmount() { agent.isStopped = false; currentState = PetState.Following; }

    // ========================================================================
    // HÀM TIỆN ÍCH
    // ========================================================================

    public void MovePet(Vector3 moveVector)
    {
        if(agent.enabled) agent.Move(moveVector);
    }
    
    public void SetAgentEnabled(bool status) { agent.enabled = status; }
    public void SetAgentStopped(bool status) { agent.isStopped = status; }
    public bool IsPetGrounded() { return agent != null && agent.isOnNavMesh; }

    private void FindNearestTarget()
    {
        List<GameObject> potentialTargets = new List<GameObject>();
        potentialTargets.AddRange(GameObject.FindGameObjectsWithTag("Enemy"));
        potentialTargets.AddRange(GameObject.FindGameObjectsWithTag("Player"));

        float closestDistance = Mathf.Infinity;
        GameObject nearestTarget = null;

        foreach (GameObject target in potentialTargets)
        {
            if (target.transform == this.player) continue; 
            if (target == this.gameObject) continue; 

            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < targetScanRadius && distance < closestDistance)
            {
                closestDistance = distance;
                nearestTarget = target;
            }
        }

        if (nearestTarget != null) currentTarget = nearestTarget.transform;
        else currentTarget = null;
    }

    // ========================================================================
    // RPCS
    // ========================================================================

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_PlaySoundEffect(int soundType)
    {
        AudioClip clipToPlay = null;
        if (soundType == 1 && skill1 != null) clipToPlay = skill1.castSound;
        if (soundType == 2 && skill2 != null) clipToPlay = skill2.castSound;
        if (audioSource != null && clipToPlay != null) audioSource.PlayOneShot(clipToPlay);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_PlayCastSound(int skillIndex)
    {
        AudioClip clip = null;
        if (skillIndex == 1 && skill1 != null) clip = skill1.castSound;
        if (skillIndex == 2 && skill2 != null) clip = skill2.castSound;
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

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
}