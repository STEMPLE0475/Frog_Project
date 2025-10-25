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
        StartCoroutine(ParabolicJump(jumpForce, onJumpStartCallback, GetWindForceVector()));
    }

    private IEnumerator ParabolicJump(float jumpForce, Action onJumpStartCallback, Vector3 windForce)
    {
        onJumpStartCallback?.Invoke(); // 점프 시작 콜백 (이펙트 재생용)
        playerState.SetAirborne(true);

        Vector3 startPos = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 endPos = startPos + jumpDirection.normalized * jumpForce + Vector3.up * 0.5f;

        float elapsedTime = 0f;
        float jumpHeight = jumpForce * heightMultiplier;

        Vector3 accumulatedWindMovement = Vector3.zero;

        while (elapsedTime < jumpDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / jumpDuration);

            float currentAngle = 360f * progress;
            transform.rotation = startRotation * Quaternion.Euler(currentAngle, 0, 0);

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * jumpHeight;

            accumulatedWindMovement += windForce * Time.deltaTime;

            transform.position = currentPos + accumulatedWindMovement;

            yield return null;
        }
        transform.rotation = startRotation;
    }
    public Vector3 GetWindForceVector()
    {
        Wind windData = playerState.GetWind();

        if (windData == null) return Vector3.zero;

        float windDirectionFactor = (float)windData.direction;
        float windPower = windData.power;
        float finalWindMagnitude = windDirectionFactor * windPower;

        Vector3 windDirectionVector = new Vector3(1f, 0f, 1f).normalized;
        Vector3 windForce = windDirectionVector * finalWindMagnitude;

        return windForce;
    }

    public void ResetVelocity()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}