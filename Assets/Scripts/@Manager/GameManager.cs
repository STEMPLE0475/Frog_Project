using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

// 모든 전문 매니저가 이 게임오브젝트에 같이 붙어있다고 가정
[RequireComponent(typeof(GameStateManager))]
[RequireComponent(typeof(DataManager))]
[RequireComponent(typeof(AudioManager))]
[RequireComponent(typeof(NetworkManager))]
public class GameManager : MonoBehaviour
{
    [Header("반드시 빌드 전 작성해야 하는 변수!!!")]
    string version = "0.6"; // 빌드시 버전 명을 반드시 명시할 것!!
    bool isDevelopMode = false; // 반드시 빌드시 개발자 모드 해제할 것!!
    bool isEventPeriod = true;


    bool isClear = false;

    [Header("Managers (Internal)")]
    private GameStateManager gameStateManager;
    private DataManager dataManager;
    private AudioManager audioManager;
    private NetworkManager networkManager;

    [Header("Scene Dependencies (Assign in Editor)")]
    [SerializeField] private PlayCanvas_Controller playCanvas_Controller;
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
    [SerializeField] private ScoreTextEffectController scoreTextEffectController;
    [SerializeField] private MapManager mapManager;

    [SerializeField] private NextCharacter nextCharacterManager;
    [SerializeField] private CheatManager cheatManager; // 개발자용 치트
    [SerializeField] private SkinCanvas_Controller skinCanvas_Contoller;
    [SerializeField] private MainCanvasManager mainCanvasManager;

    [SerializeField] private EventPanelController eventPanelController;
    
    [Header("Game Variables")]
    [SerializeField] private List<ButtonSound> buttonSounds;

