using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(PlayerState))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour
{
    [Header("점프 파워 설정")]
    [SerializeField] private float minJumpForce = 4f;
    [SerializeField] private float maxJumpForce = 15f;
    [SerializeField] private float chargeRate = 9f;

    // PlayerController가 구독할 이벤트
    public event Action OnChargeStarted;
    public event Action<float> OnJumpRequested; // 점프 힘을 실어서 보냄

    private PlayerState playerState;
    private PlayerInput playerInput;
    private Keyboard keyboard;

    private float currentJumpForce;
    private bool isCharging = false;
    private bool inputEnabled = true;

    public void Initiate()
    {
        playerState = GetComponent<PlayerState>();
        playerInput = GetComponent<PlayerInput>();
        keyboard = Keyboard.current;

        if (playerState == null) Debug.LogError("[PlayerInputHandler] PlayerState가 없습니다.");
        if (playerInput == null) Debug.LogError("[PlayerInputHandler] PlayerInput이 없습니다.");
        if (keyboard == null) Debug.LogWarning("[PlayerInputHandler] Keyboard.current가 null (플랫폼/포커스 확인)");
        currentJumpForce = minJumpForce;
    }

    void Update()
    {
        // 아직 초기화 안됐으면 재시도하고, 그래도 없으면 리턴
        if (keyboard == null)
            keyboard = Keyboard.current;

        if (!inputEnabled || playerState == null || keyboard == null || playerState.IsFloating)
            return;

        HandleInput();
    }

    private void HandleInput()
    {
        if (keyboard == null || playerState == null) return;

        // 일부 플랫폼/디바이스에서 spaceKey가 null일 수도 있어 보호
        var space = keyboard.spaceKey;
        if (space == null) return;

        // 스페이스 처음 눌렀을 때
        if (space.wasPressedThisFrame && !playerState.IsAirborne)
        {
            isCharging = true;
            currentJumpForce = minJumpForce;
            OnChargeStarted?.Invoke();
        }

        // 누르는 중
        if (space.isPressed && isCharging)
        {
            currentJumpForce += chargeRate * Time.deltaTime;
            currentJumpForce = Mathf.Clamp(currentJumpForce, minJumpForce, maxJumpForce);
        }

        // 뗐을 때
        if (space.wasReleasedThisFrame && isCharging && !playerState.IsAirborne)
        {
            isCharging = false;
            OnJumpRequested?.Invoke(currentJumpForce);
        }
    }

    public void EnableInput(bool on)
    {
        inputEnabled = on;
        if (playerInput == null) return;

        if (on) playerInput.ActivateInput();
        else playerInput.DeactivateInput();
    }
}