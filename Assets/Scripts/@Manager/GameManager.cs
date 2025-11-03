using UnityEngine;
using System.Collections.Generic;

// 모든 전문 매니저가 이 게임오브젝트에 같이 붙어있다고 가정
[RequireComponent(typeof(GameStateManager))]
[RequireComponent(typeof(DataManager))]
[RequireComponent(typeof(AudioManager))]
[RequireComponent(typeof(NetworkManager))]
public class GameManager : MonoBehaviour
{
    string version = "0.4.0"; // 빌드시 버전 명을 반드시 명시할 것!!

    [Header("Managers (Internal)")]
    private GameStateManager gameStateManager;
    private DataManager dataManager;
    private AudioManager audioManager;
    private NetworkManager networkManager;

    [Header("Scene Dependencies (Assign in Editor)")]
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
    [SerializeField] private WindManager windManager;
    [SerializeField] private WindEffectController windEffectController;

    [SerializeField] private WindowComboEffect windowComboEffect;
    [SerializeField] private FireworkController fireworkController;

    [Header("Game Variables")]
    [SerializeField] private List<ButtonSound> buttonSounds;

    private async void Awake()
    {
        // 1. 모든 전문 매니저 컴포넌트 가져오기
        gameStateManager = GetComponent<GameStateManager>();
        dataManager = GetComponent<DataManager>();
        audioManager = GetComponent<AudioManager>();
        networkManager = GetComponent<NetworkManager>();

        // 2. 각 매니저 'Initiate' (의존성 주입)
        await networkManager.Initiate();
        audioManager.Initiate(buttonSounds);
        dataManager.Initiate(version);

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
        windEffectController.Initiate(playerController.transform);
        seaManager.Initiate();

        canvasManager.Initiate();
        canvasEffectManager.Initiate();
        comboTextEffect.Initiate(mainCamera);
        hudController.Initiate();

        windowComboEffect.Initiate();
        fireworkController.Initiate();

        gameStateManager.Initiate(playerController, hudController, audioManager);

        ShowLeaderBoard();

        // 3. === 이벤트 연결 ===

        // DB 로드 이벤트 (UserData userData)
        networkManager.OnUserDataLoaded += (userData) => {
            dataManager.SetInitialUserData(userData);
            //dataManager.SetInitialMaxScore(userData);
            //canvasManager.Update_GameOverMaxScore(userData.HighScore);
            //canvasManager.Update_Header_MaxScore(userData.HighScore);
            gameStateManager.StartGame();
        };

        // --- 스코어 변경 이벤트 ---
        dataManager.OnScoreChanged += (score) => {
            canvasManager.Update_Header_CurrentScore(score);
            canvasManager.Update_GameOverCurrentScore(score);
        };
        dataManager.OnMaxScoreChanged += (maxScore) => {
            canvasManager.Update_Header_MaxScore(maxScore);
            canvasManager.Update_GameOverMaxScore(maxScore);
        };
        dataManager.OnComboChanged += (combo) => {
            // canvasManager.UpdateCombo(combo);
        };


        // --- 게임 상태 이벤트 ---

        //  게임 시작
        gameStateManager.OnGameStart += () =>
        {
            string sessionId = GameReset();
            networkManager.StartNewSession(sessionId, "start_button");
            canvasManager.StartTutorialImageBlink();
        };

        // 게임 재시작
        gameStateManager.OnGameResume += () =>
        {
            GameReset();
            networkManager.StartNewSession("restart_button");
        };

        string GameReset()
        {
            playerController.ResetCurSessionLandCount();
            blockManager.ResetBlocks();
            string newSessionId = dataManager.StartNewSession();
            playerController.RespawnPlayer();
            windManager.ResetWindMangaer();
            cameraController.ResetCamera();
            canvasManager.SetActive_Header(true);

            return newSessionId;
        }

        // 게임 종료
        gameStateManager.OnGameOver += () =>
        {
            SessionData finalSessionData = dataManager.EndSessionAndGetResults();
            if (finalSessionData != null) networkManager.EndCurrentSession(finalSessionData);

            int maxScore = dataManager.GetMaxScore();
            _ = networkManager.SaveHighScoreIfBestAsync(maxScore);

            ShowLeaderBoard();
            hudController.GameOver();
            cameraController.DeathZoomStart();
            canvasManager.SetActive_Header(false);
        };

        // --- HUD 버튼 이벤트 ---
        hudController.OnStartGameClicked += HandleStartGameRequest;
        hudController.OnResumeGameClicked += gameStateManager.ResumeGame;
        hudController.OnQuitGameClicked += gameStateManager.QuitGame;
        hudController.OnRestartClicked += gameStateManager.RestartGame;

        // 플레이어 착지 이벤트 (LandingAccuracy acc, int _currentCombo, vector3 playerPos, int sessionLandCount)
        playerController.OnLanded += (acc, combo, playerPos, sessionLandCount) =>
        {
            dataManager.HandleLanding(acc);
            windManager.SetLandCount(sessionLandCount);
            cameraController.ShakeCamera(combo);
        };

        // 플레이어 점프 시작 이벤트
        playerController.OnJumpStart += (jumpduration) => windManager.StartMakeNewWind();

        // 콤보 성공시 이벤트 (int _currentCombo, vector playerPos)
        playerController.OnCombo += (combo, playerPos) =>
        {
            comboTextEffect.Show(combo, playerPos);
            volumeController.ComboFadeInOut(combo);
            audioManager.PlayComboSound(combo);
            windowComboEffect.StartComboEffect(combo);
        };

        // 바다에 떨어지는 이벤트 (게임오버)
        playerController.OnSeaCollision += () =>
        {
            dataManager.LogDeath(playerController.GetPlayerPos());
            gameStateManager.TriggerGameOver();
        };

        // 바람 변화시 이벤트 (Wind wind)
        windManager.OnWindChanged += (wind) =>
        {
            playerController.ApplyNewWind(wind);
            canvasManager.UpdateWind(wind);
            seaManager.SetSeaSpeed(wind);
            audioManager.PlayStartWindSound(wind);
            windEffectController.UpdateWindEffect(wind);
        };

        // 체크포인트 진입 이벤트
        blockManager.OnEnterCheckPoint += (checkpoint) =>
        {
            dataManager.HandleCheckPoint(checkpoint);
            fireworkController.PlayEffect();
            if (checkpoint == 1) playerController.HideTrajectoryByCheckPoint();
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
        networkManager.HandleUserAuthentication(nickname); 
    }


    // 랭킹 화면을 열 때 호출 
    public void ShowLeaderBoard()
    {
        StartCoroutine(ShowLeaderBoardCoroutine());
    }
    private System.Collections.IEnumerator ShowLeaderBoardCoroutine()
    {
        var task = networkManager.GetTop10RankingStringAsync(); // 문자열 한 방에 받기
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