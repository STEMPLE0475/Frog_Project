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
    private const string DEVICE_ID_KEY = "LocalDeviceID";
    private string currentDeviceID;
    private UserData loadedUserData;

    // ===================== 신규: 세션 로깅 내부 상태 =====================
    private string _currentSessionId = null;       // 현재 세션 식별자
    private DateTime _sessionStartLocal;           // 로컬 기준 시작시각(디버그/전송용)
    private int _sessionJumpCount = 0;             // 세션 동안 점프 횟수
    private int _sessionBestCombo = 0;             // 세션 최고 콤보
    private Vector3? _lastDeathPosition = null;    // 마지막 사망 지점(있다면)

    // 테스트용 스위치(에디터에서 On/Off 가능)
    [Header("Logging Test")]
    [SerializeField] private bool ENABLE_DB_START_TEST = false;

    public void Initiate()
    {
        db = FirebaseFirestore.DefaultInstance;
        currentDeviceID = GetOrCreateDeviceID();

        // 시작하자마자 인증 및 데이터 로드 시도
        HandleUserAuthentication("");
    }

    private void Start()
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
    }

    public string GetOrCreateDeviceID()
    {
        string deviceID = PlayerPrefs.GetString(DEVICE_ID_KEY, "");
        if (string.IsNullOrEmpty(deviceID))
        {
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

    // [When] 1. Initiate() 시 (닉네임 없이)
    //        2. 유저가 닉네임 입력 UI 등에서 닉네임을 확정했을 때
    // [What] 닉네임이 주어지면 닉네임 저장을 비동기로 시도하고,
    //        항상 'currentDeviceID'를 기준으로 유저 데이터를 로드/생성함
    public void HandleUserAuthentication(string inputNickname)
    {
        if (!string.IsNullOrWhiteSpace(inputNickname))
        {
            // 표시용 닉네임을 저장(비동기, 중복 허용)
            _ = SaveNicknameRawAsync(inputNickname);
        }

        // 내 deviceID 기준으로만 데이터 로드
        ProcessDataByDeviceID(currentDeviceID, inputNickname ?? "");
    }

    // [When] HandleUserAuthentication()에서 호출됨
    // [What] 'users' 컬렉션에서 'docId'(즉, currentDeviceID) 문서를 가져옴.
    //        - 문서가 있으면 : 데이터를 읽고(ConvertTo), GameOpenedCount를 1 증가시킴.
    //        - 문서가 없으면(신규 유저) : 기본 UserData 객체를 생성하여 DB에 저장(SetAsync)함.
    //        - 마지막으로, DAU 집계를 위해 MarkDailyActive()를 호출하고 OnUserDataLoaded 이벤트를 발생시킴.
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
                docRef.UpdateAsync("GameOpenedCount", FieldValue.Increment(1));
                loadedUserData = snapshot.ConvertTo<UserData>();
            }
            else
            {
                UserData initialData = new UserData
                {
                    Nickname = finalNickname,
                    FirstStartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    HighScore = 0,
                    GameOpenedCount = 1,
                    GameStartCount = 0
                };
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
        if (string.IsNullOrEmpty(currentDeviceID)) return;
        db.Collection("users")
          .Document(currentDeviceID)
          .UpdateAsync("GameStartCount", FieldValue.Increment(1));
    }

    // [When] (구버전) 게임 오버 시 최고 점수 갱신용으로 호출 (현재 SaveHighScoreIfBestAsync로 대체된 듯)
    // [What] 로컬 캐시(loadedUserData)의 HighScore보다 새 점수가 높으면,
    //        로컬 캐시를 갱신하고 DB의 'HighScore' 필드도 갱신함
/*    public void SaveHighScore(int newHighScore)
    {
        if (loadedUserData == null || newHighScore <= loadedUserData.HighScore) return;

        loadedUserData.HighScore = newHighScore;
        if (string.IsNullOrEmpty(currentDeviceID)) return;

        DocumentReference docRef = db.Collection("users").Document(currentDeviceID);
        docRef.UpdateAsync("HighScore", newHighScore);
    }
*/
    // [When] 1. ProcessDataByDeviceID (앱 실행 시)
    //        2. StartNewSession (게임 시작 시)
    // [What] DAU(일일 활성 유저) 집계를 위해 'users/{userId}/daily/{오늘날짜KST}' 문서를 생성/업데이트함.
    //        'SetOptions.MergeAll'을 사용하여, 'app_open'으로 이미 문서가 있어도 'game_start' 정보 등을 덮어쓰지 않고 병합함.
    private void MarkDailyActive(string reason = "app_open")
    {
        if (string.IsNullOrEmpty(currentDeviceID)) return;

        string dateId = GetKSTDateId();
        var dailyRef = db.Collection("users").Document(currentDeviceID)
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
        if (string.IsNullOrEmpty(currentDeviceID))
        {
            Debug.LogWarning("StartNewSession: currentDeviceID is null or empty.");
            return null;
        }

        _currentSessionId = Guid.NewGuid().ToString("N");
        _sessionStartLocal = DateTime.UtcNow;
        _sessionJumpCount = 0;
        _sessionBestCombo = 0;
        _lastDeathPosition = null;

        var sessionRef = db.Collection("users")
                           .Document(currentDeviceID)
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
    //        (finalScore, endedAt, 그리고 세션 동안 누적된 jumpCount, bestCombo 등)
    public void EndCurrentSession(int finalScore)
    {
        if (!EnsureSession("EndCurrentSession")) return;

        var sessionRef = db.Collection("users")
                           .Document(currentDeviceID)
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
    //           (이벤트 문서 ID는 Firestore가 자동으로 생성)
    public void LogLanding(Vector3 landingPosition, string accuracy)
    {
        if (!EnsureSession("LogLanding")) return;

        _sessionJumpCount++;

        var evRef = db.Collection("users")
                      .Document(currentDeviceID)
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
                      .Document(currentDeviceID)
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
                      .Document(currentDeviceID)
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

    // [When] 닉네임 입력 UI 등에서 닉네임을 설정/변경할 때
    // [What] 닉네임 정보를 *두 곳*에 동시에(Task.WhenAll) 저장/업데이트함 (SetOptions.MergeAll로 병합)
    //        1. 'users/{userId}' : 메인 유저 정보 문서 (필수)
    //        2. 'leaderboard/{userId}' : 랭킹 조회를 최적화하기 위한 별도 컬렉션 (Denormalization)
    public async Task SaveNicknameRawAsync(string nicknameRaw)
    {
        if (string.IsNullOrEmpty(currentDeviceID)) return;

        var userRef = CurrentUserRef();
        var lbRef = db.Collection("leaderboard").Document(currentDeviceID);

        await Task.WhenAll(
            userRef.SetAsync(new Dictionary<string, object> {
                { "Nickname", nicknameRaw },
                { "UpdatedAt", FieldValue.ServerTimestamp }
            }, SetOptions.MergeAll),

            lbRef.SetAsync(new Dictionary<string, object> {
                { "uid", currentDeviceID },
                { "nickname", nicknameRaw },                         // 표시용 원문
                { "nicknameLower", NormalizeNicknameLower(nicknameRaw) }, // 조회 최적화
                { "UpdatedAt", FieldValue.ServerTimestamp }
            }, SetOptions.MergeAll)
        );

        if (loadedUserData == null) loadedUserData = new UserData();
        loadedUserData.Nickname = nicknameRaw;
        OnUserDataLoaded?.Invoke(loadedUserData);
    }

    // [When] 게임 오버 시, 'EndCurrentSession'과 별개로 (또는 직후에) 최고 점수 갱신을 위해 호출
    // [What] 현재 게임 점수(candidateScore)가 로컬 캐시의 최고 점수(currentBest)보다 높을 때만 DB에 저장.
    //        'SaveNicknameRawAsync'와 마찬가지로 *두 곳*('users'와 'leaderboard')에 동시에 업데이트함.
    public async Task SaveHighScoreIfBestAsync(int candidateScore)
    {
        if (string.IsNullOrEmpty(currentDeviceID)) return;

        int currentBest = loadedUserData?.HighScore ?? 0;
        if (candidateScore <= currentBest) return;

        if (loadedUserData == null) loadedUserData = new UserData();
        loadedUserData.HighScore = candidateScore;

        var userRef = CurrentUserRef();
        var lbRef = db.Collection("leaderboard").Document(currentDeviceID);

        var nicknameRaw = loadedUserData.Nickname ?? ("User_" + currentDeviceID.Substring(0, 5).ToUpper());
        var nicknameLower = NormalizeNicknameLower(nicknameRaw);

        await Task.WhenAll(
            userRef.UpdateAsync(new Dictionary<string, object> {
                { "HighScore", candidateScore },
                { "UpdatedAt", FieldValue.ServerTimestamp }
            }),
            lbRef.SetAsync(new Dictionary<string, object> {
                { "uid", currentDeviceID },
                { "nickname", nicknameRaw },
                { "nicknameLower", nicknameLower },
                { "HighScore", candidateScore },
                { "UpdatedAt", FieldValue.ServerTimestamp }
            }, SetOptions.MergeAll)
        );
    }

    // [When] 닉네임으로 유저의 점수를 검색하는 기능(예: '친구 점수 보기')에 사용
    // [What] 'leaderboard' 컬렉션에서 'nicknameLower' 필드를 사용해 특정 닉네임의 *최고 점수* 1건을 조회.
    //        (동일 닉네임이 여러 명일 경우, 그 중 가장 높은 점수를 가진 사람의 점수를 반환)
    /*public async Task<int?> GetHighScoreByNicknameAsync(string nicknameRaw)
    {
        string nicknameLower = NormalizeNicknameLower(nicknameRaw);
        if (string.IsNullOrEmpty(nicknameLower)) return null;

        var snap = await db.Collection("leaderboard")
            .WhereEqualTo("nicknameLower", nicknameLower)
            .OrderByDescending("HighScore")   // 동닉네임 중 최고점 1건
            .Limit(1)
            .GetSnapshotAsync();

        var doc = snap.Documents.FirstOrDefault();  // IEnumerable → 인덱싱 대신
        if (doc == null) return null;

        return doc.ContainsField("HighScore")
            ? (int)(doc.GetValue<long>("HighScore"))
            : 0;
    }*/

    // [When] 랭킹 보드 UI를 표시해야 할 때 호출
    // [What] 'leaderboard' 컬렉션에서 'HighScore' 기준으로 상위 10명을 조회.
    //        동점일 경우 'UpdatedAt'(최근 갱신) 순으로 정렬함.
    //        결과를 "1위 닉네임 - 점수\n2위..." 형태의 단일 문자열로 가공하여 반환.
    public async Task<string> GetTop10RankingStringAsync()
    {
        var snap = await db.Collection("leaderboard")
            .OrderByDescending("HighScore")
            .OrderBy("UpdatedAt") // 동점 시 최근 갱신 우선
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

    // [When] 세션 로그를 기록하는 모든 메서드(LogLanding, LogDeath, EndSession 등)의 시작 지점
    // [What] 'currentDeviceID'와 '_currentSessionId'가 유효한지(null이 아닌지) 확인.
    //        만약 유효하지 않으면(예: 세션 시작 전 로그 호출) 경고를 출력하고 false를 반환하여,
    //        호출한 메서드가 즉시 중단되도록 함 (NullReferenceException 방지)
    private bool EnsureSession(string caller)
    {
        if (string.IsNullOrEmpty(currentDeviceID))
        {
            Debug.LogWarning($"{caller}: currentDeviceID is null. Call HandleUserAuthentication first.");
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
        return db.Collection("users").Document(currentDeviceID);
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
