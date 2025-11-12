using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System; // (⭐ Event 사용을 위해 추가)

public class HUDController : MonoBehaviour
{
    public event Action<string> OnStartGameClicked; // 닉네임을 함께 보냄
    public event Action OnResumeGameClicked;
    public event Action OnQuitGameClicked;
    public event Action OnRestartClicked; // (⭐ 추가됨: 게임오버 패널의 '다시하기')

    [SerializeField] private float gameOverDelay = 3f;

    [Header("Root & Panels")]
    [SerializeField] private CanvasGroup inGameUI;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject guidePanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject blocker;
    [SerializeField] private GameObject GameOverPanel;

    [Header("Buttons (Main Menu)")]
    [SerializeField] private Button gameStartB;
    [SerializeField] private Button gameExplainB;
    [SerializeField] private Button gameEndB;

    [Header("Buttons (Pause)")]
    [SerializeField] private Button pauseResumeB;
    [SerializeField] private Button pauseQuitB;

    [Header("Buttons (GameOverPanel)")]
    [SerializeField] private Button restartB;
    [SerializeField] private Button gameOverEndB;

    [Header("Fade Settings")]
    [SerializeField] private float fadeTime = 0.3f;

    [Header("Input Text")]
    [SerializeField] private TextMeshProUGUI input_field;

    [Header("Leader Board")]
    [SerializeField] private TextMeshProUGUI leaderBoard;
    [SerializeField] private TextMeshProUGUI leaderBoard2;

    // (⭐ 수정됨: GameManager 참조 제거)
    public void Initiate()
    {
        // 버튼 이벤트 초기화
        if (gameStartB) { gameStartB.onClick.RemoveAllListeners(); gameStartB.onClick.AddListener(OnClickStart); }
        if (gameExplainB) { gameExplainB.onClick.RemoveAllListeners(); gameExplainB.onClick.AddListener(OnClickExplain); }
        if (gameEndB) { gameEndB.onClick.RemoveAllListeners(); gameEndB.onClick.AddListener(OnClickEnd); }
        if (gameOverEndB) { gameOverEndB.onClick.RemoveAllListeners(); gameOverEndB.onClick.AddListener(OnClickEnd); }

        // 일시정지 버튼 바인딩
        if (pauseResumeB) { pauseResumeB.onClick.RemoveAllListeners(); pauseResumeB.onClick.AddListener(OnClickResume); }
        if (pauseQuitB) { pauseQuitB.onClick.RemoveAllListeners(); pauseQuitB.onClick.AddListener(OnClickEnd); }

        // 다시하기 버튼 바인딩
        if (restartB) { restartB.onClick.RemoveAllListeners(); restartB.onClick.AddListener(OnClickRestart); }

        // 처음엔 HUD 보이기
        ShowHUD(true, instant: true);
        ShowMainMenu(true);
        ShowGuidePanel(false);
        EnableHUDInputOnly(true);
        ShowGameOverPanel(false);
    }

    public void ShowHUD(bool on, bool instant = false)
    {
        // (기존 코드와 동일)
        if (!inGameUI)
        {
            gameObject.SetActive(on);
            return;
        }
        if (!on && !gameObject.activeInHierarchy)
            return;
        StopAllCoroutines();
        if (on)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            inGameUI.gameObject.SetActive(true);
        }
        if (instant)
        {
            inGameUI.alpha = on ? 1f : 0f;
            inGameUI.interactable = on;
            inGameUI.blocksRaycasts = on;
            if (!on) gameObject.SetActive(false);
        }
        else
        {
            StartCoroutine(FadeHUD(on));
        }
    }

    public void ShowMainMenu(bool on) => mainMenu?.SetActive(on);
    public void ShowGuidePanel(bool on) => guidePanel?.SetActive(on);
    public void ShowGameOverPanel(bool on) => GameOverPanel?.SetActive(on);

    private IEnumerator FadeHUD(bool on)
    {
        // (기존 코드와 동일)
        float from = inGameUI.alpha;
        float to = on ? 1f : 0f;
        float t = 0f;
        inGameUI.interactable = false;
        inGameUI.blocksRaycasts = false;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            inGameUI.alpha = Mathf.Lerp(from, to, t / fadeTime);
            yield return null;
        }
        inGameUI.alpha = to;
        inGameUI.interactable = on;
        inGameUI.blocksRaycasts = on;
        if (!on)
        {
            yield return null;
            gameObject.SetActive(false);
        }
    }

    public void ShowPausePanel(bool on)
    {
        pausePanel?.SetActive(on);
        blocker?.SetActive(on);
    }

    public void EnableHUDInputOnly(bool hudOnly)
    {
        if (inGameUI)
        {
            inGameUI.blocksRaycasts = hudOnly;
            inGameUI.interactable = hudOnly;
        }
    }

    public void GameOver()
    {
        ShowGameOverPanel(false);
        ShowHUD(true);
        ShowMainMenu(false);
        ShowPausePanel(false);
        StartCoroutine(GameOverCoroutine()); }

    IEnumerator GameOverCoroutine()
    {
        yield return new WaitForSecondsRealtime(gameOverDelay);
        ShowGameOverPanel(true);
        ShowHUD(true);
    }

    // ========== 버튼 클릭 이벤트 ==========

    private void OnClickStart()
    {
        // 1. 내부 UI 로직은 스스로 처리
        ShowHUD(false);
        EnableHUDInputOnly(false);

        // 2. 외부에 "시작" 보고 (닉네임과 함께)
        OnStartGameClicked?.Invoke(input_field.text);
    }

    // (이 로직은 순수 내부 로직이므로 변경 없음)
    private void OnClickExplain()
    {
        ShowMainMenu(false);
        ShowGuidePanel(true);
    }

    // (⭐ 수정됨: GameManager 호출 대신 이벤트 발생)
    public void OnClickEnd()
    {
        // 외부에 "종료" 보고
        OnQuitGameClicked?.Invoke();
    }

    // (⭐ 수정됨: GameManager 호출 대신 이벤트 발생)
    private void OnClickResume()
    {
        // 1. 내부 UI 로직은 스스로 처리
        ShowPausePanel(false);
        ShowHUD(false);
        EnableHUDInputOnly(false);

        // 2. 외부에 "재개" 보고
        OnResumeGameClicked?.Invoke();
    }

    // (⭐ 예시: 다시하기 버튼)
    private void OnClickRestart()
    {
        ShowGameOverPanel(false);
        OnRestartClicked?.Invoke();
    }

    public void Update_LeaderBoardTMP(string txt)
    {
        leaderBoard.text = txt;
        leaderBoard2.text = txt;
    }
}