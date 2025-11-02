using UnityEngine;
using System;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(PlayerState))]
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
        if (playerState == null) Debug.LogError("[PlayerCollisionHandler] PlayerState가 없습니다.");
        if (boxCollider == null) Debug.LogError("[PlayerCollisionHandler] BoxCollider가 없습니다.");
    }

    private void OnCollisionEnter(Collision collision)
    {
        // playerState 미초기화 방어
        if (playerState == null) return;

        if (collision.gameObject.CompareTag(SeaTag))
        {
            OnSeaCollision?.Invoke(transform.position);
            return;
        }

        if (collision.gameObject.CompareTag(BlockTag) && playerState.IsAirborne)
        {
            var sinkBlock = collision.gameObject.GetComponent<SinkBlock>();
            if(sinkBlock != null)
            {
                if (sinkBlock.isNotLanded)
                {
                    sinkBlock.CollisionPlayer();
                    var acc = CalculateLandingAccuracy(transform.position, collision.collider, sinkBlock);
                    OnLanded?.Invoke(acc);
                }
                else
                {
                    OnLanded?.Invoke(LandingAccuracy.Excep);
                }
            }
            else
            {
                OnLanded?.Invoke(LandingAccuracy.Excep);
            }
        }
    }

    LandingAccuracy CalculateLandingAccuracy(Vector3 landingPosition, Collider blockCollider, SinkBlock blockScript)
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