using UnityEngine;
using System.Collections;
using Fusion;

public class LightningStrikeSkill : PetSkillBase
{
    [Header("Thông số Skill Sét")]
    public NetworkPrefabRef lightningStrikePrefab; // Đổi thành NetworkPrefabRef
    public float maxCastRange = 15f;
    public float castTime = 1f;

    public override void ExecuteSkill()
    {
        // Chỉ Server xử lý
        if (!petAI.Runner.IsServer) return;

        PlayerController playerController = petAI.player.GetComponent<PlayerController>();
        if (playerController == null) return;
        Vector3 targetPoint = playerController.aimPoint;

        float distance = Vector3.Distance(petAI.transform.position, targetPoint);
        if (distance > maxCastRange) return;

        // Bắt đầu Coroutine trên PetAI
        petAI.StartCoroutine(CastCoroutine(targetPoint));
    }

    private IEnumerator CastCoroutine(Vector3 strikePosition)
    {
        // Chuẩn bị
        petAI.SetAgentStopped(true); 
        Vector3 lookPosition = new Vector3(strikePosition.x, petAI.transform.position.y, strikePosition.z);
        petAI.transform.LookAt(lookPosition);
        
        // Anim (Trigger anim phải được đồng bộ qua NetworkMecanimAnimator hoặc RPC, nhưng tạm thời cứ gọi local)
        if(animator) animator.SetTrigger("shout");

        yield return new WaitForSeconds(castTime);

        // SPAWN BẰNG FUSION
        if (lightningStrikePrefab.IsValid)
        {
            petAI.Runner.Spawn(lightningStrikePrefab, strikePosition, Quaternion.identity);
        }

        petAI.SetAgentStopped(false);
        petAI.currentState = PetAI.PetState.Following;
    }
}