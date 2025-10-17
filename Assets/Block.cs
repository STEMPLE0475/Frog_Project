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
        GetComponent<BlockTargetZone>().EnableTargetZone(perfectThreshold);
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
        float waitTimer = 0;
        while (waitTimer < 1.0f)
        {
            waitTimer += Time.deltaTime;
            yield return null;
        }

        float SinkingTimer = 0;
        while (SinkingTimer < 2.0f)
        {
            transform.position = Vector3.Lerp(transform.position, transform.position + Vector3.up * (-0.003f), SinkingTimer);
            SinkingTimer += Time.deltaTime;
            yield return null;
        }

        float SinkedTimer = 0;
        while (SinkedTimer < 3.0f)
        {
            SinkedTimer += Time.deltaTime;
            yield return null;
        }

        float floatingTimer = 0;
        while (floatingTimer < 2.0f)
        {
            transform.position = Vector3.Lerp(transform.position, transform.position + Vector3.up * (0.003f), floatingTimer);
            floatingTimer += Time.deltaTime;
            yield return null;
        }
    }
}

public enum BlockType
{
    Normal,
    Sink,
}
