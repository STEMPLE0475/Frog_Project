using System;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;

public class SkinCanvas_Controller : MonoBehaviour
{

    public List<GameObject> SkinBlocks;
    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private GameObject panel;

    [SerializeField] private TextMeshProUGUI skinNameTMP;
    [SerializeField] private TextMeshProUGUI skinContextTMP;

    //선택완료 / 선택 전 표시 이미지
    [SerializeField] private GameObject SelectedObj;
    [SerializeField] private GameObject WaitSelectObj;

    private Action OnChangeIndex;
    public Action<int> OnChangeSkin; // 스킨 적용 & networkManager에 반영

    private PlayerController playerController;
    private NetworkManager networkManager;

    public int current_show_index = 0;
    public int current_select_index = 0;
    
    public Dictionary<int, SkinData> skinDataDic = new Dictionary<int, SkinData>();
    public Dictionary<int, GameObject> skinBlockDic = new Dictionary<int, GameObject>();
    public List<SkinData> skinDataList = new List<SkinData>();
    public List<GameObject> skinBlockList = new List<GameObject>();

    public void Initiate(PlayerController playerController, NetworkManager networkManager)
    {
        this.playerController = playerController;
        this.networkManager = networkManager;

        OnChangeIndex += ShowSkinHandler;
        OnChangeSkin += (index) => SetSkin(index);

        skinDataDic.Clear();
        skinBlockDic.Clear();

        //임시로 리스트 -> 딕셔너리 저장
        for (int i = 0; i < skinDataList.Count; i++)
        {
            skinDataDic.Add(i, skinDataList[i]);
            skinBlockDic.Add(i, skinBlockList[i]);
        }

        //임시
        UpdateCurrentSelectedSkinByIndex(0);
    }


    private void OnDisable()
    {
        OnChangeIndex -= ShowSkinHandler;
        OnChangeSkin -= (index) => SetSkin(index);
    }

    //패널 관련

    //패널 활성화 : 카메라 전환
    public void EnablePanel()
    {
        ShowSkinHandler();
        cam.Priority = 10;
        panel.SetActive(true);
        Debug.Log("스킨 화면");
    }

    //패널 비활성화 : 카메라 기존 카메라로 복구
    public void DisablePanel()
    {
        cam.Priority = -2;
        panel.SetActive(false);
    }

    // - 좌우 버튼 -
    public void OnClickPrevButton()
    {
        int target_show_index = current_show_index - 1;
        current_show_index = Math.Clamp(target_show_index, 0 , skinBlockDic.Count - 1);
        OnChangeIndex?.Invoke();
    }
    public void OnClickNextButton()
    {
        int target_show_index = current_show_index + 1;
        current_show_index = Math.Clamp(target_show_index, 0, skinBlockDic.Count - 1);
        OnChangeIndex?.Invoke();
    }

    // - 카메라 조작 - 
    private void ShowSkinHandler() 
    {
        //Debug.Log("ShowSkinHandler");
        cam.Follow = skinBlockDic[current_show_index].transform;

        //지금 보는 스킨을 가지고 있거나, 이미 장착하고 있으면, Select할 수 없게 버튼이 변함
        if (networkManager.IsAcquiredSkin(current_show_index))
        {
            if(current_show_index == current_select_index)
            {
                SelectButtenSetActive(false);
            }
            else
            {
                SelectButtenSetActive(true);
            }
        }
        else
        {
            SelectButtenSetActive(false);
        }

        skinNameTMP.text = skinDataDic[current_show_index].Name;
        skinContextTMP.text = skinDataDic[current_show_index].Context;
    }

    // - 선택 버튼 -
    public void SelectButton()
    {
        OnChangeSkin?.Invoke(current_show_index);
    }

    // - 기타 -

    //Select 버튼이 활성화되어있는지 표현
    public void SelectButtenSetActive(bool state)
    {
        SelectedObj.SetActive(!state);
        WaitSelectObj.SetActive(state);
    }

    //GameManager에서 데이터 로드 후 현재 스킨 가져오기
    private void UpdateCurrentSelectedSkinByIndex(int index)
    {
        current_select_index = index;
        current_show_index = index;
    }

    //OnChangeSkin 이벤트와 연결
    public void SetSkin(int index)
    {
        //스킨을 보유하고 있지 않은 경우 (일반적으로 보유하고 있는 것이 정상)
        if(!networkManager.IsAcquiredSkin(index))
        {
            Debug.Log("에러 : 스킨 미보유");
            return;
        }
        playerController.SetSkin(skinDataDic[index].material);
        networkManager.SelectSkin(index);
        current_select_index = index;
        ShowSkinHandler();
    }
}