    private async void Awake()
    {
        // 1. 모든 전문 매니저 컴포넌트 가져오기
        gameStateManager = GetComponent<GameStateManager>();
        dataManager = GetComponent<DataManager>();
        audioManager = GetComponent<AudioManager>();
        networkManager = GetComponent<NetworkManager>();
        NetworkManager.SetDeveloperMode(isDevelopMode);
        NetworkManager.SetEventPeriod(isEventPeriod);

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

        comboTextEffect.Initiate(mainCamera);
        //hudController.Initiate();

        windowComboEffect.Initiate();
        fireworkController.Initiate();
        scoreTextEffectController.Initiate(playerController.transform);

        //gameStateManager.Initiate(playerController, hudController, audioManager);
        gameStateManager.Initiate(playerController, audioManager);
        mapManager.Initiate(blockManager, playerController);
        nextCharacterManager.Initiate();
        cheatManager.Initiate(playerController, blockManager);
        if (!isDevelopMode) cheatManager.gameObject.SetActive(false);
        skinCanvas_Contoller.Initiate(playerController, networkManager);
        mainCanvasManager.Initiate(playerController);

        eventPanelController.Initiate(isEventPeriod, networkManager);

        ShowLeaderBoard();

        nextCharacterManager.SpawnFrog();
        // 3. === 이벤트 연결 ===

        // DB 로드 이벤트 (UserData userData)
        networkManager.OnUserDataLoaded += (userData) => {
            Debug.Log("유저 데이터 로드 완료");
            dataManager.SetInitialUserData(userData);
            mainCanvasManager.EnableMainPanel();
            eventPanelController.ShowEventPanel();
            networkManager.CheckEventThreeDay();
            networkManager.YetClearPlayerCheck(); // 이전 클리어 유저 보상
            skinCanvas_Contoller.SetSkin(networkManager.GetSelectedSkin());
        };

        // --- 스코어 변경 이벤트 ---
        dataManager.OnScoreChanged += (score) => {
            playCanvas_Controller.Update_Header_CurrentScore(score);
            playCanvas_Controller.Update_GameOverCurrentScore(score);

        };
        dataManager.OnMaxScoreChanged += (maxScore) => {
            playCanvas_Controller.Update_Header_MaxScore(maxScore);
            playCanvas_Controller.Update_GameOverMaxScore(maxScore);
        };
        dataManager.OnComboChanged += (combo) => {
            //playCanvas_Controller.UpdateCombo(combo);
        };
        dataManager.OnScorePlus += (addScore) =>
        {
            scoreTextEffectController.Show(addScore);
        };


        // --- 게임 상태 이벤트 ---

        //  게임 시작
        gameStateManager.OnGameStart += () =>
        {
            Debug.Log("Event : OnGameStart");
            string sessionId = GameReset();
            networkManager.StartNewSession(sessionId, "start_button");
        };

        


        // 게임 종료
        gameStateManager.OnGameOver += () =>
        {
            RecordDataToServer();
            ShowLeaderBoard();
            mainCanvasManager.EnablePlayPanel_GameOver(gameStateManager.isClear);
            mapManager.DisableMap();
            cameraController.DeathZoomStart();
            playerController.EnableInput(false);
        };

        // --- HUD 버튼 이벤트 ---
        mainCanvasManager.OnNickNameWrite += HandleLoginRequest;

        //hudController.OnResumeGameClicked += gameStateManager.ResumeGame;
        //hudController.OnQuitGameClicked += gameStateManager.QuitGame;
        //hudController.OnRestartClicked += gameStateManager.RestartGame;
        mainCanvasManager.OnRestartClicked += gameStateManager.RestartGame;

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

        // 바다에 빠진 이벤트 (게임오버)
        playerController.OnSeaCollision += () =>
        {
            dataManager.LogDeath(playerController.GetPlayerPos());
            _ = networkManager.SaveUserDataAsync();
            gameStateManager.TriggerGameOver();
        };

        // 바람 변화시 이벤트 (Wind wind)
        windManager.OnWindChanged += (wind) =>
        {
            playerController.ApplyNewWind(wind);
            playCanvas_Controller.UpdateWind(wind);
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
            if (checkpoint == 5) gameStateManager.EndGame();
        };

        gameStateManager.OnGameClear += () =>
        {
            nextCharacterManager.PlayEndAnimation();
            networkManager.LogClear();
            _ = networkManager.SaveUserDataAsync();
            isClear = true;
            networkManager.AcquireSkin(1);
        };

        nextCharacterManager.OnCharacterAnimationEnd += (Transform nextTarget) =>
        {
            cameraController.OnFollowStart(nextTarget);
        };
        nextCharacterManager.OnGameEnd += () =>
        {
            dataManager.LogDeath(playerController.GetPlayerPos());
            RecordDataToServer();
            _ = networkManager.SaveUserDataAsync();

            gameStateManager.TriggerGameOver();
        };
    }

    // === 함수 ===

    private void HandleLoginRequest(string nickname)
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


        mainCanvasManager.Update_LeaderBoardTMP(top10);
    }

    public void RecordDataToServer()
    {
        SessionData finalSessionData = dataManager.EndSessionAndGetResults();
        if (finalSessionData != null) networkManager.RecordSessionData(finalSessionData);

        int maxScore = dataManager.GetMaxScore();
        _ = networkManager.SaveHighScoreIfBestAsync(maxScore);

    }

    public string GameReset()
    {
        playerController.ResetCurSessionLandCount();
        blockManager.ResetBlocks();
        string newSessionId = dataManager.StartNewSession();
        playerController.RespawnPlayer();
        windManager.ResetWindMangaer();
        cameraController.ResetCamera();
        mainCanvasManager.EnablePlayPanel_PlayStart();
        mapManager.EnableMap();
        nextCharacterManager.SpawnFrog();
        nextCharacterManager.PlayStartAnimation();

        return newSessionId;
    }
    public void GameOverToHome()
    {
        gameStateManager.isGameStarted = false;
        blockManager.ResetBlocks();
        playerController.RespawnPlayer();
        windManager.ResetWindMangaer();
        cameraController.ResetCamera();
        nextCharacterManager.SpawnFrog();
        mainCanvasManager.EnableMainPanel();
    }

    public void GameOverToRestart()
    {
        gameStateManager.RestartGame();
    }

    private void Update()
    {
        if (gameStateManager.isGameStarted)
        {
            mapManager.UpdateMap();
        }
        
    }

}