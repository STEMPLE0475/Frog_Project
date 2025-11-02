using System;
using System.Collections;
using UnityEngine;

public class SinkBlock : MonoBehaviour
{
    public float perfectThreshold = 0.35f;
    public float goodThreshold = 0.8f;
    public bool isNotLanded = true;

    private Vector3 originalPosition;
    private Coroutine runningSinkCoroutine;

    private void Start()
    {
        InitiateBlock();
    }

    public void InitiateBlock()
    {
        originalPosition = transform.position;
        GetComponent<BlockTargetZone>().EnableTargetZone(perfectThreshold);
        isNotLanded = true;
    }

    public void CollisionPlayer()
    {
        if (runningSinkCoroutine != null) StopCoroutine(runningSinkCoroutine);
        runningSinkCoroutine = StartCoroutine(StartSink());
        isNotLanded = false;
    }

    public void ResetBlock()
    {
        if (runningSinkCoroutine != null)
        {
            StopCoroutine(runningSinkCoroutine);
            runningSinkCoroutine = null;
        }
        transform.position = originalPosition;
        isNotLanded = true;
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
        float duration2 = 8f;
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
