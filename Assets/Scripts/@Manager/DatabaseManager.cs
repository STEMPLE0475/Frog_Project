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

    // ✅ 중복 허용: 닉네임이 들어오면 내 문서에 그대로 저장하고, 인증은 내 deviceID로만 처리
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

    // 게임 시작 시
    public void IncrementGameStartCount()
    {
        if (string.IsNullOrEmpty(currentDeviceID)) return;
        db.Collection("users")
          .Document(currentDeviceID)
          .UpdateAsync("GameStartCount", FieldValue.Increment(1));
    }

    // 최고 점수 저장 시(기존 로컬용)
    public void SaveHighScore(int newHighScore)
    {
        if (loadedUserData == null || newHighScore <= loadedUserData.HighScore) return;

        loadedUserData.HighScore = newHighScore;
        if (string.IsNullOrEmpty(currentDeviceID)) return;

        DocumentReference docRef = db.Collection("users").Document(currentDeviceID);
        docRef.UpdateAsync("HighScore", newHighScore);
    }

    // ===================== DAU 보조: 당일 활성 흔적 =====================
    private string GetKSTDateId() // "YYYYMMDD"
    {
        var nowKst = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time"));
        return nowKst.ToString("yyyyMMdd");
    }

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

    // ===================== 신규: 세션 로깅용 공개 API =====================
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
        // _currentSessionId = null; // 필요 시 해제
    }

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

    // ===================== 내부 유틸 =====================
    private Dictionary<string, object> Vec(Vector3 v)
    {
        return new Dictionary<string, object>
        {
            { "x", v.x }, { "y", v.y }, { "z", v.z }
        };
    }

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

    private DocumentReference CurrentUserRef()
    {
        return db.Collection("users").Document(currentDeviceID);
    }

    private string NormalizeNicknameLower(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        return s.Trim().ToLowerInvariant();
    }

    // ==================== 닉네임 '그대로' 저장(중복 허용) + 리더보드 동기화 ====================
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

    // ==================== 최고기록 저장(더 클 때만) ====================
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

    // ==================== 특정 닉네임의 최고기록 조회(동닉네임 다수 → 최고점 1건) ====================
    public async Task<int?> GetHighScoreByNicknameAsync(string nicknameRaw)
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
    }

    // ==================== TOP10 랭킹 문자열 ====================
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
}

[FirestoreData]
public class UserData
{
    [FirestoreProperty] public string Nickname { get; set; }
    [FirestoreProperty] public string FirstStartTime { get; set; }
    [FirestoreProperty] public int HighScore { get; set; }
    [FirestoreProperty] public long GameOpenedCount { get; set; }
    [FirestoreProperty] public long GameStartCount { get; set; }
}
