using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HUDController : MonoBehaviour
{
    [Header("Root & Panels")]
    [SerializeField] private CanvasGroup inGameUI;   // InGameUI (HUD 전체 루트)
    [SerializeField] private GameObject mainMenu;    // MainMenu 패널
    [SerializeField] private GameObject guidePanel;  // 추후 설명 패널 (지금은 비워둬도 됨)

    [Header("Buttons (Main Menu)")]
    [SerializeField] private Button gameStartB;      // GameStartB 버튼
    [SerializeField] private Button gameExplainB;    // GameExplainB 버튼
    [SerializeField] private Button gameEndB;        // GameEndB 버튼

    [Header("Fade Settings")]
    [SerializeField] private float fadeTime = 0.3f;

    private GameManager gm;

    // GameManager에서만 호출
    public void Initiate(GameManager owner)
    {
        gm = owner;

        // 버튼 이벤트 초기화
        if (gameStartB) { gameStartB.onClick.RemoveAllListeners(); gameStartB.onClick.AddListener(OnClickStart); }
        if (gameExplainB) { gameExplainB.onClick.RemoveAllListeners(); gameExplainB.onClick.AddListener(OnClickExplain); }
        if (gameEndB) { gameEndB.onClick.RemoveAllListeners(); gameEndB.onClick.AddListener(OnClickEnd); }

        // 처음엔 HUD 보이기
        ShowHUD(true, instant: true);
        ShowMainMenu(true);
        ShowGuidePanel(false);

        // HUD 입력만 받게 설정
        EnableHUDInputOnly(true);
        SetCursor(true);
    }

    // ========== HUD 표시 / 전환 ==========
    public void ShowHUD(bool on, bool instant = false)
    {
        if (!inGameUI)
        {
            gameObject.SetActive(on);
            return;
        }

        StopAllCoroutines();
        if (on)
        {
           inGameUI.gameObject.SetActive(true); 
        }

        if (instant)
        {
            inGameUI.alpha = on ? 1f : 0f;
            inGameUI.interactable = on;
            inGameUI.blocksRaycasts = on;
            gameObject.SetActive(on || inGameUI.alpha > 0f);
        }
        else
        {
            StartCoroutine(FadeHUD(on));
        }
    }

    public void ShowMainMenu(bool on) => mainMenu?.SetActive(on);
    public void ShowGuidePanel(bool on) => guidePanel?.SetActive(on);

    private IEnumerator FadeHUD(bool on)
    {
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

    // ========== 마우스 입력 및 커서 제어 ==========
    public void EnableHUDInputOnly(bool hudOnly)
    {
        if (inGameUI)
        {
            inGameUI.blocksRaycasts = hudOnly;
            inGameUI.interactable = hudOnly;
        }
    }

    public void SetCursor(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    // ========== 버튼 클릭 이벤트 ==========
    private void OnClickStart()
    {
        ShowHUD(false);
        EnableHUDInputOnly(false);
        SetCursor(false);
        gm?.StartGame();
    }

    private void OnClickExplain()
    {
        ShowMainMenu(false);
        ShowGuidePanel(true);
    }

    private void OnClickEnd()
    {
        gm?.QuitGame();
    }
}