using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Để dùng List

// Kế thừa từ PetSkillBase
public class DashAttackSkill : PetSkillBase 
{
    [Header("Thông số Skill 1")]
    public float dashSpeed = 20f;      // Tốc độ lao
    public float dashDuration = 0.5f;  // Lao trong bao lâu (giây)
    public float dashDamage = 10f;     // Sát thương
    public float knockbackForce = 5f;  // Lực đẩy lùi
    public float collisionRadius = 1.5f; // Bán kính va chạm

    // Khởi tạo (được gọi bởi PetAI)
    public override void Initialize(PetAI owner)
    {
        base.Initialize(owner); // Gọi hàm Initialize của lớp cha
    }

    // Hàm thực thi skill (được gọi bởi PetAI khi nhấn T)
    public override void ExecuteSkill()
    {
        if (petAI == null || animator == null) return;

        // Bắt đầu Coroutine để xử lý việc "Lao"
        petAI.StartCoroutine(DashCoroutine());
    }

    // Coroutine xử lý việc lao tới
    private IEnumerator DashCoroutine()
    {
        // 1. Chuẩn bị
        petAI.currentState = PetAI.PetState.UsingSkill; // Đổi trạng thái
        petAI.SetAgentEnabled(false);// Tắt NavMeshAgent (quan trọng)
        animator.SetTrigger("shout"); // Tạm dùng anim "shout" cho cú lao

        float startTime = Time.time;
        Vector3 dashDirection = petAI.transform.forward; // Lao về phía mặt pet

        // 2. Thực hiện Lao (trong X giây)
        while (Time.time < startTime + dashDuration)
        {
            // Di chuyển pet
            petAI.transform.Translate(dashDirection * dashSpeed * Time.deltaTime, Space.World);

            // Tìm tất cả địch trong 1 quả cầu xung quanh pet
            Collider[] hits = Physics.OverlapSphere(petAI.transform.position, collisionRadius);
            foreach (var hit in hits)
            {
                // Nếu là "Enemy"
                if (hit.CompareTag("Enemy"))
                {
                    Debug.Log("Lao trúng: " + hit.name);

                    // TODO: Gây sát thương
                    // hit.GetComponent<EnemyHealth>().TakeDamage(dashDamage);

                    // Đẩy lùi địch
                    Rigidbody enemyRb = hit.GetComponent<Rigidbody>();
                    if (enemyRb != null)
                    {
                        Vector3 knockbackDir = (hit.transform.position - petAI.transform.position).normalized;
                        knockbackDir.y = 0.2f; // Hơi nảy lên 1 chút
                        enemyRb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
                    }

                    // === DÒNG MỚI ĐƯỢC THÊM ===
                    // Dừng Coroutine ngay lập tức sau khi húc trúng
                    Debug.Log("Đã va chạm, dừng cú lao!");
                    yield break; 
                    // === KẾT THÚC DÒNG MỚI ===
                }
            }
            
            yield return null; // Chờ frame tiếp theo
        }

        // 3. Kết thúc (Chỉ chạy khi hết giờ mà không trúng ai)
        petAI.SetAgentEnabled(true); // Bật lại NavMeshAgent
        petAI.currentState = PetAI.PetState.Following; // Quay về trạng thái đi theo
    }
}