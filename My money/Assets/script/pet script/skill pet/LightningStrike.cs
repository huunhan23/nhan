using UnityEngine;
using Fusion;

public class LightningStrike : NetworkBehaviour
{
    [Header("Thông số Vụ Nổ")]
    public float radius = 3f;
    public float damage = 25f;
    public float knockbackForce = 7f;
    public float lifeTime = 2f;

    [Networked] private TickTimer DespawnTimer { get; set; }

    public override void Spawned()
    {
        // Chỉ Server mới chạy logic gây dame
        if (Object.HasStateAuthority)
        {
            DespawnTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
            DoDamage();
        }
        AudioSource audio = GetComponent<AudioSource>();
        if (audio != null)
        {
            // Thay đổi cao độ (Pitch) ngẫu nhiên một chút để tiếng sét nghe tự nhiên hơn
            audio.pitch = Random.Range(0.9f, 1.1f); 
            audio.Play();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            if (DespawnTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
        }
    }

    
    private void DoDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var hit in hits)
        {
            // === SỬA ĐOẠN NÀY: Chấp nhận cả "Enemy" và "Player" ===
            if (hit.CompareTag("Enemy") || hit.CompareTag("Player"))
            {
                // 1. Gây sát thương
                var hp = hit.GetComponent<HealthComponent>();
                if (hp != null)
                {
                    hp.TakeDamage(damage);
                }

                // 2. Đẩy lùi (giữ nguyên)
                Rigidbody enemyRb = hit.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    Vector3 knockbackDir = (hit.transform.position - transform.position).normalized;
                    knockbackDir.y = 0.1f;
                    enemyRb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
                }
            }
            // =======================================================
        }
    }
}