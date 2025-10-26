using UnityEngine;
using System;

[RequireComponent(typeof(BoxCollider))]
public class PlayerCollisionHandler : MonoBehaviour
{
    // PlayerController가 구독할 이벤트
    public event Action<LandingAccuracy> OnLanded;
    public event Action<Vector3> OnSeaCollision; // 충돌 위치 전달

    private PlayerState playerState;
    private BoxCollider boxCollider;

    private const string SeaTag = "Sea";
    private const string BlockTag = "Block";

    public void Initiate()
    {
        playerState = GetComponent<PlayerState>();
        boxCollider = GetComponent<BoxCollider>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(SeaTag))
        {
            OnSeaCollision?.Invoke(transform.position); // "바다와 충돌!"
            return;
        }

        if (collision.gameObject.CompareTag(BlockTag) && playerState.IsAirborne)
        {
            Block blockScript = collision.gameObject.GetComponent<Block>();
            if (blockScript == null) return;
           
            if (blockScript.blockType == BlockType.Sink)
            {
                if (blockScript.isComboable == true)
                {
                    blockScript.CollisionPlayer();
                    LandingAccuracy accuracy = CalculateLandingAccuracy(transform.position, collision.collider, blockScript);
                    OnLanded?.Invoke(accuracy);
                }
                else
                {
                    OnLanded?.Invoke(LandingAccuracy.Excep);
                }
                
            } else if (blockScript.blockType == BlockType.Normal)
            {
                blockScript.CollisionPlayer();
                OnLanded?.Invoke(LandingAccuracy.Excep);
            }
        }
    }

    LandingAccuracy CalculateLandingAccuracy(Vector3 landingPosition, Collider blockCollider, Block blockScript)
    {
        BoxCollider box = blockCollider as BoxCollider;
        if (box == null) return LandingAccuracy.Bad;

        Vector3 localLandingPos = blockCollider.transform.InverseTransformPoint(landingPosition);
        float distanceX = Mathf.Abs(localLandingPos.x);
        float distanceZ = Mathf.Abs(localLandingPos.z);
        float normalizedX = distanceX / (box.size.x / 2f + float.Epsilon);
        float normalizedZ = distanceZ / (box.size.z / 2f + float.Epsilon);
        float finalNormalizedDistance = Mathf.Max(normalizedX, normalizedZ);

        if (finalNormalizedDistance <= blockScript.perfectThreshold) return LandingAccuracy.Perfect;
        if (finalNormalizedDistance <= blockScript.goodThreshold) return LandingAccuracy.Good;
        return LandingAccuracy.Bad;
    }

    public void SetCollider(bool enabled)
    {
        boxCollider.enabled = enabled;
    }
}