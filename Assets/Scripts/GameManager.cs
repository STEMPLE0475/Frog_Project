using UnityEngine;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Extensions;

public class GameManager : MonoBehaviour
{
    //private FirebaseFirestore db;
    // 게임의 전체적인 로직을 관리하는 스크립트
    // 해당하는 변수를 Editor에서 할당해줄 필요가 있음. (이것만은 반드시 해줘야함! 안하면 오류남)
    // 이 스크립트 오류만 없으면 다른 세부적인 부분 (PlayerContorller 등에서 설정 안해도 작동하도록 깔끔하게 해야함)

    [Header("Class")]
    [SerializeField] private CanvasManager canvasManager;
    [SerializeField] private CinemachineCameraScript cinemachineCameraScript;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private HUDController hudController;

    [Header("Variable")]
    [SerializeField] private Transform playerSpawnTransform;
    private int combo = 0;
    private bool isPaused = false; // 일시정지 여부를 저장하는 플래그

    [Header("Audio")]
    [SerializeField] private AudioSource bgmSource;  // 배경음악용 AudioSource
    [SerializeField] private AudioClip bgmClip;      // 배경음악 파일
    [SerializeField] private AudioSource uiSfxSource;        // 버튼 사운드용 AudioSource
    [SerializeField] private List<ButtonSound> buttonSounds; // 버튼 리스트
    [SerializeField, Range(0f, 2f)] private float uiVolume = 1.0f; // UI볼륨 배수(편의)

    private int score = 0;
    private int max_score = 0; // start 호출시 최고 점수를 DB에서 불러와야 함.

    public string player_name;

    // 본 프로젝트의 모든 Awake()나 Start()는 사용 금지.
    // 모든 프로세스의 시작을 분명히 하기 위해서. 모든 로직은 GameManager을 통해서 시작된다.
    private void Awake()
    {
        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;     // 계속 재생
            bgmSource.volume = 0.3f;   // 음량 (0~1)
            bgmSource.Play();          // 재생 시작
        }
        

        playerController.Initiate(this, playerSpawnTransform);
        cinemachineCameraScript.Initiate(playerController);
        canvasManager.Initiate(playerController);

        playerController.EnableInput(false);
        hudController?.Initiate(this);

        // 처음엔 게임을 멈춘 상태로 시작 
        Time.timeScale = 0f;
    }
    private void Start()
    {

        // === 버튼 클릭 사운드 연결 ===
        foreach (var btnSound in buttonSounds)
        {
            if (btnSound != null && btnSound.button != null && uiSfxSource != null)
            {
                btnSound.button.onClick.AddListener(() =>
                {
                    uiSfxSource.PlayOneShot(btnSound.clickSfx);
                    Debug.Log("btn");
                });
            }
        }
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        DocumentReference docRef = db.Collection("users").Document("alovelace");
        Dictionary<string, object> user = new Dictionary<string, object>
        {
            { "First", "Ada" },
            { "Last", "Lovelace" },
            { "Born", 1815 },
        };
        docRef.SetAsync(user).ContinueWithOnMainThread(task => {
            Debug.Log("Added data to the alovelace document in the users collection.");
        });
    }
    
    public void Land(int accuracy)
    {
        // 정확한 착지가 몇 콤보인지를 체크하여, Canvas 애니메이션을 실행
        // 수정 필요함. !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        switch (accuracy)
        {
            case 0:
                combo += 1;
                break;
            case 1:
                combo += 2;
                break;
            case 2:
                combo += 5;
                canvasManager.PlayIllustAnimation(0);
                break;
        }
    }
    public void StartGame()
    {
        // HUD 숨김 + HUD입력 차단 + 커서 숨김
        hudController.ShowHUD(false);
        hudController.EnableHUDInputOnly(false);
        //hudController.SetCursor(false);

        // 인게임 시작 
        Time.timeScale = 1f;
        isPaused = false;
        playerController.EnableInput(true);
        //canvasManager.PlayIllustAnimation(0); //필요시 연출

        canvasManager.InGameScoreTMP.gameObject.SetActive(true);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    

    private void Update()         // 매 프레임마다 ESC 입력을 감지
    {
        // ESC 키를 누르면 일시정지 토글
        if (Input.GetKeyDown(KeyCode.Escape)) 
            TogglePause();
    }
    
    private void TogglePause()
    {
        // 일시정지 상태를 전환하는 함수
        // true ↔ false 전환
        isPaused = !isPaused;

        // 일시정지 상태에 따라 실행할 함수 분기
        if (isPaused) PauseGame();
        else ResumeGame();
    }

    public void PauseGame()     // 게임을 일시정지하는 로직
    {
        Time.timeScale = 0f;                        // 물리 연산 및 Update 정지
        playerController.EnableInput(false);        // 플레이어 입력 비활성화
        hudController.ShowHUD(true, instant: true); // HUD 표시 (즉시 활성화)
        hudController.ShowMainMenu(false);          // 메인메뉴는 숨기기
        hudController.ShowPausePanel(true);         // 일시정지 패널 표시
        hudController.EnableHUDInputOnly(true);     // UI만 입력 받게 설정
        //hudController.SetCursor(true);              // 마우스 커서 표시
        if (bgmSource != null) bgmSource.Pause();   // BGM 일시정지
    }

    public void ResumeGame()
    {
        hudController.ShowPausePanel(false);      // 일시정지 패널 숨김
        hudController.ShowHUD(false);             // HUD 전체 숨김
        hudController.EnableHUDInputOnly(false);  // HUD 입력 비활성화
        //hudController.SetCursor(false);           // 커서 숨김
        playerController.EnableInput(true);       // 플레이어 입력 활성화
        Time.timeScale = 1f;                      // 게임 속도 정상화
        if (bgmSource != null) bgmSource.UnPause();// BGM 재개
    }

    public void GameOver()
    {
        

        canvasManager.InGameScoreTMP.gameObject.SetActive(true);
        playerController.GameOver();
    }

    // 스코어 관련
    public void ResetScore(){
        score = 0;
        canvasManager.InGameScoreTMP.text = "0점";
        canvasManager.gameOverScoreTMP.text = "0점";
        canvasManager.gameOverScoreBestTMP.text = max_score + "점";

    }
    public void PlusScore(int addScore)
    {
        this.score += addScore;
        canvasManager.InGameScoreTMP.text = score.ToString() + "점";
        canvasManager.gameOverScoreTMP.text = "현재 점수 : " + score.ToString() + "점";
    }

    public void SaveScore()
    {
        Debug.Log("현재 점수" + score);
        if (max_score < score) max_score = score;
        canvasManager.gameOverScoreBestTMP.text = "최고 점수 : " +  max_score + "점";
        Debug.Log("최고 점수" + max_score);
    }
}
