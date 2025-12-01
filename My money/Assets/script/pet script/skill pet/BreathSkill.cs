using UnityEngine;
using Fusion;

public class BreathSkill : PetSkillBase
{
    [Header("Thông số Skill Phun")]
    public NetworkPrefabRef breathPrefab; // Kéo Prefab lửa vào đây
    public Transform mouthPoint;          // Vị trí miệng (ShootPoint)
    public float duration = 3f;           // Thời gian phun (để khớp animation)

    public override void ExecuteSkill()
    {
        if (!petAI.Runner.IsServer) return; // Chỉ Server xử lý

        // 1. Chạy Animation Phun (gồng)
        if (animator) animator.SetTrigger("shout"); // Dùng tạm anim shout hoặc tạo anim mới "breath"

        // 2. Spawn luồng lửa
        Vector3 spawnPos = (mouthPoint != null) ? mouthPoint.position : petAI.transform.position + petAI.transform.forward;
        Quaternion spawnRot = (mouthPoint != null) ? mouthPoint.rotation : petAI.transform.rotation;

        NetworkObject breathObj = petAI.Runner.Spawn(breathPrefab, spawnPos, spawnRot, petAI.Object.InputAuthority);

        // 3. Gắn luồng lửa vào miệng Pet (Để pet quay đầu thì lửa quay theo)
        if (breathObj != null)
        {
            // Dùng NetworkParent để gắn kết cha-con qua mạng
            // Lưu ý: Prefab lửa phải có NetworkTransform và tích Sync Parent
            breathObj.GetComponent<NetworkTransform>().transform.SetParent(mouthPoint);
            
            // Khởi tạo thông tin người bắn
            BreathDamageArea breathScript = breathObj.GetComponent<BreathDamageArea>();
            NetworkObject playerObj = petAI.player.GetComponent<NetworkObject>();
            
            breathScript.Init(petAI.Object, playerObj);
        }

        // 4. Dừng Pet lại khi đang phun (tùy chọn)
        petAI.StartCoroutine(StopPetDuringBreath());
    }

    private System.Collections.IEnumerator StopPetDuringBreath()
    {
        petAI.SetAgentStopped(true);
        yield return new WaitForSeconds(duration);
        petAI.SetAgentStopped(false);
        // Reset về trạng thái đi theo sau khi phun xong
        petAI.currentState = PetAI.PetState.Following;
    }
}