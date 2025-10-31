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
    private WindManager windManager;

    [Header("Scene Dependencies (Assign in Editor)")]
    // (⭐ 수정됨: UI/Effect 매니저들을 세분화하여 참조)
    [SerializeField] private CanvasManager canvasManager;
    [SerializeField] private CanvasEffectManager canvasEffectManager;
    [SerializeField] private ComboTextEffect comboTextEffect;

    [SerializeField] private CameraController cameraController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GlobalVolumeController volumeController;

    [SerializeField] private PlayerController playerController;
    [SerializeField] private HUDController hudController;

    [SerializeField] private BlockManager blockManager;
    [SerializeField] private SeaManager seaManager;

    [Header("Game Variables")]
    [SerializeField] private List<ButtonSound> buttonSounds;

    private async void Awake()
    {
        // 1. 모든 전문 매니저 컴포넌트 가져오기
        gameStateManager = GetComponent<GameStateManager>();
        scoreManager = GetComponent<ScoreManager>();
        audioManager = GetComponent<AudioManager>();
        databaseManager = GetComponent<DatabaseManager>();
        windManager = GetComponent<WindManager>();

        // 2. 각 매니저 'Initiate' (의존성 주입)
        await databaseManager.Initiate();
        audioManager.Initiate(buttonSounds);
        scoreManager.Initiate();

        // (씬에 있는 객체들 초기화)
        playerController.Initiate();
        var collisionHandler = playerController.GetComponent<PlayerCollisionHandler>();
        collisionHandler?.Initiate();

        var inputHandler = playerController.GetComponent<PlayerInputHandler>();
        inputHandler?.Initiate();
        cameraController.Initiate(playerController.transform);
        volumeController.Initiate();

        blockManager.Initiate();
        windManager.Initiate();
        seaManager.Initiate();

        canvasManager.Initiate();
        canvasEffectManager.Initiate();
        comboTextEffect.Initiate(mainCamera);
        hudController.Initiate();

        gameStateManager.Initiate(playerController, hudController, audioManager);

        ShowTop10Ranking();

        // 3. === 이벤트 연결 ===

        // DB 로드 이벤트 (UserData userData)
        databaseManager.OnUserDataLoaded += (userData) => {
            scoreManager.SetInitialMaxScore(userData);
            canvasManager.Update_GameOverMaxScore(userData.HighScore);
            canvasManager.Update_Header_MaxScore(userData.HighScore);
            gameStateManager.StartGame();
        };

        // --- 스코어 변경 이벤트 ---
        scoreManager.OnScoreChanged += (score) => {
            canvasManager.Update_Header_CurrentScore(score);
            canvasManager.Update_GameOverCurrentScore(score);
        };
        scoreManager.OnMaxScoreChanged += (maxScore) => {
            canvasManager.Update_Header_MaxScore(maxScore);
            canvasManager.Update_GameOverMaxScore(maxScore);
        };

        // --- 게임 상태 이벤트 ---

        //  게임 시작
        gameStateManager.OnGameStart += () =>
        {
            GameReset();
            databaseManager.StartNewSession("start_button");
            canvasManager.StartTutorialImageBlink();
            
        };

        // 게임 재시작
        gameStateManager.OnGameResume += () =>
        {
            GameReset();
            databaseManager.StartNewSession("restart_button");
        };

        void GameReset()
        {
            playerController.ResetCurSessionLandCount();
            databaseManager.IncrementGameStartCount();
            blockManager.ResetBlocks();
            scoreManager.ResetScore();
            playerController.RespawnPlayer();
            windManager.ResetWindMangaer();
            cameraController.ResetCamera();
            canvasManager.SetActive_Header(true);
        }

        // 게임 종료
        gameStateManager.OnGameOver += () =>
        {
            scoreManager.SaveScore();
            databaseManager.EndCurrentSession(scoreManager.GetMaxScore());
            _ = databaseManager.SaveHighScoreIfBestAsync(scoreManager.GetMaxScore());
            ShowTop10Ranking(); // 리더보드 갱신
            hudController.GameOver();
            cameraController.DeathZoomStart();
            canvasManager.SetActive_Header(false);
        };

        // --- HUD 버튼 이벤트 ---
        hudController.OnStartGameClicked += HandleStartGameRequest;
        hudController.OnResumeGameClicked += gameStateManager.ResumeGame;
        hudController.OnQuitGameClicked += gameStateManager.QuitGame;
        hudController.OnRestartClicked += gameStateManager.RestartGame;

        // 플레이어 착지 이벤트 (LandingAccuracy acc, int combo, vector3 playerPos, int sessionLandCount)
        playerController.OnLanded += (acc, combo, playerPos, sessionLandCount) =>
        {
            //databaseManager.LogLanding(playerPos, acc.ToString());
            scoreManager.HandleLanding(acc);
            windManager.SetLandCount(sessionLandCount);
            cameraController.ShakeCamera(combo);
        };

        // 콤보 성공시 이벤트 (int combo, vector playerPos)
        playerController.OnCombo += (combo, playerPos) =>
        {
            //databaseManager.LogCombo(combo);
            comboTextEffect.Show(combo, playerPos);
            volumeController.ComboFadeInOut(combo);
            audioManager.PlayComboSound(combo);
            //canvasEffectManager.PlayIllustEffect(combo);
        };

        // 바다에 떨어지는 이벤트 (게임오버)
        playerController.OnSeaCollision += () =>
        {
            gameStateManager.TriggerGameOver();
            databaseManager.LogDeath(playerController.GetPlayerPos());
        };

        // 바람 변화시 이벤트 (Wind wind)
        windManager.OnWindChanged += (wind) =>
        {
            playerController.ApplyNewWind(wind);
            canvasManager.UpdateWind(wind);
            seaManager.SetSeaSpeed(wind);
            audioManager.PlayStartWindSound(wind);
        };
    }

    // === 함수 ===

    private void HandleStartGameRequest(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            Debug.LogWarning("닉네임이 비어있습니다.");
            return;
        }
        databaseManager.HandleUserAuthentication(nickname); 
    }


    // 랭킹 화면을 열 때 호출 (버튼 OnClick 등에 연결해도 됨)
    public void ShowTop10Ranking()
    {
        StartCoroutine(CoShowTop10Ranking());
    }

    private System.Collections.IEnumerator CoShowTop10Ranking()
    {
        var task = databaseManager.GetTop10RankingStringAsync(); // 문자열 한 방에 받기
        while (!task.IsCompleted) yield return null;

        if (task.Exception != null)
        {
            Debug.LogWarning($"랭킹 조회 실패: {task.Exception}");
            yield break;
        }

        string top10 = task.Result;

        hudController.Update_LeaderBoardTMP(top10);
    }
}