using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EventPanelController : MonoBehaviour
{
    [SerializeField] private List<Sprite> Sprites;
    [SerializeField] private GameObject EventPanel;
    [SerializeField] private Image EventImage;

    NetworkManager networkManager;
    bool isEventPeriod;

    public void Initiate(bool isEventPeriod, NetworkManager networkManager)
    {
        this.isEventPeriod = isEventPeriod;
        this.networkManager = networkManager;
    }

    // networkManager에서 데이터가 로드되었을 때 실행
    public void ShowEventPanel()
    {
        if (!isEventPeriod) return;

        EventPanel.SetActive(true);

        switch (networkManager.GetEventLoginTime() - 1)
        {
            case 0:
                EventImage.sprite = Sprites[0];
                break;
            case 1:
                EventImage.sprite = Sprites[1];
                break;
            case 2:
                EventImage.sprite = Sprites[2];
                break;
            case 3:
                EventImage.sprite = Sprites[3];
                break;
            case 4:
                EventImage.sprite = Sprites[4];
                break;
            default:
                EventImage.sprite = Sprites[4];
                break;
        }
    }
}
