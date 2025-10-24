using Firebase.Extensions;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        // 에디터에서 DatabaseManager 단독 실행 시,
        // 한 번의 세션을 흉내 내며 로그가 잘 쌓이는지 확인합니다.
        if (ENABLE_DB_START_TEST)
        {
            // 1) 세션 시작
            string sid = StartNewSession("manual_test"); // 세션 ID 반환

            // 2) 점프/착지 로그 2회
            LogLanding(new Vector3(1.2f, 0f, 3.4f), "Perfect");
            LogLanding(new Vector3(2.5f, 0f, 4.1f), "Good");

            // 3) 콤보 갱신(예: 7콤보)
            LogCombo(7);

            // 4) 사망 위치 기록
            LogDeath(new Vector3(5.0f, -2.0f, 8.0f));

            // 5) 세션 종료(최종점수=1234, 재도전여부=true)
            EndCurrentSession(finalScore: 1234, isRetry: true);
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
        // ProcessDataByDeviceID가 성공적으로 loadedUserData를 채웠을 때
        // OnUserDataLoaded?.Invoke(loadedUserData); 를 호출해야 함.
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
                // 권장: CreatedAt(Timestamp) 필드 추가를 고려하세요(신규/복귀 집계에 유리)
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

            // DAU 마킹(선택) - KST 당일 활성 흔적
            MarkDailyActive("app_open");

            // 데이터 로드 알림
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

    // 최고 점수 저장 시
    public void SaveHighScore(int newHighScore)
    {
        if (loadedUserData == null || newHighScore <= loadedUserData.HighScore) return;

        loadedUserData.HighScore = newHighScore; // 로컬 데이터 갱신

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

    /// <summary>
    /// ✅ 세션 시작 함수
    /// - 언제 호출? : "게임 시작" 버튼 직후(실제 플레이 진입 시)
    /// - 인수 : startReason(string) - 세션 시작 사유(예: "start_button", "retry" 등)
    /// - 반환 : sessionId(string) - 이후 이벤트/종료에 사용할 세션 식별자
    /// - 저장 위치 : /users/{uid}/sessions/{sessionId}
    /// - 효과 : 내부 카운터 초기화, startedAt 기록
    /// </summary>
    public string StartNewSession(string startReason = "start_button")
    {
        if (string.IsNullOrEmpty(currentDeviceID))
        {
            Debug.LogWarning("StartNewSession: currentDeviceID is null or empty.");
            return null;
        }

        _currentSessionId = Guid.NewGuid().ToString("N"); // 32자
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
            { "startedAt", FieldValue.ServerTimestamp }, // 서버 기준
            { "startReason", startReason },
            { "jumpCount", 0 },
            { "bestCombo", 0 },
            { "finalScore", 0 },
            { "isRetry", false },
            { "endedAt", null },
            { "lastDeathPos", null }
        };

        sessionRef.SetAsync(payload);
        MarkDailyActive("game_start"); // DAU용 흔적

        return _currentSessionId;
    }

    /// <summary>
    /// ✅ 세션 종료 함수
    /// - 언제 호출? : "게임 오버" 또는 "스테이지 클리어" 등 플레이 종료 시점
    /// - 인수 :
    ///   finalScore(int) - 세션 최종 점수
    ///   isRetry(bool)   - 재도전 여부(게임오버 후 바로 재시작 등)
    /// - 반환 : void
    /// - 저장 위치 : /users/{uid}/sessions/{sessionId}
    /// - 효과 : endedAt, 최종 점수, 점프 수, 최고 콤보, 사망 지점까지 요약 반영
    /// </summary>
    public void EndCurrentSession(int finalScore, bool isRetry)
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
            { "isRetry", isRetry },
            { "jumpCount", _sessionJumpCount },
            { "bestCombo", _sessionBestCombo },
            { "lastDeathPos", _lastDeathPosition.HasValue ? Vec(_lastDeathPosition.Value) : null }
        };

        sessionRef.UpdateAsync(update);

        // 세션 종료 후 내부 상태 정리(선택)
        // _currentSessionId = null;  // 필요 시 주석 해제
    }

    /// <summary>
    /// ✅ 점프 착지 이벤트 기록
    /// - 언제 호출? : 플레이어가 "착지" 판정을 받은 즉시
    /// - 인수 :
    ///   landingPosition(Vector3) - 착지 좌표(월드/로컬 중 한 기준으로 통일)
    ///   accuracy(string)         - "Perfect", "Good" 등 판정 텍스트
    /// - 반환 : void
    /// - 저장 위치 : /users/{uid}/sessions/{sessionId}/events/{autoId}
    /// - 효과 : 원시 이벤트로 남기며, 세션 jumpCount 증가
    /// </summary>
    public void LogLanding(Vector3 landingPosition, string accuracy)
    {
        if (!EnsureSession("LogLanding")) return;

        _sessionJumpCount++;

        var evRef = db.Collection("users")
                      .Document(currentDeviceID)
                      .Collection("sessions").Document(_currentSessionId)
                      .Collection("events").Document(); // autoId

        var payload = new Dictionary<string, object>
        {
            { "type", "landing" },
            { "ts", FieldValue.ServerTimestamp },
            { "pos", Vec(landingPosition) },
            { "accuracy", accuracy }
        };

        evRef.SetAsync(payload);
    }

    /// <summary>
    /// ✅ 사망(실패) 이벤트 기록
    /// - 언제 호출? : 플레이어가 실패/사망 판정될 때(물에 떨어짐 등)
    /// - 인수 :
    ///   deathPosition(Vector3) - 사망 좌표
    /// - 반환 : void
    /// - 저장 위치 : /users/{uid}/sessions/{sessionId}/events/{autoId}
    /// - 효과 : 세션 요약에 마지막 사망 지점으로도 반영
    /// </summary>
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

    /// <summary>
    /// ✅ 콤보 갱신 이벤트
    /// - 언제 호출? : 콤보 수가 갱신될 때(점프 연속 성공 등)
    /// - 인수 :
    ///   combo(int) - 현재 콤보 수
    /// - 반환 : void
    /// - 저장 위치 : /users/{uid}/sessions/{sessionId}/events/{autoId}
    /// - 효과 : 세션 최고 콤보 갱신(bestCombo), 원시 이벤트 남김
    /// </summary>
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

    // Vector3 -> Firestore 저장용 Map
    private Dictionary<string, object> Vec(Vector3 v)
    {
        return new Dictionary<string, object>
        {
            { "x", v.x }, { "y", v.y }, { "z", v.z }
        };
    }

    // 세션이 없을 때 방어 + 경고 로그
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
