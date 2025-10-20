using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    private List<Block> sink_blocks;
    [SerializeField] private GameObject sink_blocks_folder;

    public void Initiate() {
        Block[] blocks = sink_blocks_folder.GetComponentsInChildren<Block>();
        sink_blocks = blocks.Where(block => block.gameObject != this.gameObject).ToList();
    }

    public void ResetBlocks()
    {
        foreach (Block block in sink_blocks)
        {
            if (block != null)
            {
                block.ResetBlock();
            }
        }
    }
}
