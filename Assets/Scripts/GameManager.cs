using Firebase.Extensions;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    //firebase
    public string player_name;
    private FirebaseFirestore db;
    private const string DEVICE_ID_KEY = "LocalDeviceID";
    private string currentDeviceID;
    private UserData loadedUserData;

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


        // 게임 시작 시 기기 ID를 먼저 확보합니다.
        db = FirebaseFirestore.DefaultInstance;
        currentDeviceID = GetOrCreateDeviceID();
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
        IncrementGameStartCount();
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
        IncrementGameStartCount();
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

        if (loadedUserData == null || score <= loadedUserData.HighScore) return;
        DocumentReference docRef = db.Collection("users").Document(currentDeviceID);
        // 💡 UpdateAsync에 필드명과 새 값을 바로 전달
        docRef.UpdateAsync("HighScore", score).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                loadedUserData.HighScore = score;
            }
        });
    }


    /// <summary>
    /// 기기에 저장된 고유 ID를 가져오거나 새로 생성합니다.
    /// </summary>
    public string GetOrCreateDeviceID()
    {
        // 1. PlayerPrefs에서 기존 ID를 로드
        string deviceID = PlayerPrefs.GetString(DEVICE_ID_KEY, "");

        if (string.IsNullOrEmpty(deviceID))
        {
            // 2. ID가 없으면 UUID(고유 식별자)를 새로 생성
            deviceID = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString(DEVICE_ID_KEY, deviceID);
            PlayerPrefs.Save();
            Debug.Log($"🔑 New Device ID Created: {deviceID}");
        }
        else
        {
            Debug.Log($"🔑 Existing Device ID Loaded: {deviceID}");
        }
        return deviceID;
    }

    /// <summary>
    /// 닉네임을 검색하거나, 닉네임이 없으면 기존 기기 ID로 데이터를 처리합니다.
    /// </summary>
    // 닉네임 검색 로직 (이 함수는 간결함을 위해 유지)
    public void HandleUserAuthentication(string inputNickname)
    {
        if (string.IsNullOrEmpty(inputNickname))
        {
            ProcessDataByDeviceID(currentDeviceID, inputNickname);
            return;
        }

        db.Collection("users")
            .WhereEqualTo("Nickname", inputNickname)
            .Limit(1)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(queryTask =>
            {
                QuerySnapshot snapshot = queryTask.Result;

                if (snapshot.Count > 0)
                {
                    // 닉네임 일치 시 ID 교체
                    string existingDocId = snapshot[0].Id;
                    PlayerPrefs.SetString(DEVICE_ID_KEY, existingDocId);
                    PlayerPrefs.Save();
                    currentDeviceID = existingDocId;

                    ProcessDataByDeviceID(existingDocId, inputNickname);
                }
                else
                {
                    ProcessDataByDeviceID(currentDeviceID, inputNickname);
                }
            });
    }
    // 문서 ID 기반 데이터 처리 로직 (간소화)
    private void ProcessDataByDeviceID(string docId, string inputNickname)
    {
        DocumentReference docRef = db.Collection("users").Document(docId);
        string finalNickname = string.IsNullOrEmpty(inputNickname) ?
                               "User_" + docId.Substring(0, 5).ToUpper() :
                               inputNickname;

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            DocumentSnapshot snapshot = task.Result;

            if (snapshot.Exists)
            {
                // 4-A. 문서가 존재함: 횟수 증가 (Dictionary 사용은 이 경우만 유지)
                docRef.UpdateAsync("GameOpenedCount", FieldValue.Increment(1));

                // 데이터 로드 후 내부 변수에 저장
                loadedUserData = snapshot.ConvertTo<UserData>();
            }
            else
            {
                // 4-B. 문서가 없음: 신규 데이터 생성 (UserData 객체 직접 사용)
                UserData initialData = new UserData
                {
                    Nickname = finalNickname,
                    FirstStartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    HighScore = 0,
                    GameOpenedCount = 1,
                    GameStartCount = 0
                };
                // 💡 SetAsync에 클래스 객체를 바로 전달
                docRef.SetAsync(initialData);
                loadedUserData = initialData;
            }
        });
    }

    /// <summary>
    /// 게임 시작 버튼을 누를 때 호출하여 횟수를 증가시킵니다.
    /// </summary>
    public void IncrementGameStartCount()
    {
        db.Collection("users")
          .Document(currentDeviceID) // 기기 ID 사용
          .UpdateAsync("GameStartCount", FieldValue.Increment(1));
    }
}


[FirestoreData]
public class UserData
{
    [FirestoreProperty]
    public string Nickname { get; set; }

    [FirestoreProperty]
    public string FirstStartTime { get; set; }

    [FirestoreProperty]
    public int HighScore { get; set; }

    [FirestoreProperty]
    public long GameOpenedCount { get; set; }

    [FirestoreProperty]
    public long GameStartCount { get; set; }
}