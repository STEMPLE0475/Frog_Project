using UnityEngine;
using System.Collections;
using System;

// 모든 전문가 컴포넌트가 반드시 있도록 강제
[RequireComponent(typeof(PlayerState))]
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerEffects))]
[RequireComponent(typeof(PlayerCollisionHandler))]
public class PlayerController : MonoBehaviour
{
    private Vector3 playerSpawnPos;

    [Header("콤보")]
    private int combo = 0;

    private int curSessionLandCount = 0;
    public void PlusCurSessionLandCount() => curSessionLandCount++;
    public void ResetCurSessionLandCount() => curSessionLandCount = 0;
    public int GetCurSessionLandCount() => curSessionLandCount;

    // 전문가 컴포넌트 참조
    private PlayerState playerState;
    private PlayerInputHandler inputHandler;
    private PlayerMovement movement;
    private PlayerEffects effects;
    private PlayerCollisionHandler collisionHandler;

    [SerializeField] private ChargeGaugeController chargeGaugeController;


    // --- 이벤트 선언 ---

    public event Action<int, Vector3> OnCombo; // 콤보, 플레이어 좌표
    public event Action<float> OnJumpStart;
    public event Action<LandingAccuracy, int, Vector3, int> OnLanded; 
    public event Action OnSeaCollision;

    public void Initiate()
    {
        this.playerSpawnPos = transform.position;

        // 컴포넌트 참조 가져오기
        playerState = GetComponent<PlayerState>();
        inputHandler = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        effects = GetComponent<PlayerEffects>();
        collisionHandler = GetComponent<PlayerCollisionHandler>();

        inputHandler.Initiate();
        movement.Initiate();
        effects.Initiate();
        collisionHandler.Initiate();
        chargeGaugeController.Initiate();

        transform.position = playerSpawnPos;

        EventBind();
    }

    // 모든 컴포넌트의 이벤트를 구독
    void EventBind()
    {
        inputHandler.OnJumpRequested += (jumpForce) =>
        {
            movement.HideTrajectory();
            movement.ExecuteJump(jumpForce, () => {
                effects.StopChargingSfx();
                effects.PlayJumpSound();
                effects.PlayJumpAnimation();
                effects.StopChargeEffect();
                OnJumpStart?.Invoke(movement.jumpDuration);
            });
        };

        inputHandler.OnChargeStarted += () =>
        {
            effects.PlayChargeAnimation();
            effects.PlayChargingSfx();
            effects.PlayChargeEffect();
            chargeGaugeController.HandleChargeStarted();
        };

        inputHandler.OnChargeUpdated += (normalForce, currentForce) =>
        {
            movement.UpdateTrajectory(currentForce);
            chargeGaugeController.HandleChargeUpdated(normalForce);
        };

        inputHandler.OnChargeStopped += () =>
        {
            chargeGaugeController.HandleChargeStopped();
            movement.HideTrajectory();
        };

        collisionHandler.OnLanded += HandleLand;
        collisionHandler.OnSeaCollision += HandleSeaCollision;

        OnCombo += (combo, pos) => effects.UpdateTrail(combo); // 콤보 변경 시 이펙트 업데이트
    }

    // 충돌 핸들러가 "착지 결과" 보고
    private void HandleLand(LandingAccuracy accuracy)
    {
        playerState.SetAirborne(false); // 상태 변경
        effects.PlayLandSound();
        effects.PlayLandParticles(accuracy);
        effects.ResetScale();

        // 콤보 계산 (내부 이펙트 연동을 위해 콤보는 Player가 관리)
        switch (accuracy)
        {
            case LandingAccuracy.Perfect:
                PlusCombo();
                break;
            case LandingAccuracy.Good:
                ResetCombo(); // (기획에 따라 굿도 콤보 유지?)
                break;
            case LandingAccuracy.Bad:
                ResetCombo();
                break;
            case LandingAccuracy.Excep:
                return;
        }
        PlusCurSessionLandCount();
        OnLanded?.Invoke(accuracy, combo, GetPlayerPos(), GetCurSessionLandCount());
    }

    // 4. 충돌 핸들러가 "바다 충돌" 보고
    private void HandleSeaCollision(Vector3 collisionPos)
    {
        inputHandler.EnableInput(false);
        effects.PlaySeaCollisionSound();
        effects.SetTrail(false);
        effects.SetPlayerMesh(true); // 섀도우 모드
        collisionHandler.SetCollider(false);
        playerState.SetFloating(true); // 상태 변경

        // (⭐ 수정됨: UI, GameManager 로직 제거)
        // gameOverPanel.SetActive(true); (제거)
        // gameManager.SaveScore(); (제거)

        StartCoroutine(FloatingCoroutine(collisionPos));

        // (⭐ 신규: 외부에 "바다에 빠졌다"고 보고)
        OnSeaCollision?.Invoke();
    }

    // --- 외부 호출용 public 함수 ---
    public Vector3 GetPlayerPos(){
        return transform.position;
    }

    public void EnableInput(bool on)
    {
        inputHandler.EnableInput(on);
    }

    // (⭐ 수정됨: GameOver() -> RespawnPlayer()로 변경 및 통합)
    // GameStateManager가 게임 시작/재시작 시 이 함수를 호출
    public void RespawnPlayer()
    {
        StopAllCoroutines(); // 둥둥 코루틴 정지
        ResetCombo();

        movement.ResetVelocity();
        transform.position = playerSpawnPos;
        effects.ResetScale();
        playerState.ResetState(); // 상태 초기화
        collisionHandler.SetCollider(true);
        effects.SetPlayerMesh(false); // 원본 메시
        effects.SetTrail(true);
        inputHandler.EnableInput(true);
    }

    public void ApplyNewWind(Wind wind)
    {
        playerState.SetWind(wind);
        Debug.Log("바람 플레이어에게 Set. In Player Controller");
        Debug.Log(wind.power + " " +  wind.direction);
    }

    // --- 내부 로직 ---

    private IEnumerator FloatingCoroutine(Vector3 startPosition)
    {
        const float floatSpeed = 2f;
        const float floatHeight = 0.25f;

        while (playerState.IsFloating)
        {
            float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(
                startPosition.x,
                startPosition.y - 0.4f + yOffset,
                startPosition.z
            );
            yield return null;
        }
    }

    private void PlusCombo()
    {
        combo++;
        OnCombo?.Invoke(combo, transform.position); // 내부 이펙트 갱신용
    }
    private void ResetCombo()
    {
        combo = 0;
        OnCombo?.Invoke(combo, transform.position); // 내부 이펙트 갱신용
    }
}