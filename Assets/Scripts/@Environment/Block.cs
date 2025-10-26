using System.Collections;
using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockType blockType = BlockType.Sink;
    public float perfectThreshold = 0.35f;
    public float goodThreshold = 0.8f;
    public bool isComboable = true;

    // 1. 블록의 '진짜' 시작 위치를 저장할 변수
    private Vector3 originalPosition;
    // 2. 실행 중인 코루틴을 저장할 변수
    private Coroutine runningSinkCoroutine;

    private void Awake()
    {
        // Start()나 Initiate()보다 먼저, 생성 시점의 위치를 저장
        originalPosition = transform.position;
    }

    private void Start()
    {
        InitiateBlock();
    }

    public void InitiateBlock()
    {
        if (blockType == BlockType.Sink)
        {
            GetComponent<BlockTargetZone>().EnableTargetZone(perfectThreshold);
        }
        isComboable = true;
    }

    public void CollisionPlayer()
    {
        if (blockType == BlockType.Sink)
        {
            // 이미 가라앉는 중이라면 중복 실행 방지
            if (runningSinkCoroutine != null)
            {
                StopCoroutine(runningSinkCoroutine);
            }
            // 2. 코루틴을 변수에 저장
            runningSinkCoroutine = StartCoroutine(StartSink());
        }
        isComboable = false;
    }

    // 3. BlockManager가 호출할 공개 리셋 함수
    public void ResetBlock()
    {
        // 1. 실행 중인 코루틴이 있다면 강제 중지
        if (runningSinkCoroutine != null)
        {
            StopCoroutine(runningSinkCoroutine);
            runningSinkCoroutine = null;
        }

        // 2. 위치를 '진짜' 시작 위치로 되돌림
        transform.position = originalPosition;
        isComboable = true;
    }

    IEnumerator StartSink()
    {
        Vector3 startPos = originalPosition; 
        Vector3 endPos1 = startPos + Vector3.down * 0.1f;
        Vector3 endPos2 = startPos + Vector3.down * 1f;

        // === 1. 첫 번째 하강 ===
        float duration1 = 0.25f;
        float firstSinkingTimer = 0;
        while (firstSinkingTimer < duration1)
        {
            float t = firstSinkingTimer / duration1;
            transform.position = Vector3.Lerp(startPos, endPos1, t);
            firstSinkingTimer += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos1;

        // === 3. 두 번째 하강 ===
        float duration2 = 5f;
        float secondSinkingTimer = 0;
        while (secondSinkingTimer < duration2)
        {
            float t = secondSinkingTimer / duration2;
            transform.position = Vector3.Lerp(endPos1, endPos2, t);
            secondSinkingTimer += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos2;

        // === 4. 싱크된 상태 대기 ===
        yield return new WaitForSeconds(1.0f);

        // === 5. 부상 ===
        float duration3 = 1.0f;
        float floatingTimer = 0;
        while (floatingTimer < duration3)
        {
            float t = floatingTimer / duration3;
            transform.position = Vector3.Lerp(endPos2, startPos, t);
            floatingTimer += Time.deltaTime;
            yield return null;
        }
        transform.position = startPos;

        // 2. 코루틴이 정상 종료되었으므로 참조를 비움
        runningSinkCoroutine = null;
    }
}

public enum BlockType
{
    Normal,
    Sink,
}
