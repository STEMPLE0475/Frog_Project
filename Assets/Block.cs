using System.Collections;
using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockType blockType = BlockType.Sink;
    public float perfectThreshold = 0.35f;
    public float goodThreshold = 0.8f;

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
    }

    // 플레이어와 충돌시 가라앉을지 체크
    public void CollisionPlayer()
    {
        if (blockType == BlockType.Sink)
        {
            StartCoroutine(StartSink());
        }
    }

    IEnumerator StartSink()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos1 = startPos + Vector3.down * 0.25f; // startPos 기준으로 설정
        Vector3 endPos2 = startPos + Vector3.down * 1f;  // startPos 기준으로 설정 (0.5f 더 내려감)

        // === 1. 첫 번째 하강 (0.25초 동안) ===
        float duration1 = 0.25f;
        float firstSinkingTimer = 0;
        while (firstSinkingTimer < duration1)
        {
            float t = firstSinkingTimer / duration1; // t = 0.0 ~ 1.0 (핵심 수정)
            transform.position = Vector3.Lerp(startPos, endPos1, t);
            firstSinkingTimer += Time.deltaTime;
            yield return null;
        }
        // Lerp는 목표지점에 정확히 도달하지 못할 수 있으므로, 루프 종료 후 최종 위치를 확실하게 설정 
        transform.position = endPos1;


        // === 3. 두 번째 하강 (3초 동안) ===
        float duration2 = 3f;
        float secondSinkingTimer = 0;
        while (secondSinkingTimer < duration2)
        {
            float t = secondSinkingTimer / duration2; // t = 0.0 ~ 1.0 (핵심 수정)
            transform.position = Vector3.Lerp(endPos1, endPos2, t);
            secondSinkingTimer += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos2;

        // === 4. 싱크된 상태 대기 (1.0초 동안) ===
        yield return new WaitForSeconds(1.0f);

        // === 5. 부상 (1.0초 동안) ===
        float duration3 = 1.0f;
        float floatingTimer = 0;
        while (floatingTimer < duration3)
        {
            float t = floatingTimer / duration3; // t = 0.0 ~ 1.0 (핵심 수정)
            transform.position = Vector3.Lerp(endPos2, startPos, t);
            floatingTimer += Time.deltaTime;
            yield return null;
        }
        transform.position = startPos;
    }
}

public enum BlockType
{
    Normal,
    Sink,
}
