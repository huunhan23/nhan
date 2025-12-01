using UnityEngine;

public abstract class PetSkillBase : MonoBehaviour
{
    protected PetAI petAI; 
    protected Animator animator;

    [Header("Âm thanh Skill")]
    public AudioClip castSound; // Biến chứa âm thanh

    // Hàm khởi tạo
    public virtual void Initialize(PetAI owner)
    {
        this.petAI = owner;
        this.animator = owner.animator; 
    }

    // Hàm trừu tượng (Chỉ khai báo 1 lần duy nhất ở đây)
    public abstract void ExecuteSkill(); 
}