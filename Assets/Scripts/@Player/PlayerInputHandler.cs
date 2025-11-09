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
    public event Action<float, float> OnChargeUpdated;
    public event Action OnChargeStopped;
    public event Action<float> OnJumpRequested;

    private PlayerState playerState;
    private PlayerInput playerInput;
    private Touchscreen touchscreen;
    private Keyboard keyboard;

    private float currentJumpForce;
    private bool isCharging = false;
    private bool inputEnabled = true;

    public void Initiate()
    {
        playerState = GetComponent<PlayerState>();
        playerInput = GetComponent<PlayerInput>();
        keyboard = Keyboard.current;
        touchscreen = Touchscreen.current;

        if (playerState == null) Debug.LogError("[PlayerInputHandler] PlayerState가 없습니다.");
        if (playerInput == null) Debug.LogError("[PlayerInputHandler] PlayerInput이 없습니다.");
        if (keyboard == null) Debug.LogWarning("[PlayerInputHandler] Keyboard.current가 null (플랫폼/포커스 확인)");
        currentJumpForce = minJumpForce;
    }

    void Update()
    {
        if (!inputEnabled || playerState == null || playerState.IsFloating)
            return;

        if (keyboard == null)
            keyboard = Keyboard.current;
        if (touchscreen == null)
            touchscreen = Touchscreen.current;

        HandleInput();
    }

    private void HandleInput()
    {
        bool pressStarted = false;
        bool isPressed = false;
        bool pressReleased = false;

        // --- 키보드 입력 ---
        if (keyboard != null)
        {
            var space = keyboard.spaceKey;
            if (space != null)
            {
                pressStarted |= space.wasPressedThisFrame;
                isPressed |= space.isPressed;
                pressReleased |= space.wasReleasedThisFrame;
            }
        }

        // --- 모바일 터치 입력 ---
        if (touchscreen != null && touchscreen.primaryTouch != null)
        {
            var touch = touchscreen.primaryTouch;
            if (touch.press.wasPressedThisFrame)
                pressStarted = true;
            if (touch.press.isPressed)
                isPressed = true;
            if (touch.press.wasReleasedThisFrame)
                pressReleased = true;
        }

        // --- 입력 처리 ---
        if (pressStarted && !playerState.IsAirborne)
        {
            isCharging = true;
            currentJumpForce = minJumpForce;
            OnChargeStarted?.Invoke();
            OnChargeUpdated?.Invoke(0f, currentJumpForce);
        }

        if (isPressed && isCharging)
        {
            currentJumpForce += chargeRate * Time.deltaTime;
            currentJumpForce = Mathf.Clamp(currentJumpForce, minJumpForce, maxJumpForce);

            float normalizedValue = (currentJumpForce - minJumpForce) / (maxJumpForce - minJumpForce);
            OnChargeUpdated?.Invoke(normalizedValue, currentJumpForce);
        }

        if (pressReleased && isCharging && !playerState.IsAirborne)
        {
            isCharging = false;
            OnJumpRequested?.Invoke(currentJumpForce);
            OnChargeStopped?.Invoke();
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