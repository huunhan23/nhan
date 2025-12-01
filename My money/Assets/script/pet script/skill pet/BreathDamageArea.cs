using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class BreathDamageArea : NetworkBehaviour
{
    [Header("Thông số")]
    public float damagePerTick = 5f;   // Sát thương mỗi lần nhảy số
    public float tickRate = 0.5f;      // Bao lâu thì nhảy số 1 lần (0.5s)
    public float lifeTime = 3f;        // Thời gian tồn tại của chiêu

    // Danh sách kẻ địch đang đứng trong lửa
    private List<GameObject> targetsInRange = new List<GameObject>();
    
    [Networked] private TickTimer LifeTimer { get; set; }
    [Networked] private TickTimer DamageTickTimer { get; set; }
    
    // Ai là người bắn (để không tự gây dame cho phe mình)
    private NetworkObject ownerPet;
    private NetworkObject ownerPlayer;

    public void Init(NetworkObject pet, NetworkObject player)
    {
        ownerPet = pet;
        ownerPlayer = player;
        
        // Bắt đầu đếm ngược thời gian tồn tại
        LifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
        // Bắt đầu đếm ngược lần gây dame đầu tiên
        DamageTickTimer = TickTimer.CreateFromSeconds(Runner, tickRate);
    }

    public override void FixedUpdateNetwork()
    {
        // Chỉ Server mới tính toán sát thương
        if (!Runner.IsServer) return;

        // 1. Kiểm tra hết giờ tồn tại -> Hủy
        if (LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        // 2. Tính toán gây dame theo chu kỳ (Tick)
        if (DamageTickTimer.Expired(Runner))
        {
            DealDamageToAll();
            // Reset timer cho lần sau
            DamageTickTimer = TickTimer.CreateFromSeconds(Runner, tickRate);
        }
    }

    // Xử lý va chạm: Ai bước vào thì thêm vào danh sách
    void OnTriggerEnter(Collider other)
    {
        if (!Runner.IsServer) return;

        // Bỏ qua phe ta
        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj == ownerPet || netObj == ownerPlayer) return;

        if (other.CompareTag("Enemy") || other.CompareTag("Player")) // Tùy logic game bạn
        {
            if (!targetsInRange.Contains(other.gameObject))
            {
                targetsInRange.Add(other.gameObject);
            }
        }
    }

    // Xử lý va chạm: Ai bước ra thì xóa khỏi danh sách
    void OnTriggerExit(Collider other)
    {
        if (!Runner.IsServer) return;

        if (targetsInRange.Contains(other.gameObject))
        {
            targetsInRange.Remove(other.gameObject);
        }
    }

    // Hàm gây dame cho tất cả mục tiêu trong danh sách
    // Hàm gây dame cho tất cả mục tiêu trong danh sách
    private void DealDamageToAll()
    {
        // Duyệt ngược để an toàn
        for (int i = targetsInRange.Count - 1; i >= 0; i--)
        {
            GameObject target = targetsInRange[i];

            if (target == null)
            {
                targetsInRange.RemoveAt(i);
                continue;
            }

            Debug.Log("Gây " + damagePerTick + " sát thương lửa lên: " + target.name);
            
            // === SỬA ĐOẠN NÀY ===
            var hp = target.GetComponent<HealthComponent>();
            if (hp != null)
            {
                hp.TakeDamage(damagePerTick);
            }
            // ====================
        }
    }
}