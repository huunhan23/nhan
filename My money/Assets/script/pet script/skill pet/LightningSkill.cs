using UnityEngine;
using Fusion; // Cần Fusion

public class LightningSkill : PetSkillBase
{
    [Header("Thông số Skill 2")]
    public NetworkPrefabRef lightningPrefab; // Đổi thành NetworkPrefabRef
    public Transform shootPoint;

    public override void ExecuteSkill()
    {
        // Chỉ Server mới được quyền Spawn đạn
        if (petAI.Runner.IsServer == false) return;

        if (petAI == null || lightningPrefab.IsValid == false) return;

        // 1. Lấy điểm nhắm từ Player (đã sync aimPoint)
        PlayerController playerController = petAI.player.GetComponent<PlayerController>();
        if (playerController == null) return;
        Vector3 targetPoint = playerController.aimPoint;

        // 2. Quay mặt
        Vector3 lookPosition = new Vector3(targetPoint.x, petAI.transform.position.y, targetPoint.z);
        petAI.transform.LookAt(lookPosition);

        // 3. Tính toán vị trí
        Vector3 spawnPos = (shootPoint != null) ? shootPoint.position : petAI.transform.position + petAI.transform.forward;
        Vector3 shootDirection = (targetPoint - spawnPos).normalized;

        // 4. SPAWN BẰNG FUSION
        NetworkObject boltObj = petAI.Runner.Spawn(
            lightningPrefab, 
            spawnPos, 
            Quaternion.LookRotation(shootDirection), 
            petAI.Object.InputAuthority
        );

        // 5. Khởi tạo đạn (Truyền thông tin phe ta)
        if (boltObj != null)
        {
            LightningBolt boltScript = boltObj.GetComponent<LightningBolt>();
            // Lấy NetworkObject của Pet và Player
            NetworkObject petNetObj = petAI.Object;
            NetworkObject playerNetObj = playerController.Object;
            
            boltScript.Init(petNetObj, playerNetObj);
        }
    }
}