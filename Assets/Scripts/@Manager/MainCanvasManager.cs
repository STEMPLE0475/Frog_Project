using System;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class MainCanvasManager : MonoBehaviour
{
    [SerializeField] private LoginCanvas_Controller LoginCanvas;
    [SerializeField] private MainCanvas_Controller MainCanvas;
    [SerializeField] private SkinCanvas_Controller SkinCanvas;
    [SerializeField] private PlayCanvas_Controller PlayCanvas;

    [Header("Leader Board")]
    [SerializeField] private TextMeshProUGUI MainPanel_LeaderBoard;
    [SerializeField] private TextMeshProUGUI GameOverPanel_LeaerBoard;

    private PlayerController playerController;

    public Action<string> OnNickNameWrite;
    public Action OnRestartClicked;

    public void Initiate(PlayerController playerController)
    {
        EnableLoginPanel();
        SkinCanvas.Initiate(playerController);
    }

    public void EnableLoginPanel()
    {
        DisableAllPanel();
        LoginCanvas.gameObject.SetActive(true);
    }

    // gameManager -> 로그인 완료시 호출 : MainPanel 활성화
    public void EnableMainPanel() 
    {
        DisableAllPanel();
        MainCanvas.gameObject.SetActive(true);
        MainCanvas.TutorialBlinkStart();
    }

    // mainPanel의 Skin버튼 클릭시 호출 : SkinPanel 활성화
    public void EnableSkinPanel()
    {
        DisableAllPanel();
        SkinCanvas.EnablePanel();
    }

    public void EnablePlayPanel_PlayStart()
    {
        DisableAllPanel();
        PlayCanvas.EnablePlayPanel_PlayStart();
    }

    public void EnablePlayPanel_Pause()
    {
        DisableAllPanel();
        PlayCanvas.EnablePlayPanel_Pause();
    }

    public void EnablePlayPanel_GameOver(bool isClear)
    {
        DisableAllPanel();
        PlayCanvas.EnablePlayPanel_GameOver();
        PlayCanvas.EnableClearPanel(isClear);
    }

    public void DisableAllPanel()
    {
        LoginCanvas.gameObject.SetActive(false);
        MainCanvas.gameObject.SetActive(false);
        SkinCanvas.DisablePanel();
        PlayCanvas.DisablePanel();
    }

    public void OnClickRestartButton()
    {
        OnRestartClicked?.Invoke();
    }

    public void Update_LeaderBoardTMP(string txt)
    {
        MainPanel_LeaderBoard.text = txt;
        GameOverPanel_LeaerBoard.text = txt;
    }
}
