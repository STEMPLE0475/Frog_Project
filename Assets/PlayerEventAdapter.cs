// PlayerEventAdapter.cs
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PlayerController))]
public class PlayerEventAdapter : MonoBehaviour
{
    private PlayerController player;

    [Header("Combo Events")]
    public UnityEvent<int, Vector3> OnCombo_ComboPlayerPos;
    public UnityEvent<int> OnCombo_Combo;

    [Header("Land Events")]
    public UnityEvent<LandingAccuracy> OnLand_Acc;
    public UnityEvent<int> OnLand_Combo;
    public UnityEvent<Vector3> OnLand_PlayerPos;
    public UnityEvent<int> OnLand_sessionLandCount;

    void Awake()
    {
        player = GetComponent<PlayerController>();

        player.OnCombo += HandleCombo;
        player.OnLanded += HandleLand;
    }

    private void HandleCombo(int combo, Vector3 pos)
    {
        OnCombo_ComboPlayerPos.Invoke(combo, pos);
        OnCombo_Combo.Invoke(combo);
    }

    private void HandleLand(LandingAccuracy acc, int combo, Vector3 playerPos, int sessionLandCount)
    {
        OnLand_Acc.Invoke(acc);
        OnLand_Combo.Invoke(combo);
        OnLand_PlayerPos.Invoke(playerPos);
        OnLand_sessionLandCount.Invoke(sessionLandCount);
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.OnCombo -= HandleCombo;
            player.OnLanded -= HandleLand;
        }
    }
}