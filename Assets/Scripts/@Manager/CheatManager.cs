using UnityEngine;

public class CheatManager : MonoBehaviour
{
    private PlayerController playerController;
    private BlockManager blockManager;

    public void Initiate(PlayerController player, BlockManager block)
    {
        gameObject.SetActive(true);
        this.playerController = player;
        this.blockManager = block;
    }

    public void ExecuteTeleportCheat(int checkpointIndex)
    {
        Vector3 targetPos;
        if (checkpointIndex == 0)
        {
            targetPos = playerController.GetPlayerSpawnPos();
        }
        else
        {
            targetPos = blockManager.GetCheckPointXPos(checkpointIndex);
        }

        if (targetPos == Vector3.zero)
        {
            Debug.LogWarning($"[Cheat] 체크포인트 {checkpointIndex} 정보를 찾을 수 없습니다.");
            return;
        }

        Debug.Log($"[Cheat] Teleport to Checkpoint {checkpointIndex} : {targetPos.x},{targetPos}");

        playerController.ForceTeleport(targetPos);
    }

    public void PlusCombo()
    {
        playerController.PlusCombo();
    }

    public void ResetCombo()
    {
        playerController.ResetCombo();
    }
}