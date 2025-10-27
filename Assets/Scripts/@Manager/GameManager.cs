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

    [SerializeField] private CinemachineCameraManager cinemachineCameraManager;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private HUDController hudController;

    [SerializeField] private BlockManager blockManager;
    [SerializeField] private SeaManager seaManager;

    [Header("Game Variables")]
    [SerializeField] private List<ButtonSound> buttonSounds;

    private void Awake()
    {
        // 1. 모든 전문 매니저 컴포넌트 가져오기
        gameStateManager = GetComponent<GameStateManager>();
        scoreManager = GetComponent<ScoreManager>();
        audioManager = GetComponent<AudioManager>();
        databaseManager = GetComponent<DatabaseManager>();
        windManager = GetComponent<WindManager>();

        // 2. 각 매니저 'Initiate' (의존성 주입)
        databaseManager.Initiate();
        audioManager.Initiate(buttonSounds);
        scoreManager.Initiate();

        // (씬에 있는 객체들 초기화)
        playerController.Initiate();
        cinemachineCameraManager.Initiate(playerController.transform);

        blockManager.Initiate();
        windManager.Initiate();
        seaManager.Initiate();

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

        //  게임 시작
        gameStateManager.OnGameStart += () =>
        {
            playerController.ResetCurSessionLandCount();
            databaseManager.IncrementGameStartCount();
            scoreManager.ResetScore();
            playerController.RespawnPlayer();
            windManager.ResetWindMangaer();
            cinemachineCameraManager.ResetCamera();
            canvasManager.SetInGameScoreActive(true);
            databaseManager.StartNewSession("start_button");
            canvasManager.StartTutorialImageBlink();

        };

        // 게임 종료
        gameStateManager.OnGameOver += () =>
        {
            scoreManager.SaveScore();
            databaseManager.EndCurrentSession(scoreManager.GetMaxScore());
            // 랭킹(최고기록) 반영
            _ = databaseManager.SaveHighScoreIfBestAsync(scoreManager.GetMaxScore());
            hudController.GameOver();
            cinemachineCameraManager.DeathZoomStart();
            canvasManager.SetInGameScoreActive(false);
        };


        // --- HUD 버튼 이벤트 ---
        hudController.OnStartGameClicked += HandleStartGameRequest;
        hudController.OnResumeGameClicked += gameStateManager.ResumeGame;
        hudController.OnQuitGameClicked += gameStateManager.QuitGame;

        hudController.OnRestartClicked += gameStateManager.RestartGame;
        hudController.OnRestartClicked += blockManager.ResetBlocks;
        hudController.OnRestartClicked += windManager.ResetWindMangaer;
        hudController.OnRestartClicked += playerController.ResetCurSessionLandCount;
        hudController.OnRestartClicked += () => databaseManager.StartNewSession("restart_button");

        // --- 플레이어 이벤트 

        playerController.OnLanded += (acc, combo, playerPos, sessionLandCount) =>
        {
            scoreManager.HandleLanding(acc);
            databaseManager.LogLanding(playerPos, acc.ToString());
            windManager.SetLandCount(sessionLandCount);
            cinemachineCameraManager.ShakeCamera(combo);
        };
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
        playerController.OnJumpStart += (jumpDuration) => windManager.StartMakeNewWind();

        // --- WindManager Event
        windManager.OnWindChanged += (wind) => playerController.ApplyNewWind(wind);
        windManager.OnWindChanged += (wind) => canvasManager.UpdateWind(wind);
        windManager.OnWindChanged += (wind) => seaManager.SetSeaSpeed(wind);
        windManager.OnWindChanged += (wind) => audioManager.PlayStartWindSound(wind);

    }

    // === 함수 ===

    private async void HandleStartGameRequest(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            Debug.LogWarning("닉네임이 비어있습니다.");
            return;
        }

        // 🔹 닉네임을 그대로 저장 (중복 허용)
        await databaseManager.SaveNicknameRawAsync(nickname);

        // 🔹 유저 데이터 로드 및 세션 준비
        databaseManager.HandleUserAuthentication(nickname);

        // 🔹 게임 시작
        gameStateManager.StartGame();
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
        // TODO: 프로젝트 UI에 맞게 표시 (예시)
        // canvasManager.UpdateTop10Text(top10);
        Debug.Log($"[TOP10]\n{top10}");
    }
}