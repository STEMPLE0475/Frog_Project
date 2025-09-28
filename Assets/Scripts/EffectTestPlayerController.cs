using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // 새로운 Input System을 사용하기 위해 꼭 추가해야 합니다.

// 이 스크립트는 Rigidbody 컴포넌트가 있는 게임 오브젝트에 추가해야 합니다.
[RequireComponent(typeof(Rigidbody))]
public class EffectTestPlayerController : MonoBehaviour
{
    public List<ParticleSystem> particles;
    private GameManager gameManager;

    #region 변수 선언

    // --- 인스펙터에서 조절할 수 있는 값들 ---

    [Header("점프 파워 설정")]
    [Tooltip("점프의 최소 힘")]
    public float minJumpForce = 1f;

    [Tooltip("점프의 최대 힘")]
    public float maxJumpForce = 10f;

    [Tooltip("초당 충전되는 힘의 양")]
    public float chargeRate = 5f;

    [Header("점프 방향 설정")]
    [Tooltip("점프가 이루어질 방향 (기본값: X:1, Y:1, Z:1)")]
    public Vector3 jumpDirection = new Vector3(1f, 1f, 1f);

    [Header("시작 포지션")]
    private Transform playerSpawnPos;

    private float perfectThreshold = 0.3f;
    private float goodThreshold = 0.8f;

    public float GetPerfectThreshold()
    {
        return perfectThreshold;
    }

    // --- 내부적으로 사용되는 변수들 ---

    private Rigidbody rb;                 // 플레이어의 Rigidbody 컴포넌트
    private float currentJumpForce;       // 현재 충전된 점프 힘
    private bool isCharging = false;      // 현재 힘을 충전 중인지 여부
    private Keyboard keyboard;            // 현재 키보드 입력을 받기 위한 변수

    #endregion

    public void Initiate(GameManager gameManager, Transform playerSpawnPos)
    {
        this.gameManager = gameManager;
        //스폰 지점을 정합니다. GameManager에서 받아온다.
        this.playerSpawnPos = playerSpawnPos;

        // Rigidbody 컴포넌트를 가져옵니다.
        rb = GetComponent<Rigidbody>();

        // 현재 사용 중인 키보드 장치를 가져옵니다.
        keyboard = Keyboard.current;

        // 시작 시 현재 점프 힘을 최소값으로 초기화합니다.
        currentJumpForce = minJumpForce;
    }

    /// <summary>
    /// 매 프레임마다 호출되는 함수
    /// </summary>
    void Update()
    {
        // 키보드가 연결되어 있는지 확인합니다. (필수)
        if (keyboard == null)
        {
            Debug.LogWarning("키보드가 연결되지 않았습니다.");
            return;
        }

        // 스페이스바를 처음 눌렀을 때 (wasPressedThisFrame)
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            isCharging = true;
            currentJumpForce = minJumpForce;
            Debug.Log("점프 충전 시작!");
        }

        // 스페이스바를 누르고 있는 동안 (isPressed)
        if (keyboard.spaceKey.isPressed && isCharging)
        {
            currentJumpForce += chargeRate * Time.deltaTime;
            currentJumpForce = Mathf.Clamp(currentJumpForce, minJumpForce, maxJumpForce);
            Debug.Log($"충전 중... 파워: {currentJumpForce:F1}");
        }

        // 스페이스바에서 손을 뗐을 때 (wasReleasedThisFrame)
        if (keyboard.spaceKey.wasReleasedThisFrame && isCharging)
        {
            Jump();
            isCharging = false;
        }
    }

    /// <summary>
    /// 실제 점프를 실행하는 함수
    /// </summary>
    private void Jump()
    {
        Vector3 forceToApply = jumpDirection.normalized * currentJumpForce;
        rb.AddForce(forceToApply, ForceMode.Impulse);
        Debug.Log($"점프! 방향: {forceToApply}, 힘: {currentJumpForce:F1}");
    }

    //추가
    private void Land(int accuracy)
    {
        AllParticleStop();
        switch (accuracy)
        {
            case 0:
                //particles[0].Play();
                Debug.Log("BAD... (정확도: 0)");
                break;
            case 1:
                particles[1].Play();
                Debug.Log("GOOD (정확도: 1)");
                    break;
            case 2:
                particles[2].Play();
                Debug.Log("PERFECT! (정확도: 2)");
                break;
        }
        gameManager.Land(accuracy); 
    }

    //진행중인 Player의 모든 파티클을 강제 종료
    void AllParticleStop()
    {
        foreach (var particle in particles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Sea"))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log("실패! 강에 휩쓸려서 처음으로 돌아갑니다");
            transform.position = playerSpawnPos.position;
        }

        if (collision.gameObject.CompareTag("Block"))
        {
            Collider blockCollider = collision.collider;

            // collision.contacts[0].point 대신 '플레이어의 현재 위치(중심)'를 사용합니다.
            int accuracy = CalculateLandingAccuracy(transform.position, blockCollider);

            Land(accuracy); // Land 함수에서 모든 처리를 하므로 여기서 호출
            
        }
    }

    // 착지 지점과 블록 콜라이더를 기반으로 정확도를 계산합니다.
    // <returns>정확도 점수 (2: Perfect, 1: Good, 0: Bad)</returns>
    int CalculateLandingAccuracy(Vector3 landingPosition, Collider blockCollider)
    {
        BoxCollider box = blockCollider as BoxCollider;
        if (box == null) return 0; // BoxCollider가 아니면 판정 불가

        // 1. 플레이어의 월드 좌표를 블록의 '로컬 좌표'로 변환
        Vector3 localLandingPos = blockCollider.transform.InverseTransformPoint(landingPosition);

        // 2. 로컬 공간에서 각 축의 중심(0,0,0)으로부터의 거리 계산
        float distanceX = Mathf.Abs(localLandingPos.x);
        float distanceZ = Mathf.Abs(localLandingPos.z);

        // 3. BoxCollider의 로컬 'size'를 이용해 정규화 (0:중앙, 1:끝)
        float normalizedX = distanceX / (box.size.x / 2f + float.Epsilon);
        float normalizedZ = distanceZ / (box.size.z / 2f + float.Epsilon);

        // 4. 두 정규화 값 중 '더 큰 값'을 최종 거리로 사용
        float finalNormalizedDistance = Mathf.Max(normalizedX, normalizedZ);

        // 5. 최종 거리를 기준으로 점수 반환
        if (finalNormalizedDistance <= perfectThreshold) return 2;
        if (finalNormalizedDistance <= goodThreshold) return 1;
        return 0;
    }
}