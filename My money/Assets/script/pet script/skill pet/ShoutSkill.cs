using UnityEngine;

// Thay vì "MonoBehaviour", nó kế thừa từ "PetSkillBase"
public class ShoutSkill : PetSkillBase 
{
    // "override" nghĩa là nó "viết đè" lên hàm ExecuteSkill rỗng của khuôn mẫu
    public override void ExecuteSkill()
    {
        // Kiểm tra an toàn
        if (petAI == null || animator == null)
        {
            Debug.LogError("Skill chưa được Initialize!");
            return;
        }

        // === ĐÂY LÀ LOGIC CỦA RIÊNG SKILL NÀY ===
        Debug.Log("PET DÙNG SKILL 1 (SHOUT)!");
        
        // Kích hoạt animation "shout"
        animator.SetTrigger("shout");

        // Bạn có thể thêm các logic khác ở đây
        // Ví dụ: Tạo hiệu ứng âm thanh, hiệu ứng hạt (particle)...
        // currentState = PetState.UsingSkill; (Bạn có thể gọi hàm public trong PetAI)
    }
}