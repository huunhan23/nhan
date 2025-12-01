using UnityEngine;
using Fusion;

public class LightningBolt : NetworkBehaviour
{
    [Header("Thông số")]
    public float speed = 20f;
    public float lifeTime = 3f;
    public float damage = 15f;

    // === BIẾN ĐỒNG BỘ (Viết Hoa Chữ Cái Đầu) ===
    [Networked] public NetworkObject ShooterPet { get; set; }
    [Networked] public NetworkObject ShooterPlayer { get; set; }
    
    [Networked] private TickTimer LifeTimer { get; set; }

    // Hàm khởi tạo
    public void Init(NetworkObject pet, NetworkObject player)
    {
        ShooterPet = pet;
        ShooterPlayer = player;
        LifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
    }

    public override void FixedUpdateNetwork()
    {
        if (Runner.IsServer)
        {
            // Di chuyển
            transform.position += transform.forward * speed * Runner.DeltaTime;

            // Hết giờ thì hủy
            if (LifeTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
        }
    }

    // Xử lý va chạm (Server Only)
    void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        // Lấy NetworkObject của vật bị trúng (nếu có)
        NetworkObject otherNetObj = other.GetComponent<NetworkObject>();
        
        // === SỬA LỖI TẠI ĐÂY (Dùng tên biến Viết Hoa) ===
        
        // 1. Kiểm tra: Nếu trúng PET CỦA MÌNH -> Bỏ qua
        if (otherNetObj != null && otherNetObj == ShooterPet) return;
        
        // 2. Kiểm tra: Nếu trúng CHỦ CỦA MÌNH -> Bỏ qua
        if (otherNetObj != null && otherNetObj == ShooterPlayer) return;
        
        // ================================================

        // 3. Kiểm tra trúng ĐỊCH hoặc PLAYER KHÁC
        if (other.CompareTag("Enemy") || other.CompareTag("Player"))
        {
            Debug.Log("Tia điện trúng: " + other.name);
            
            // Gây sát thương
            var health = other.GetComponent<HealthComponent>();
            if(health != null)
            {
                health.TakeDamage(damage);
            }

            Runner.Despawn(Object); // Hủy đạn
        }
        
        // 4. Trúng tường/đất
        if (other.CompareTag("Ground") || other.CompareTag("Wall")) 
        {
             // Runner.Despawn(Object); // Bỏ comment nếu muốn đạn biến mất khi chạm đất
        }
    }
}