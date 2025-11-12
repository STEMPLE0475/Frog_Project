using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MapManager : MonoBehaviour
{
    BlockManager blockManager;
    PlayerController playerController;
    private float playerStartXPos;

    [SerializeField] private GameObject map_ParentObj;
    [SerializeField] private Image map_FillAmountImage;

    public void Initiate(BlockManager blockManager, PlayerController playerController)
    {
        playerStartXPos = playerController.GetPlayerSpawnPos().x;
        this.blockManager = blockManager;
        this.playerController = playerController;
        DisableMap();
    }

    public void EnableMap()
    {
        map_ParentObj.SetActive(true);
        UpdateMap();
    }
    public void DisableMap() => map_ParentObj.SetActive(false);

    public void UpdateMap()
    {
        map_FillAmountImage.fillAmount = (playerController.transform.position.x - playerStartXPos) / (blockManager.GetEndCheckPointXPos() - playerStartXPos);
        Debug.Log((playerController.transform.position.x - playerStartXPos) / (blockManager.GetEndCheckPointXPos() - playerStartXPos));
        Debug.Log(playerController.transform.position.x - playerStartXPos);
        Debug.Log(blockManager.GetEndCheckPointXPos() - playerStartXPos);
    }
}
