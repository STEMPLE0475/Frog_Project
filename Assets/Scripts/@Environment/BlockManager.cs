using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    //내부 변수
    private List<SinkBlock> sink_blocks;
    private List<CheckPointTrigger> checkPoint_triggers;

    //블록이 담긴 폴더
    [SerializeField] private GameObject sink_blocks_folder;
    [SerializeField] private GameObject checkPoint_blocks_folder;

    public Action<int> OnEnterCheckPoint;

    public void Initiate()
    {
        SinkBlock[] sinkBlocks = sink_blocks_folder.GetComponentsInChildren<SinkBlock>();
        sink_blocks = sinkBlocks.Where(block => block.gameObject != this.gameObject).ToList();

        CheckPointTrigger[] checkTriggers = checkPoint_blocks_folder.GetComponentsInChildren<CheckPointTrigger>();
        checkPoint_triggers = checkTriggers.Where(trigger => trigger.gameObject != this.gameObject).ToList();

        foreach (var trigger in checkPoint_triggers)
        {
            trigger.OnEnterCheckPoint += HandleEnterCheckPoint;
        }
    }

    public void HandleEnterCheckPoint(int checkPoint)
    {
        Debug.Log($"체크포인트 {checkPoint} 도달!");
        OnEnterCheckPoint?.Invoke(checkPoint);
    }

    public void ResetBlocks()
    {
        foreach (SinkBlock block in sink_blocks)
        {
            if (block != null)
            {
                block.ResetBlock();
            }
        }
    }

    public List<float> GetCheckPointXPosList() => checkPoint_triggers.Select(t => t.transform.position.x).ToList();
    public Vector3 GetCheckPointXPos(int index) => checkPoint_triggers[index -1].transform.position;
    public float GetEndCheckPointXPos() => checkPoint_triggers[checkPoint_triggers.Count - 1].transform.position.x;
}