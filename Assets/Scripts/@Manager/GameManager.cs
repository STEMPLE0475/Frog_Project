using UnityEngine;
using System.Collections.Generic;

// 모든 전문 매니저가 이 게임오브젝트에 같이 붙어있다고 가정
[RequireComponent(typeof(GameStateManager))]
[RequireComponent(typeof(ScoreManager))]
[RequireComponent(typeof(AudioManager))]
[RequireComponent(typeof(DatabaseManager))]
public class GameManager : MonoBehaviour
{
    [Header("Managers (Internal)")]
    private GameStateManager gameStateManager;
    private ScoreManager scoreManager;
    private AudioManager audioManager;
    private DatabaseManager databaseManager;

    [Header("Scene Dependencies (Assign in Editor)")]
    // (⭐ 수정됨: UI/Effect 매니저들을 세분화하여 참조)
    [SerializeField] private CanvasManager canvasManager;
    [SerializeField] private CanvasEffectManager canvasEffectManager;
    [SerializeField] private ComboTextEffect comboTextEffect;

    [SerializeField] private CinemachineCameraManager cinemachineCameraManager;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private HUDController hudController;

    [SerializeField] private BlockManager blockManager;

    [Header("Game Variables")]
    [SerializeField] private List<ButtonSound> buttonSounds;

    private void Awake()
    {
        // 1. 모든 전문 매니저 컴포넌트 가져오기
        gameStateManager = GetComponent<GameStateManager>();
        scoreManager = GetComponent<ScoreManager>();
        audioManager = GetComponent<AudioManager>();
        databaseManager = GetComponent<DatabaseManager>();

        // 2. 각 매니저 'Initiate' (의존성 주입)
        databaseManager.Initiate();
        audioManager.Initiate(buttonSounds);
        scoreManager.Initiate();

        // (씬에 있는 객체들 초기화)
        playerController.Initiate();
        cinemachineCameraManager.Initiate(playerController.transform);

        blockManager.Initiate();

        canvasManager.Initiate();
        canvasEffectManager.Initiate();
        comboTextEffect.Initiate(mainCamera);
        hudController.Initiate();

        // 게임 상태 매니저 초기화 (제어할 대상들을 넘겨줌)
        gameStateManager.Initiate(playerController, hudController, audioManager);

        // 3. === 이벤트 연결 ===

        // --- DB 로드 이벤트 ---
        databaseManager.OnUserDataLoaded += scoreManager.SetInitialMaxScore;
        databaseManager.OnUserDataLoaded += (userData) => {
            canvasManager.UpdateGameOverMaxScore(userData.HighScore);
        };

        // --- 스코어 변경 이벤트 ---
        scoreManager.OnScoreChanged += (score) => {
            canvasManager.UpdateInGameScore(score);
            canvasManager.UpdateGameOverCurrentScore(score);
        };
        scoreManager.OnMaxScoreChanged += (maxScore) => {
            canvasManager.UpdateGameOverMaxScore(maxScore);
        };

        // --- 게임 상태 이벤트 ---
        gameStateManager.OnGameStart += databaseManager.IncrementGameStartCount;
        gameStateManager.OnGameStart += scoreManager.ResetScore;
        gameStateManager.OnGameStart += playerController.RespawnPlayer;
        gameStateManager.OnGameStart += cinemachineCameraManager.ResetCamera;
        gameStateManager.OnGameStart += () => canvasManager.SetInGameScoreActive(true);
        gameStateManager.OnGameStart += () => databaseManager.StartNewSession("start_button");
        gameStateManager.OnGameStart += canvasManager.StartTutorialImageBlink;

        gameStateManager.OnGameOver += scoreManager.SaveScore;
        gameStateManager.OnGameOver += () => databaseManager.EndCurrentSession(scoreManager.GetMaxScore());
        gameStateManager.OnGameOver += hudController.GameOver;
        gameStateManager.OnGameOver += cinemachineCameraManager.DeathZoomStart;
        
        gameStateManager.OnGameOver += () => canvasManager.SetInGameScoreActive(false);

        // --- HUD 버튼 이벤트 ---
        hudController.OnStartGameClicked += HandleStartGameRequest;
        hudController.OnResumeGameClicked += gameStateManager.ResumeGame;
        hudController.OnQuitGameClicked += gameStateManager.QuitGame;

        hudController.OnRestartClicked += gameStateManager.RestartGame;
        hudController.OnRestartClicked += blockManager.ResetBlocks;
        hudController.OnRestartClicked += () => databaseManager.StartNewSession("restart_button");

        // --- 플레이어 이벤트 

        playerController.OnLanded += (acc, combo, playerPos) => scoreManager.HandleLanding(acc);
        playerController.OnLanded += (acc, combo, playerPos) => databaseManager.LogLanding(playerPos, acc.ToString());
        /*playerController.OnLanded += (acc, combo) =>
        {
            if (acc == LandingAccuracy.Perfect) canvasEffectManager.PlayIllustEffect(combo);
        };*/

        // (Player의 OnCombo 이벤트를 각 이펙트가 구독)
        playerController.OnCombo += (combo) =>
        {
            Vector3 pos = playerController.transform.position;
            comboTextEffect.Show(combo, pos);
        };
        playerController.OnCombo += audioManager.PlayComboSound; // (AudioManager에 이 기능이 추가되었다고 가정)
        playerController.OnCombo += (combo) => databaseManager.LogCombo(combo);

        // (Player의 OnSeaCollision 이벤트를 GameStateManager가 구독)
        playerController.OnSeaCollision += gameStateManager.TriggerGameOver;
        playerController.OnSeaCollision += () => databaseManager.LogDeath(playerController.GetPlayerPos());
        playerController.OnJumpStart += cinemachineCameraManager.OnZoomStart;
    }

    // === 함수 ===

    private void HandleStartGameRequest(string nickname)
    {
        // 1. DB에 인증 요청
        databaseManager.HandleUserAuthentication(nickname);

        // 2. 게임 상태 매니저에 시작 명령
        gameStateManager.StartGame();
    }
}