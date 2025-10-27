using Firebase.Extensions;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    public event Action<UserData> OnUserDataLoaded; // 데이터 로드 완료 이벤트

    private FirebaseFirestore db;
    private UserData loadedUserData;

    private string currentUserID_Normalized; // "mynick" (DB의 Key로 사용될 정규화 ID)
    private string currentUserNickname_Display; // "MyNick" (DB에 저장될 표시용 원본)

    // ===================== 신규: 세션 로깅 내부 상태 =====================
    private string _currentSessionId = null;       // 현재 세션 식별자
    private DateTime _sessionStartLocal;           // 로컬 기준 시작시각(디버그/전송용)
    private int _sessionJumpCount = 0;             // 세션 동안 점프 횟수
    private int _sessionBestCombo = 0;             // 세션 최고 콤보
    private Vector3? _lastDeathPosition = null;    // 마지막 사망 지점(있다면)

    // 테스트용 스위치(에디터에서 On/Off 가능)
/*    [Header("Logging Test")]
    [SerializeField] private bool ENABLE_DB_START_TEST = false;*/

    public void Initiate()
    {
        db = FirebaseFirestore.DefaultInstance;
    }

/*    private void Start()
    {
        // === 간단 동작 테스트 ===
        if (ENABLE_DB_START_TEST)
        {
            string sid = StartNewSession("manual_test");
            LogLanding(new Vector3(1.2f, 0f, 3.4f), "Perfect");
            LogLanding(new Vector3(2.5f, 0f, 4.1f), "Good");
            LogCombo(7);
            LogDeath(new Vector3(5.0f, -2.0f, 8.0f));
            EndCurrentSession(finalScore: 1234);
        }
    }*/

    // [When] 1. Initiate() 시 (닉네임 없이)
    //        2. 유저가 닉네임 입력 UI 등에서 닉네임을 확정했을 때
    // [What] 닉네임이 주어지면 닉네임 저장을 비동기로 시도하고,
    //        항상 'currentDeviceID'를 기준으로 유저 데이터를 로드/생성함
    public void HandleUserAuthentication(string inputNickname)
    {
        if (string.IsNullOrWhiteSpace(inputNickname))
        {
            Debug.LogError("로그인 실패: 닉네임이 비어있습니다.");
            return; // 닉네임 없이는 진행 불가
        }

        // 1. 표시용 닉네임과 DB Key용 ID를 저장
        this.currentUserNickname_Display = inputNickname.Trim(); // "MyNick"
        this.currentUserID_Normalized = NormalizeNicknameLower(inputNickname); // "mynick"

        if (string.IsNullOrEmpty(this.currentUserID_Normalized))
        {
            Debug.LogError("로그인 실패: 닉네임이 유효하지 않습니다.");
            return;
        }

        // 2. 닉네임 기반으로 데이터 처리 시작 (이전 DeviceID 대신)
        ProcessDataByUserID(this.currentUserID_Normalized, this.currentUserNickname_Display);
    }

    // [When] HandleUserAuthentication()에서 호출됨
    // [What] 'users' 컬렉션에서 'docId'(즉, currentDeviceID) 문서를 가져옴.
    //        - 문서가 있으면 : 데이터를 읽고(ConvertTo), GameOpenedCount를 1 증가시킴.
    //        - 문서가 없으면(신규 유저) : 기본 UserData 객체를 생성하여 DB에 저장(SetAsync)함.
    //        - 마지막으로, DAU 집계를 위해 MarkDailyActive()를 호출하고 OnUserDataLoaded 이벤트를 발생시킴.
    private void ProcessDataByUserID(string normalizedUserID, string displayNickname)
    {
        // [수정] Document ID(Key)로 정규화된 닉네임(소문자)을 사용
        DocumentReference docRef = db.Collection("users").Document(normalizedUserID);

        // [삭제] finalNickname 로직 (필요 없어짐)

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            DocumentSnapshot snapshot = task.Result;
            if (snapshot.Exists) // 1. 기존 유저
            {
                // [수정] 닉네임 대소문자를 바꿨을 수도 있으니, 표시용 닉네임은 매번 갱신
                docRef.UpdateAsync(new Dictionary<string, object> {
                { "GameOpenedCount", FieldValue.Increment(1) },
                { "Nickname", displayNickname } // "MyNick" (표시용 닉네임 갱신)
            });
                loadedUserData = snapshot.ConvertTo<UserData>();
            }
            else // 2. 신규 유저
            {
                UserData initialData = new UserData
                {
                    Nickname = displayNickname, // "MyNick" (표시용 닉네임 저장)
                    FirstStartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    HighScore = 0,
                    GameOpenedCount = 1,
                    GameStartCount = 0
                };
                // [수정] "mynick"을 문서 ID로 하여 새 문서 생성
                docRef.SetAsync(initialData);
                loadedUserData = initialData;
            }

            MarkDailyActive("app_open");
            OnUserDataLoaded?.Invoke(loadedUserData);
        });
    }

    // [When] 실제 '게임 시작' 버튼을 눌렀을 때 (GameManager 등에서 호출)
    // [What] 'users' 문서의 'GameStartCount' 필드를 1 증가시킴 (앱 실행(Open)과는 별개)
    public void IncrementGameStartCount()
    {
        if (string.IsNullOrEmpty(currentUserID_Normalized)) return;
        db.Collection("users")
          .Document(currentUserID_Normalized) // [수정]
          .UpdateAsync("GameStartCount", FieldValue.Increment(1));
    }


    // [When] 1. ProcessDataByDeviceID (앱 실행 시)
    //        2. StartNewSession (게임 시작 시)
    // [What] DAU(일일 활성 유저) 집계를 위해 'users/{userId}/daily/{오늘날짜KST}' 문서를 생성/업데이트함.
    //        'SetOptions.MergeAll'을 사용하여, 'app_open'으로 이미 문서가 있어도 'game_start' 정보 등을 덮어쓰지 않고 병합함.
    private void MarkDailyActive(string reason = "app_open")
    {
        if (string.IsNullOrEmpty(currentUserID_Normalized)) return;

        string dateId = GetKSTDateId();
        var dailyRef = db.Collection("users").Document(currentUserID_Normalized)
                           .Collection("daily").Document(dateId);

        dailyRef.SetAsync(new
        {
            active = true,
            reason = reason,
            firstSeenAt = FieldValue.ServerTimestamp
        }, SetOptions.MergeAll);
    }

    // [When] MarkDailyActive() 내부에서 호출됨
    // [What] 현재 시각을 KST(한국 표준시) 기준으로 "yyyyMMdd" 형태의 문자열로 반환
    private string GetKSTDateId() // "YYYYMMDD"
    {
        var nowKst = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time"));
        return nowKst.ToString("yyyyMMdd");
    }

    // [When] '게임 시작' 버튼을 눌러 실제 게임 플레이가 시작될 때
    // [What] '한 판'의 플레이 로그를 기록하기 위한 새 세션을 시작함.
    //        1. 내부 세션 변수들(점프 카운트, 콤보 등)을 리셋함.
    //        2. 새 세션 ID(GUID)를 발급함.
    //        3. 'users/{userId}/sessions/{newSessionId}' 문서를 생성하고 'startedAt' 등 초기 데이터를 기록함.
    //        4. DAU 집계를 위해 MarkDailyActive("game_start")를 호출함.
    public string StartNewSession(string startReason = "start_button")
    {
        // [수정] currentDeviceID -> currentUserID_Normalized
        if (string.IsNullOrEmpty(currentUserID_Normalized))
        {
            Debug.LogWarning("StartNewSession: currentUserID_Normalized is null or empty. Call HandleUserAuthentication first.");
            return null;
        }

        _currentSessionId = Guid.NewGuid().ToString("N");
        _sessionStartLocal = DateTime.UtcNow;
        _sessionJumpCount = 0;
        _sessionBestCombo = 0;
        _lastDeathPosition = null;

        var sessionRef = db.Collection("users")
                           // [수정] currentDeviceID -> currentUserID_Normalized
                           .Document(currentUserID_Normalized)
                           .Collection("sessions")
                           .Document(_currentSessionId);

        var payload = new Dictionary<string, object>
        {
            { "startedAt", FieldValue.ServerTimestamp },
            { "startReason", startReason },
            { "jumpCount", 0 },
            { "bestCombo", 0 },
            { "finalScore", 0 },
            { "endedAt", null },
            { "lastDeathPos", null }
        };

        sessionRef.SetAsync(payload);
        MarkDailyActive("game_start");
        return _currentSessionId;
    }

    // [When] 게임 오버(사망) 등으로 '한 판'이 종료되었을 때
    // [What] 진행 중이던 세션 문서를 'Update'하여 최종 결과를 기록함.
    public void EndCurrentSession(int finalScore)
    {
        if (!EnsureSession("EndCurrentSession")) return;

        var sessionRef = db.Collection("users")
                           // [수정] currentDeviceID -> currentUserID_Normalized
                           .Document(currentUserID_Normalized)
                           .Collection("sessions")
                           .Document(_currentSessionId);

        var update = new Dictionary<string, object>
        {
            { "endedAt", FieldValue.ServerTimestamp },
            { "finalScore", finalScore },
            { "jumpCount", _sessionJumpCount },
            { "bestCombo", _sessionBestCombo },
            { "lastDeathPos", _lastDeathPosition.HasValue ? Vec(_lastDeathPosition.Value) : null }
        };

        sessionRef.UpdateAsync(update);
    }

    // [When] 플레이어가 발판에 '착지'할 때마다 호출
    // [What] 1. 내부적으로 _sessionJumpCount를 1 증가시킴.
    //        2. '.../sessions/{sessionId}/events' 서브컬렉션에 'landing' 타입의 새 이벤트를 기록함.
    public void LogLanding(Vector3 landingPosition, string accuracy)
    {
        if (!EnsureSession("LogLanding")) return;

        _sessionJumpCount++;

        var evRef = db.Collection("users")
                      // [수정] currentDeviceID -> currentUserID_Normalized
                      .Document(currentUserID_Normalized)
                      .Collection("sessions").Document(_currentSessionId)
                      .Collection("events").Document();

        var payload = new Dictionary<string, object>
        {
            { "type", "landing" },
            { "ts", FieldValue.ServerTimestamp },
            { "pos", Vec(landingPosition) },
            { "accuracy", accuracy }
        };

        evRef.SetAsync(payload);
    }

    // [When] 플레이어가 '사망'했을 때마다 호출
    // [What] 1. 내부적으로 _lastDeathPosition을 기록함 (EndCurrentSession에서 사용)
    //        2. '.../sessions/{sessionId}/events' 서브컬렉션에 'death' 타입의 새 이벤트를 기록함.
    public void LogDeath(Vector3 deathPosition)
    {
        if (!EnsureSession("LogDeath")) return;

        _lastDeathPosition = deathPosition;

        var evRef = db.Collection("users")
                      // [수정] currentDeviceID -> currentUserID_Normalized
                      .Document(currentUserID_Normalized)
                      .Collection("sessions").Document(_currentSessionId)
                      .Collection("events").Document();

        var payload = new Dictionary<string, object>
        {
            { "type", "death" },
            { "ts", FieldValue.ServerTimestamp },
            { "pos", Vec(deathPosition) }
        };

        evRef.SetAsync(payload);
    }

    // [When] 플레이어가 '콤보'를 달성했을 때 (아마도 'Perfect' 착지 시)
    // [What] 1. 내부적으로 _sessionBestCombo를 갱신함.
    //        2. '.../sessions/{sessionId}/events' 서브컬렉션에 'combo' 타입의 새 이벤트를 기록함.
    public void LogCombo(int combo)
    {
        if (!EnsureSession("LogCombo")) return;

        if (combo > _sessionBestCombo) _sessionBestCombo = combo;

        var evRef = db.Collection("users")
                      // [수정] currentDeviceID -> currentUserID_Normalized
                      .Document(currentUserID_Normalized)
                      .Collection("sessions").Document(_currentSessionId)
                      .Collection("events").Document();

        var payload = new Dictionary<string, object>
        {
            { "type", "combo" },
            { "ts", FieldValue.ServerTimestamp },
            { "value", combo }
        };

        evRef.SetAsync(payload);
    }

    // [When] 게임 오버 시, 'EndCurrentSession'과 별개로 (또는 직후에) 최고 점수 갱신을 위해 호출
    // [What] 현재 게임 점수(candidateScore)가 로컬 캐시의 최고 점수(currentBest)보다 높을 때만 DB에 저장.
    //        'users'와 'leaderboard' 두 곳에 동시에 업데이트함.
    public async Task SaveHighScoreIfBestAsync(int candidateScore)
    {
        // [수정] currentDeviceID -> currentUserID_Normalized
        if (string.IsNullOrEmpty(currentUserID_Normalized)) return;

        int currentBest = loadedUserData?.HighScore ?? 0;
        if (candidateScore <= currentBest) return;

        if (loadedUserData == null) loadedUserData = new UserData();
        loadedUserData.HighScore = candidateScore;

        var userRef = CurrentUserRef(); // 헬퍼 함수 (내부가 수정됨)
        // [수정] currentDeviceID -> currentUserID_Normalized
        var lbRef = db.Collection("leaderboard").Document(currentUserID_Normalized);

        // [수정] 로그인 시 저장해둔 전역 변수 사용
        var nicknameRaw = this.currentUserNickname_Display;
        var nicknameLower = this.currentUserID_Normalized;

        await Task.WhenAll(
            userRef.UpdateAsync(new Dictionary<string, object> {
                { "HighScore", candidateScore },
                { "UpdatedAt", FieldValue.ServerTimestamp }
            }),
            lbRef.SetAsync(new Dictionary<string, object> {
                { "uid", currentUserID_Normalized }, // [수정]
                { "nickname", nicknameRaw },
                { "nicknameLower", nicknameLower },
                { "HighScore", candidateScore },
                { "UpdatedAt", FieldValue.ServerTimestamp }
            }, SetOptions.MergeAll)
        );
    }

    // [When] 랭킹 보드 UI를 표시해야 할 때 호출
    // [What] 'leaderboard' 컬렉션에서 'HighScore' 기준으로 상위 10명을 조회.
    public async Task<string> GetTop10RankingStringAsync()
    {
        var snap = await db.Collection("leaderboard")
            .OrderByDescending("HighScore")
            .OrderBy("UpdatedAt") // 동점 시 최근 갱신 우선 (이슈: 먼저 달성한 사람이 우선이어야 할 수도 있음)
            .Limit(10)
            .GetSnapshotAsync();

        if (snap.Count == 0) return "랭킹 데이터가 없습니다.";

        StringBuilder sb = new StringBuilder();
        int rank = 1;
        foreach (var doc in snap.Documents)
        {
            string name = doc.ContainsField("nickname") ? doc.GetValue<string>("nickname") : "(닉네임없음)";
            int score = doc.ContainsField("HighScore") ? (int)(doc.GetValue<long>("HighScore")) : 0;
            sb.AppendLine($"{rank}위 {name} - {score}");
            rank++;
        }
        return sb.ToString();
    }

    // ===================== 내부 유틸 =====================

    // [When] LogLanding, LogDeath 등 Vector3 위치를 DB에 저장할 때
    // [What] Unity의 Vector3 구조체를 Firestore가 이해할 수 있는 Dictionary(Map) 형태로 변환
    //        (예: { "x": 1.0, "y": 2.0, "z": 3.0 })
    private Dictionary<string, object> Vec(Vector3 v)
    {
        return new Dictionary<string, object>
        {
            { "x", v.x }, { "y", v.y }, { "z", v.z }
        };
    }

    // [When] 세션 로그를 기록하는 모든 메서드의 시작 지점
    // [What] 'currentUserID_Normalized'와 '_currentSessionId'가 유효한지 확인.
    private bool EnsureSession(string caller)
    {
        // [수정] currentDeviceID -> currentUserID_Normalized
        if (string.IsNullOrEmpty(currentUserID_Normalized))
        {
            Debug.LogWarning($"{caller}: currentUserID_Normalized is null. Call HandleUserAuthentication first.");
            return false;
        }
        if (string.IsNullOrEmpty(_currentSessionId))
        {
            Debug.LogWarning($"{caller}: sessionId is null. Call StartNewSession first.");
            return false;
        }
        return true;
    }

    // [When] 'users' 컬렉션의 '내 문서'를 참조해야 할 때 (SaveNickname, SaveHighScore 등)
    // [What] db.Collection("users").Document(currentDeviceID) 코드를 짧게 줄여주는 헬퍼 메서드
    private DocumentReference CurrentUserRef()
    {
        return db.Collection("users").Document(currentUserID_Normalized);
    }

    // [When] 닉네임을 'leaderboard'에 저장하거나, 닉네임으로 검색할 때
    // [What] 닉네임의 양쪽 공백을 제거(Trim)하고 모두 소문자(ToLowerInvariant)로 변경하여,
    //        'Apple', 'apple', ' apple ' 등이 모두 'apple'로 동일하게 취급되도록 표준화함.
    private string NormalizeNicknameLower(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        return s.Trim().ToLowerInvariant();
    }
}

[FirestoreData]
public class UserData
{
    [FirestoreProperty] public string Nickname { get; set; }
    [FirestoreProperty] public string FirstStartTime { get; set; }
    [FirestoreProperty] public int HighScore { get; set; }
    [FirestoreProperty] public long GameOpenedCount { get; set; }
    [FirestoreProperty] public long GameStartCount { get; set; }
    [FirestoreProperty] public Timestamp UpdatedAt { get; set; }
}
