using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HUDController : MonoBehaviour
{
    [Header("Root & Panels")]
    [SerializeField] private CanvasGroup inGameUI;   // InGameUI (HUD ��ü ��Ʈ)
    [SerializeField] private GameObject mainMenu;    // MainMenu �г�
    [SerializeField] private GameObject guidePanel;  // ���� ���� �г� (������ ����ֵ� ��)

    [Header("Buttons (Main Menu)")]
    [SerializeField] private Button gameStartB;      // GameStartB ��ư
    [SerializeField] private Button gameExplainB;    // GameExplainB ��ư
    [SerializeField] private Button gameEndB;        // GameEndB ��ư

    [Header("Fade Settings")]
    [SerializeField] private float fadeTime = 0.3f;

    private GameManager gm;

    // GameManager������ ȣ��
    public void Initiate(GameManager owner)
    {
        gm = owner;

        // ��ư �̺�Ʈ �ʱ�ȭ
        if (gameStartB) { gameStartB.onClick.RemoveAllListeners(); gameStartB.onClick.AddListener(OnClickStart); }
        if (gameExplainB) { gameExplainB.onClick.RemoveAllListeners(); gameExplainB.onClick.AddListener(OnClickExplain); }
        if (gameEndB) { gameEndB.onClick.RemoveAllListeners(); gameEndB.onClick.AddListener(OnClickEnd); }

        // ó���� HUD ���̱�
        ShowHUD(true, instant: true);
        ShowMainMenu(true);
        ShowGuidePanel(false);

        // HUD �Է¸� �ް� ����
        EnableHUDInputOnly(true);
        SetCursor(true);
    }

    // ========== HUD ǥ�� / ��ȯ ==========
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

    // ========== ���콺 �Է� �� Ŀ�� ���� ==========
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

    // ========== ��ư Ŭ�� �̺�Ʈ ==========
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