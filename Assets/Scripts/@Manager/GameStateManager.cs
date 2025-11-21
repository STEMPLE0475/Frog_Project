using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class GameStateManager : MonoBehaviour
{

    // 상태 변경 시 다른 매니저들에게 알려주기 위한 이벤트
    public event Action OnGameStart;
    public event Action OnGamePause;
    public event Action OnGameResume;
    public event Action OnGameOver;
    public event Action OnGameClear;

    private bool isPaused = false;
    public bool isGameStarted = false; // 메인화면/인게임 구분
    public bool isClear = false;

    // 이 매니저가 제어해야 할 다른 컴포넌트들
    private PlayerController playerController;
    private HUDController hudController;
    private AudioManager audioManager;

    public void Initiate(PlayerController pc, AudioManager audio)
    {
        this.playerController = pc;
        this.audioManager = audio;

        // 처음엔 게임을 멈춘 상태(메인화면)로 시작
        //Time.timeScale = 0f;
        isPaused = true;
        isGameStarted = false;
        playerController.EnableInput(false);
    }

    private void Update()
    {
        // 게임이 시작된 후에만 ESC 키가 작동하도록
        if (isGameStarted && Input.GetKeyDown(KeyCode.Escape))
        {
            //TogglePause();
        }
    }

    public void OnClickStartButton() => StartGame();

/*    public void TogglePause()
    {
        isPaused = !isPaused;
        if (isPaused)
            PauseGame();
        else
            ResumeGame();
    }*/

    public void StartGame()
    {
        isPaused = false;
        isGameStarted = true;
        playerController.EnableInput(true);
        isClear = false;

        OnGameStart?.Invoke();

    }

    // 키보드 Space 또는 화면 터치 입력을 감지하는 함수 (Input System 사용)
    private bool IsStartInputPressed()
    {
        bool isInputPressed = false;

        // Input System 장치에 안전하게 접근합니다.
        var keyboard = Keyboard.current;
        var touchscreen = Touchscreen.current;

        // 1. 키보드 Space 키 입력 감지
        // keyboard가 null인지 확인하고 접근
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            isInputPressed = true;
        }

        // 2. 모바일 터치 입력 감지
        // touchscreen이 null인지 확인하고, primaryTouch의 press가 눌렸는지 확인
        if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            isInputPressed = true;
        }

        // 키보드와 터치 중 하나라도 눌렸으면 true 반환
        return isInputPressed;
    }

    public void PauseGame()
    {
        if (!isGameStarted) return; // 메인 화면에서는 Pause가 작동 안 함

        
        //Time.timeScale = 0f;
        isPaused = true;
        playerController.EnableInput(false);
        hudController.ShowHUD(true, instant: true);
        hudController.ShowMainMenu(false);
        hudController.ShowPausePanel(true);
        hudController.EnableHUDInputOnly(true);
        //audioManager.PauseBGM(true);

        OnGamePause?.Invoke();
    }

/*    public void ResumeGame()
    {
        hudController.ShowPausePanel(false);
        hudController.ShowHUD(false);
        hudController.EnableHUDInputOnly(false);
        playerController.EnableInput(true);
        Time.timeScale = 1f;
        isPaused = false;
        //audioManager.PauseBGM(false);

        OnGameResume?.Invoke();
    }*/

    // PlayerController가 바다에 빠졌을 때 GameManager를 통해 호출됨
    public void TriggerGameOver()
    {
        isGameStarted = false; // 게임 오버 상태
                               // (GameManager의 GameOver 로직 중 일부)
                               // playerController.GameOver(); // 이건 PlayerController가 스스로 처리 (ex: OnSeaCollision)

        OnGameOver?.Invoke(); // 게임 오버 이벤트 발생
    }

    // 마지막 체크포인트에 도달시 GameManager를 통해 호출
    public void EndGame()
    {
        playerController.EnableInput(false);
        OnGameClear?.Invoke();
        isClear = true;
    }

    // (PlayerController의 GameOver()가 호출하는) 리스폰 로직
    public void RestartGame()
    {
        isPaused = false;
        isGameStarted = true;
        isClear = false;
        playerController.EnableInput(true);
        OnGameStart?.Invoke(); // 게임 재시작
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}