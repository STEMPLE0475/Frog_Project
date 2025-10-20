using UnityEngine;
using UnityEngine.InputSystem;
using System;

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

    public void initiate()
    {
        playerState = GetComponent<PlayerState>();
        playerInput = GetComponent<PlayerInput>();
        keyboard = Keyboard.current;
        currentJumpForce = minJumpForce;
    }

    void Update()
    {
        if (!inputEnabled || playerState.IsFloating || keyboard == null) return;

        HandleInput();
    }

    private void HandleInput()
    {
        // 스페이스바를 처음 눌렀을 때
        if (keyboard.spaceKey.wasPressedThisFrame && !playerState.IsAirborne)
        {
            isCharging = true;
            currentJumpForce = minJumpForce;
            OnChargeStarted?.Invoke(); // "충전 시작!" 이벤트 발생
        }

        // 스페이스바를 누르고 있는 동안
        if (keyboard.spaceKey.isPressed && isCharging)
        {
            currentJumpForce += chargeRate * Time.deltaTime;
            currentJumpForce = Mathf.Clamp(currentJumpForce, minJumpForce, maxJumpForce);
        }

        // 스페이스바에서 손을 뗐을 때
        if (keyboard.spaceKey.wasReleasedThisFrame && isCharging && !playerState.IsAirborne)
        {
            isCharging = false;
            OnJumpRequested?.Invoke(currentJumpForce); // "이 힘으로 점프!" 이벤트 발생
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