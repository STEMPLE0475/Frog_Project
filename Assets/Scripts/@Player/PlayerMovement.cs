using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("점프 설정")]
    [SerializeField] private Vector3 jumpDirection = new Vector3(1f, 0f, 1f);
    [SerializeField] public float jumpDuration = 1.2f;
    [SerializeField] private float heightMultiplier = 1f;

    private Rigidbody rb;
    private PlayerState playerState;

    public void Initiate()
    {
        rb = GetComponent<Rigidbody>();
        playerState = GetComponent<PlayerState>();
    }

    // PlayerController가 이 메서드를 호출
    public void ExecuteJump(float jumpForce, Action onJumpStartCallback)
    {
        if (playerState.IsAirborne) return;
        StartCoroutine(ParabolicJump(jumpForce, onJumpStartCallback));
    }

    private IEnumerator ParabolicJump(float jumpForce, Action onJumpStartCallback)
    {
        onJumpStartCallback?.Invoke(); // 점프 시작 콜백 (이펙트 재생용)
        playerState.SetAirborne(true);

        Vector3 startPos = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 endPos = startPos + jumpDirection.normalized * jumpForce + Vector3.up * 0.5f;

        float elapsedTime = 0f;
        float jumpHeight = jumpForce * heightMultiplier;

        while (elapsedTime < jumpDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / jumpDuration);

            float currentAngle = 360f * progress;
            transform.rotation = startRotation * Quaternion.Euler(currentAngle, 0, 0);

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            transform.position = currentPos;

            yield return null;
        }
        transform.rotation = startRotation;
    }

    public void ResetVelocity()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}