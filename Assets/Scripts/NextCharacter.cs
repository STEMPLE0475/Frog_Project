using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NextCharacter : MonoBehaviour
{
    //내부 변수
    private GameObject spawnedCharacterObj;
    private GameObject spawnedEndCharacterObj;

    //참조 변수
    [SerializeField] private List<GameObject> skinPrefabs;
    [SerializeField] private Transform nextCharacterSpawnPos; // 시작 소환 장소
    [SerializeField] private Transform nextCharacterEndPos; // 엔딩 소환 장소

    [Header("점프 설정")]
    private Vector3 jumpDirection = new Vector3(1f, 0f, 1f);
    public float jumpDuration = 1.2f;
    public float jumpEndDuration = 2f;
    public float moveDuration = 3f;
    private float heightMultiplier = 1f;

    //시작 애니메이션 변수
    [SerializeField] private float jumpPower = 4f;
    [SerializeField] private float StartDelayTime = 1f;

    //끝 애니메이션 변수
    [SerializeField] private float jumpPowerEnd = 10f;
    private float turnaroundTime = 1f;
    private float turnaroundAndWaitDelay = 1f;
    public Action<Transform> OnCharacterAnimationEnd;
    public Action OnGameEnd;

    // ▼▼▼ 안전장치 1: 실행 중인 코루틴 참조 저장 변수 ▼▼▼
    private Coroutine startAnimationCoroutine;
    private Coroutine endAnimationCoroutine;

    public void Initiate()
    {

    }

    //GameManger -> ResetGame
    public void SpawnFrog()
    {
        SpawnCharacterSpawnPos(0);
        SpawnCharacterEndPos(0);
    }

    public void SpawnCharacterSpawnPos(int skinIndex)
    {
        // ▼▼▼ 안전장치 2: 새 캐릭터 생성 전, 기존 애니메이션 코루틴 즉시 중지 ▼▼▼
        if (startAnimationCoroutine != null)
        {
            StopCoroutine(startAnimationCoroutine);
            startAnimationCoroutine = null;
        }

        // ▼▼▼ 안전장치 3: 파괴 로직 수정 (Destroy를 먼저 호출해야 함) ▼▼▼
        if (spawnedCharacterObj != null)
        {
            Destroy(spawnedCharacterObj);
            // spawnedCharacterObj = null; // 어차피 바로 Instantiate로 덮어쓰므로 불필요
        }

        spawnedCharacterObj = Instantiate(skinPrefabs[0], nextCharacterSpawnPos);
    }
    public void SpawnCharacterEndPos(int skinIndex)
    {
        // ▼▼▼ 안전장치 2: 새 캐릭터 생성 전, 기존 애니메이션 코루틴 즉시 중지 ▼▼▼
        if (endAnimationCoroutine != null)
        {
            StopCoroutine(endAnimationCoroutine);
            endAnimationCoroutine = null;
        }

        // ▼▼▼ 안전장치 3: 파괴 로직 수정 (Destroy를 먼저 호출해야 함) ▼▼▼
        if (spawnedEndCharacterObj != null)
        {
            Destroy(spawnedEndCharacterObj);
        }
        spawnedEndCharacterObj = Instantiate(skinPrefabs[0], nextCharacterEndPos);
    }

    public void PlayStartAnimation()
    {
        // ▼▼▼ 안전장치 4: 중복 실행 방지 (기존 코루틴 중지) ▼▼▼
        if (startAnimationCoroutine != null)
        {
            StopCoroutine(startAnimationCoroutine);
        }

        // ▼▼▼ 안전장치 5: 애니메이션 대상이 없으면 실행하지 않음 ▼▼▼
        if (spawnedCharacterObj == null)
        {
            Debug.LogError("PlayStartAnimation: spawnedCharacterObj가 null입니다. Spawn이 먼저 되었는지 확인하세요.");
            return;
        }

        startAnimationCoroutine = StartCoroutine(ParabolicJump(jumpPower));
    }

    public void PlayEndAnimation()
    {
        // ▼▼▼ 안전장치 4: 중복 실행 방지 (기존 코루틴 중지) ▼▼▼
        if (endAnimationCoroutine != null)
        {
            StopCoroutine(endAnimationCoroutine);
        }

        // ▼▼▼ 안전장치 5: 애니메이션 대상이 없으면 실행하지 않음 ▼▼▼
        if (spawnedEndCharacterObj == null)
        {
            Debug.LogError("PlayEndAnimation: spawnedEndCharacterObj가 null입니다. Spawn이 먼저 되었는지 확인하세요.");
            return;
        }

        endAnimationCoroutine = StartCoroutine(EndAnimationCoroutine(jumpPowerEnd));
    }

    private IEnumerator EndAnimationCoroutine(float jumpForce)
    {
        yield return new WaitForSeconds(1f);

        if (turnaroundTime <= 0) turnaroundTime = 0.1f;

        float elapsedTime = 0f;
        Quaternion startQuaternion = spawnedEndCharacterObj.transform.rotation;

        Quaternion targetQuaternion = Quaternion.Euler(0, -225, 0);

        while (elapsedTime < turnaroundTime)
        {
            // ▼▼▼ 안전장치 7: 코루틴 실행 중 오브젝트가 파괴되면 즉시 중단 ▼▼▼
            if (spawnedEndCharacterObj == null)
            {
                endAnimationCoroutine = null;
                yield break; // 코루틴 즉시 탈출
            }

            elapsedTime += Time.deltaTime;
            float percent = elapsedTime / turnaroundTime;

            spawnedEndCharacterObj.transform.rotation = Quaternion.Slerp(startQuaternion, targetQuaternion, percent);

            yield return null;
        }

        // 오브젝트가 null이 아닐 때만 접근
        if (spawnedEndCharacterObj != null)
            spawnedEndCharacterObj.transform.rotation = targetQuaternion;


        OnCharacterAnimationEnd?.Invoke(spawnedEndCharacterObj.transform);
        yield return new WaitForSecondsRealtime(turnaroundAndWaitDelay);


        Vector3 startPos = spawnedEndCharacterObj.transform.position;
        Quaternion startRotation = spawnedEndCharacterObj.transform.rotation;
        Vector3 endPos = startPos + jumpDirection.normalized * jumpForce + Vector3.up * 0.1f;

        elapsedTime = 0f;
        float jumpHeight = jumpForce * heightMultiplier;

        while (elapsedTime < jumpEndDuration)
        {
            // ▼▼▼ 안전장치 7: 코루틴 실행 중 오브젝트가 파괴되면 즉시 중단 ▼▼▼
            if (spawnedEndCharacterObj == null)
            {
                endAnimationCoroutine = null;
                yield break;
            }

            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / jumpEndDuration); // 1.0을 넘지 않게

            float currentAngle = 360f * progress;
            spawnedEndCharacterObj.transform.rotation = startRotation * Quaternion.Euler(currentAngle, 0, 0);

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            spawnedEndCharacterObj.transform.position = currentPos;

            yield return null;
        }

        // 오브젝트가 null이 아닐 때만 접근
        if (spawnedEndCharacterObj != null)
        {
            spawnedEndCharacterObj.transform.position = endPos;
            spawnedEndCharacterObj.transform.rotation = startRotation;
        }

        OnGameEnd?.Invoke();

        // ▼▼▼ 안전장치 6: 코루틴 완료 후 참조 비우기 ▼▼▼
        endAnimationCoroutine = null;
    }


    private IEnumerator ParabolicJump(float jumpForce)
    {
        yield return new WaitForSeconds(StartDelayTime);

        // ▼▼▼ 안전장치 7: 대기 시간 후 오브젝트가 파괴되었는지 확인 ▼▼▼
        if (spawnedCharacterObj == null)
        {
            startAnimationCoroutine = null;
            yield break; // 코루틴 즉시 탈출
        }

        Vector3 startPos = spawnedCharacterObj.transform.position;
        Quaternion startRotation = spawnedCharacterObj.transform.rotation;
        Vector3 endPos = startPos + jumpDirection.normalized * jumpForce + Vector3.up * -2.5f;

        float elapsedTime = 0f;
        float jumpHeight = jumpForce * heightMultiplier;

        Vector3 accumulatedWindMovement = Vector3.zero;

        while (elapsedTime < jumpDuration)
        {
            // ▼▼▼ 안전장치 7: 코루틴 실행 중 오브젝트가 파괴되면 즉시 중단 ▼▼▼
            if (spawnedCharacterObj == null)
            {
                startAnimationCoroutine = null;
                yield break;
            }

            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / jumpDuration);

            float currentAngle = 360f * progress;
            spawnedCharacterObj.transform.rotation = startRotation * Quaternion.Euler(currentAngle, 0, 0);

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * jumpHeight;

            spawnedCharacterObj.transform.position = currentPos + accumulatedWindMovement;

            yield return null;
        }

        // 오브젝트가 null이 아닐 때만 접근
        if (spawnedCharacterObj != null)
            spawnedCharacterObj.transform.rotation = startRotation;

        yield return new WaitForSecondsRealtime(0.3f);

        elapsedTime = 0f;
        while (elapsedTime < moveDuration)
        {
            // ▼▼▼ 안전장치 7: 코루틴 실행 중 오브젝트가 파괴되면 즉시 중단 ▼▼▼
            if (spawnedCharacterObj == null)
            {
                startAnimationCoroutine = null;
                yield break;
            }

            elapsedTime += Time.deltaTime;
            spawnedCharacterObj.transform.position = spawnedCharacterObj.transform.position + new Vector3(1, 0, 1) * 20f * Time.deltaTime;
            yield return null;
        }

        // 오브젝트가 null이 아닐 때만 접근
        if (spawnedCharacterObj != null)
            Destroy(spawnedCharacterObj);

        spawnedCharacterObj = null; // 참조도 비워줍니다.

        // ▼▼▼ 안전장치 6: 코루틴 완료 후 참조 비우기 ▼▼▼
        startAnimationCoroutine = null;
    }
}