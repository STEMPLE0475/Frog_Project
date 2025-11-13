using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MapManager : MonoBehaviour
{
    BlockManager blockManager;
    PlayerController playerController;
    private float playerStartXPos;

    [SerializeField] private GameObject map_Parent;
    [SerializeField] private Image map_FillAmountImage;
    
    // 체크 포인트 UI 관련 변수
    [SerializeField] private GameObject checkPointAnchorFolder; // 체크포인트 눈금을 넣어두는 폴더
    private List<GameObject> checkPointAnchors = new List<GameObject>(); // 체크포인트 눈금 리스트
    private List<float> checkPointXPos;

    public void Initiate(BlockManager blockManager, PlayerController playerController)
    {
        playerStartXPos = playerController.GetPlayerSpawnPos().x;
        this.blockManager = blockManager;
        this.playerController = playerController;
        DisableMap();

        // 체크 포인트 UI용 눈금 오브젝트를 담는 리스트 checkPointAnchors에 오브젝트 할당.
        foreach (Transform child in checkPointAnchorFolder.transform)
        {
            checkPointAnchors.Add(child.gameObject);
        }
        // 체크 포인트 UI의 눈금을 체크포인트의 위치에 맡게 이동시킨다.
        checkPointXPos = blockManager.GetCheckPointXPosList();
        RectTransform rectTransform;
        rectTransform = checkPointAnchors[0].GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(470 / 2 * (-1f), rectTransform.anchoredPosition.y);

        for (int i = 0; i < checkPointXPos.Count; i++)
        {
            float ratio = (checkPointXPos[i] - playerStartXPos) / (blockManager.GetEndCheckPointXPos() - playerStartXPos);
            rectTransform = checkPointAnchors[i+1].GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(470 * ratio - 470 / 2, rectTransform.anchoredPosition.y);
        }
    }



    public void EnableMap()
    {
        map_Parent.SetActive(true);
        UpdateMap();
    }
    public void DisableMap() => map_Parent.SetActive(false);

    public void UpdateMap()
    {
        map_FillAmountImage.fillAmount = (playerController.transform.position.x - playerStartXPos) / (blockManager.GetEndCheckPointXPos() - playerStartXPos);
    }
}
