using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class RainArea : NetworkBehaviour
{
    [Header("Thông số")]
    public float radius = 5f;
    public float lifeTime = 5f;
    public float slowAmount = 0.5f; // Giảm còn 50% tốc độ

    [Networked] private TickTimer LifeTimer { get; set; }
    private NetworkObject ownerObject; // Người ném (để không làm chậm đồng đội)

    public void Init(NetworkObject owner)
    {
        ownerObject = owner;
        LifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsServer) return;

        // 1. Tự hủy khi hết giờ
        if (LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        // 2. Quét vùng ảnh hưởng liên tục
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        
        foreach (var hit in hits)
        {
            // Chỉ tác động lên Enemy hoặc Player khác
            if (hit.CompareTag("Enemy") || hit.CompareTag("Player"))
            {
                NetworkObject hitNetObj = hit.GetComponent<NetworkObject>();
                
                // Bỏ qua phe ta (Nếu muốn mưa làm chậm cả địch lẫn ta thì xóa dòng này)
                if (hitNetObj == ownerObject) continue; 
                // (Lưu ý: Logic team phức tạp hơn cần check ID, tạm thời check owner)

                var status = hit.GetComponent<HealthComponent>();
                if (status != null)
                {
                    // Áp dụng trạng thái: Ướt và Chậm
                    status.SetStatus(true, slowAmount);
                }
            }
        }
    }

    // Khi vùng mưa biến mất, phải trả lại trạng thái bình thường cho những ai đang bị ướt?
    // Cách đơn giản nhất: Trong HealthComponent hoặc PlayerController, tự reset status nếu không còn đứng trong mưa.
    // Tuy nhiên, để đơn giản cho bạn, ta sẽ dùng OnTriggerExit (yêu cầu Prefab có Sphere Collider Trigger).
    
    void OnTriggerExit(Collider other)
    {
        if (!Runner.IsServer) return;
        
        var status = other.GetComponent<HealthComponent>();
        if (status != null)
        {
            // Hết mưa -> Khô ráo, Tốc độ về 1
            status.SetStatus(false, 1.0f);
        }
    }
}