using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    private bool _inputEnabled = true;
    public List<ParticleSystem> particles;
    private GameManager gameManager;

    #region 변수 선언

    // 점프 지속 시간 (초)
    private float jumpDuration = 1.2f;
    // 점프 거리(currentJumpForce)에 비례한 높이 계수
    private float heightMultiplier = 1f;

    [Header("점프 파워 설정")]
    [SerializeField] private float minJumpForce = 4f;
    [SerializeField] private float maxJumpForce = 15f;
    [SerializeField] private float chargeRate = 9f;

    [Header("점프 방향 설정")]
    [SerializeField] private Vector3 jumpDirection = new Vector3(1f, 3f, 1f);

    [Header("착지 판정 범위 (0~1)")]
    [Range(0f, 1f)][SerializeField] private float perfectThreshold = 0.3f;
    [Range(0f, 1f)][SerializeField] private float goodThreshold = 0.8f;

    // --- 애니메이션 설정 ---
    [Header("점프 애니메이션 설정")]
    [Tooltip("힘을 모을 때 Y축으로 눌리는 정도")]
    [SerializeField] private float squashAmount = 0.5f;
    [Tooltip("점프 직후 Y축으로 늘어나는 정도")]
    [SerializeField] private float stretchAmount = 1.5f;
    [Tooltip("애니메이션이 원래 크기로 돌아오는 속도")]
    [SerializeField] private float animationSpeed = 2.2f;

    private Transform playerSpawnPos;
    private Rigidbody rb;
    private float currentJumpForce;
    private bool isCharging = false;
    private Keyboard keyboard;

    // --- 애니메이션용 내부 변수 ---
    private Vector3 originalScale;
    private Coroutine scaleAnimationCoroutine;

    // ↓↓↓↓↓↓ 공중 상태 추적 변수 추가 ↓↓↓↓↓↓
    private bool isAirborne = false; // 플레이어가 공중에 떠 있는지 여부

    [SerializeField] private PlayerInput _playerInput;
    #endregion

    #region Event
    public event Action OnJumpStart;
    public event Action OnLand;
    #endregion


    public void Initiate(GameManager gameManager, Transform playerSpawnPos)
    {
        this.gameManager = gameManager;
        this.playerSpawnPos = playerSpawnPos;
        rb = GetComponent<Rigidbody>();
        keyboard = Keyboard.current;
        currentJumpForce = minJumpForce;

        originalScale = transform.localScale;
        isAirborne = false; // 초기에는 땅에 붙어있는 상태
    }
    public void EnableInput(bool on)
    {
        _inputEnabled = on;

        if (_playerInput == null) _playerInput = GetComponent<PlayerInput>();
        if (_playerInput != null)
        {
            if (on) _playerInput.ActivateInput();
            else _playerInput.DeactivateInput();
        }
    }

    void Update()
    {
        if (!_inputEnabled) return;

        if (keyboard == null) return;

        HandleInput();
    }

    private void HandleInput()
    {
        // 스페이스바를 처음 눌렀을 때
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            isCharging = true;
            currentJumpForce = minJumpForce;

            if (scaleAnimationCoroutine != null) StopCoroutine(scaleAnimationCoroutine);
            scaleAnimationCoroutine = StartCoroutine(AnimateScale(new Vector3(originalScale.x, originalScale.y * squashAmount, originalScale.z)));
        }

        // 스페이스바를 누르고 있는 동안
        if (keyboard.spaceKey.isPressed && isCharging)
        {
            currentJumpForce += chargeRate * Time.deltaTime;
            currentJumpForce = Mathf.Clamp(currentJumpForce, minJumpForce, maxJumpForce);
        }

        // 스페이스바에서 손을 뗐을 때
        if (keyboard.spaceKey.wasReleasedThisFrame && isCharging && !isAirborne)
        {
            isCharging = false;
            StartJump();

            if (scaleAnimationCoroutine != null) StopCoroutine(scaleAnimationCoroutine);
            scaleAnimationCoroutine = StartCoroutine(AnimateJumpStretch());
        }
    }

    private void StartJump()
    {
        if (isAirborne) return;
        StartCoroutine(ParabolicJump());
    }

    private IEnumerator ParabolicJump()
    {
        OnJumpStart?.Invoke();

        isAirborne = true; 

        Vector3 startPos = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 endPos = startPos + jumpDirection.normalized * currentJumpForce;

        float elapsedTime = 0f;
        float jumpHeight = currentJumpForce * heightMultiplier;

        while (elapsedTime < jumpDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / jumpDuration;
            progress = Mathf.Clamp01(progress);

            float currentAngle = 360f * progress;
            transform.rotation = startRotation * Quaternion.Euler(currentAngle, 0, 0);

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            transform.position = currentPos;

            yield return null;
        }

        // 점프가 끝난 후, 회전 값을 시작 값으로 완벽하게 복원
        transform.rotation = startRotation;

        isAirborne = false;
    }

    // --- 착지 및 충돌 처리 ---

    private void Land(int accuracy)
    {
        OnLand?.Invoke();
        // 공중에 떠 있지 않으면 착지 판정하지 않음 (이중 호출 방지 및 정확한 시점 제어) 
        if (!isAirborne) return;

        isAirborne = false; // 착지했으니 공중에 떠 있지 않은 상태로 설정

        AllParticleStop();
        switch (accuracy)
        {
            case 2: // Perfect 
                Debug.Log("PERFECT! (정확도: 2)");
                if (particles.Count > 0 && particles[2] != null) particles[2].Play();
                break;
            case 1: // Good
                Debug.Log("GOOD (정확도: 1)");
                if (particles.Count > 1 && particles[1] != null) particles[1].Play();
                break;
            case 0: // Bad
                Debug.Log("BAD... (정확도: 0)");
                //if (particles.Count > 2 && particles[0] != null) particles[0].Play();
                break;
        }
        if (gameManager != null) gameManager.Land(accuracy);

        // 착지 후 스케일 애니메이션이 진행 중이었다면 멈추고 원래 크기로 복구
        if (scaleAnimationCoroutine != null) StopCoroutine(scaleAnimationCoroutine);
        transform.localScale = originalScale;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Sea"))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log("실패! 강에 휩쓸려서 처음으로 돌아갑니다");
            transform.position = playerSpawnPos.position;

            // 물에 빠졌을 때도 스케일 원복
            if (scaleAnimationCoroutine != null) StopCoroutine(scaleAnimationCoroutine);
            transform.localScale = originalScale;
            isAirborne = false; // 바다에 빠졌으니 공중에 떠 있지 않음
        }

        // 블록에 착지했을 때 (아래로 떨어지는 중이고, 공중에 떠 있는 상태였을 때만 판정)
        if (collision.gameObject.CompareTag("Block") && isAirborne) 
        {
            int accuracy = CalculateLandingAccuracy(transform.position, collision.collider);
            Land(accuracy);
        }
    }

    int CalculateLandingAccuracy(Vector3 landingPosition, Collider blockCollider)
    {
        BoxCollider box = blockCollider as BoxCollider;
        if (box == null) return 0;

        Vector3 localLandingPos = blockCollider.transform.InverseTransformPoint(landingPosition);
        float distanceX = Mathf.Abs(localLandingPos.x);
        float distanceZ = Mathf.Abs(localLandingPos.z);
        float normalizedX = distanceX / (box.size.x / 2f + float.Epsilon);
        float normalizedZ = distanceZ / (box.size.z / 2f + float.Epsilon);
        float finalNormalizedDistance = Mathf.Max(normalizedX, normalizedZ);

        if (finalNormalizedDistance <= perfectThreshold) return 2;
        if (finalNormalizedDistance <= goodThreshold) return 1;
        return 0;
    }

    // --- 애니메이션 코루틴 ---

    private IEnumerator AnimateScale(Vector3 targetScale)
    {
        while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
            yield return null;
        }
        transform.localScale = targetScale;
    }

    private IEnumerator AnimateJumpStretch()
    {
        Vector3 stretchTarget = new Vector3(originalScale.x, originalScale.y * stretchAmount, originalScale.z);

        // 1. 빠르게 늘어납니다.
        while (Vector3.Distance(transform.localScale, stretchTarget) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, stretchTarget, Time.deltaTime * animationSpeed * 2f);
            yield return null;
        }
        transform.localScale = stretchTarget;

        // 2. 원래 크기로 돌아옵니다.
        while (Vector3.Distance(transform.localScale, originalScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * animationSpeed);
            yield return null;
        }
        transform.localScale = originalScale;
    }

    // --- 기타 함수 ---

    public float GetPerfectThreshold()
    {
        return perfectThreshold;
    }

    void AllParticleStop()
    {
        foreach (var particle in particles)
        {
            if (particle != null)
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}