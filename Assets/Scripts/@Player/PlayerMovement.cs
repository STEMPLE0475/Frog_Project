using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("점프 설정")]
    [SerializeField] private Vector3 jumpDirection = new Vector3(1f, 0f, 1f);
    [SerializeField] public float jumpDuration = 1.2f;
    [SerializeField] private float heightMultiplier = 1f;

    private Rigidbody rb;
    private PlayerState playerState;

    [SerializeField] private LineRenderer trajectoryLine; // 인스펙터에서 할당
    [SerializeField] private int lineSegmentCount = 30; // 궤적의 부드러움

    public void Initiate()
    {
        rb = GetComponent<Rigidbody>();
        playerState = GetComponent<PlayerState>();

        if (trajectoryLine == null)
            trajectoryLine = GetComponent<LineRenderer>();
        HideTrajectory(); // 시작 시 궤적 숨기기
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

    public void UpdateTrajectory(float currentJumpForce)
    {
        if (trajectoryLine == null) return;

        // 1. 현재 조건 (점프 힘, 바람)으로 궤적 포인트 계산
        Vector3 currentWind = GetWindForceVector();
        Vector3[] points = CalculateTrajectoryPoints(currentJumpForce, currentWind, transform.position);

        // 2. LineRenderer에 포인트 설정 및 활성화
        trajectoryLine.positionCount = points.Length;
        trajectoryLine.SetPositions(points);
        trajectoryLine.enabled = true;
    }

    public void HideTrajectory()
    {
        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
        }
    }
    private Vector3[] CalculateTrajectoryPoints(float jumpForce, Vector3 windForce, Vector3 startPos)
    {
        List<Vector3> points = new List<Vector3>();

        // 코루틴의 계산식과 동일해야 함
        Vector3 endPos = startPos + jumpDirection.normalized * jumpForce + Vector3.up * 0.5f;
        float jumpHeight = jumpForce * heightMultiplier;

        // 'for' 루프를 사용하여 코루틴의 'while' 로직을 한 프레임에 시뮬레이션
        for (int i = 0; i <= lineSegmentCount; i++)
        {
            float progress = (float)i / lineSegmentCount; // 0.0 ~ 1.0 (코루틴의 progress)
            float elapsedTime = progress * jumpDuration;  // 0.0 ~ jumpDuration (코루틴의 elapsedTime)

            // 1. 기본 위치 (Vector3.Lerp)
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);

            // 2. 높이 (Mathf.Sin)
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * jumpHeight;

            // 3. 바람 (누적)
            // 코루틴: accumulatedWindMovement += windForce * Time.deltaTime;
            // 시뮬레이션 (적분 결과): accumulatedWindMovement = windForce * elapsedTime;
            Vector3 accumulatedWindMovement = windForce * elapsedTime;

            // 4. 최종 위치
            points.Add(currentPos + accumulatedWindMovement);
        }

        return points.ToArray();
    }

}