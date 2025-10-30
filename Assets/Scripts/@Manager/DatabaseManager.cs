using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Leaderboards;
using Unity.Services.Analytics;
using Newtonsoft.Json;

public class DatabaseManager : MonoBehaviour
{
    public event Action<UserData> OnUserDataLoaded; // 데이터 로드 완료 이벤트

    private UserData loadedUserData;
    private string currentUserID_Normalized;    // "mynick" (표준화된 ID)
    private string currentUserNickname_Display; // "MyNick" (표시용 닉네임)

    // 세션 상태
    private string _currentSessionId = null;
    private int _sessionJumpCount = 0;
    private int _sessionBestCombo = 0;
    private Vector3? _lastDeathPosition = null;

    // 상수
    private const string CLOUD_SAVE_USER_DATA_KEY = "USER_DATA";
    private const string LEADERBOARD_ID = "Frog_Jump"; // 대시보드의 Leaderboard ID와 동일해야 함

    // =========================================================
    // 1) UGS 초기화 (GameManager에서 await databaseManager.Initiate();)
    // =========================================================
    public async Task Initiate()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            AnalyticsService.Instance.StartDataCollection();
            Debug.Log($"✅ UGS 로그인 | PlayerID={AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ UGS 초기화 실패: {e.Message}");
        }
    }

    // =========================================================
    // 2) 로그인/유저 데이터 로드 — (기존 함수명 유지: 동기 메서드)
    // =========================================================
    public void HandleUserAuthentication(string inputNickname)
    {
        if (string.IsNullOrWhiteSpace(inputNickname))
        {
            Debug.LogError("로그인 실패: 닉네임이 비어있습니다.");
            return;
        }

        currentUserNickname_Display = inputNickname.Trim();
        currentUserID_Normalized = NormalizeNicknameLower(inputNickname);

        // 비동기 내부 처리 시작 (fire-and-forget)
        _ = ProcessDataByUserIDAsync(currentUserID_Normalized, currentUserNickname_Display);
    }

    // 내부 비동기 구현
    private async Task ProcessDataByUserIDAsync(string normalizedUserID, string displayNickname)
    {
        try
        {
            // 최신 API: Player 스코프 사용 (경고 제거)
            var dict = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { CLOUD_SAVE_USER_DATA_KEY });

            loadedUserData = null;

            if (dict != null && dict.TryGetValue(CLOUD_SAVE_USER_DATA_KEY, out var item) && item.Value != null)
            {
                // 값 추출
                string json = item.Value.GetAsString();
                if (!string.IsNullOrEmpty(json))
                {
                    loadedUserData = JsonConvert.DeserializeObject<UserData>(json);
                }
            }

            if (loadedUserData == null)
            {
                // 신규 유저
                loadedUserData = new UserData
                {
                    Nickname = displayNickname,
                    FirstStartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    HighScore = 0,
                    GameOpenedCount = 1,
                    GameStartCount = 0
                };
            }
            else
            {
                // 기존 유저 갱신
                loadedUserData.GameOpenedCount++;
                loadedUserData.Nickname = displayNickname;
            }

            // 저장 및 닉네임 반영
            await SaveUserDataAsync();
            await AuthenticationService.Instance.UpdatePlayerNameAsync(displayNickname);

            // 콜백
            OnUserDataLoaded?.Invoke(loadedUserData);
            Debug.Log("✅ 유저 데이터 로드/갱신 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ ProcessDataByUserIDAsync 실패: {e.Message}");
        }
    }

    // =========================================================
    // 3) 게임 시작 카운트 증가 (기존 시그니처 유지)
    // =========================================================
    public async void IncrementGameStartCount()
    {
        if (loadedUserData == null) return;
        loadedUserData.GameStartCount++;
        await SaveUserDataAsync();
    }

    // =========================================================
    // 4) 세션 시작/종료 (Analytics 이벤트 전송)
    // =========================================================
    public string StartNewSession(string startReason = "start_button")
    {
        if (string.IsNullOrEmpty(currentUserID_Normalized))
        {
            Debug.LogWarning("StartNewSession: currentUserID_Normalized가 비어있음");
            return null;
        }

        _currentSessionId = Guid.NewGuid().ToString("N");
        _sessionJumpCount = 0;
        _sessionBestCombo = 0;
        _lastDeathPosition = null;

        AnalyticsService.Instance.RecordEvent(new CustomEvent("game_start")
        {
            { "sessionId", _currentSessionId },
            { "startReason", startReason }
        });

        return _currentSessionId;
    }

    public void EndCurrentSession(int finalScore)
    {
        if (string.IsNullOrEmpty(_currentSessionId)) return;

        AnalyticsService.Instance.RecordEvent(new CustomEvent("game_end")
        {
            { "sessionId", _currentSessionId },
            { "finalScore", finalScore },
            { "jumpCount", _sessionJumpCount },
            { "bestCombo", _sessionBestCombo },
            { "lastDeath_X", _lastDeathPosition?.x ?? 0f },
            { "lastDeath_Y", _lastDeathPosition?.y ?? 0f },
            { "lastDeath_Z", _lastDeathPosition?.z ?? 0f }
        });

        _currentSessionId = null;
    }

    // =========================================================
    // 5) 플레이 로그 (착지/사망/콤보)
    // =========================================================
    public void LogLanding(Vector3 landingPosition, string accuracy)
    {
        if (string.IsNullOrEmpty(_currentSessionId)) return;
        _sessionJumpCount++;

        AnalyticsService.Instance.RecordEvent(new CustomEvent("landing")
        {
            { "sessionId", _currentSessionId },
            { "posX", landingPosition.x },
            { "posY", landingPosition.y },
            { "posZ", landingPosition.z },
            { "accuracy", accuracy }
        });
    }

    public void LogDeath(Vector3 deathPosition)
    {
        if (string.IsNullOrEmpty(_currentSessionId)) return;
        _lastDeathPosition = deathPosition;

        AnalyticsService.Instance.RecordEvent(new CustomEvent("death")
        {
            { "sessionId", _currentSessionId },
            { "posX", deathPosition.x },
            { "posY", deathPosition.y },
            { "posZ", deathPosition.z }
        });
    }

    public void LogCombo(int combo)
    {
        if (string.IsNullOrEmpty(_currentSessionId)) return;
        if (combo > _sessionBestCombo) _sessionBestCombo = combo;

        AnalyticsService.Instance.RecordEvent(new CustomEvent("combo")
        {
            { "sessionId", _currentSessionId },
            { "value", combo }
        });
    }

    // =========================================================
    // 6) 최고점 저장 + 리더보드 반영
    // =========================================================
    public async Task SaveHighScoreIfBestAsync(int candidateScore)
    {
        if (loadedUserData == null) return;
        if (candidateScore <= (loadedUserData.HighScore)) return;

        loadedUserData.HighScore = candidateScore;

        var saveTask = SaveUserDataAsync();
        // 최신 API: AddPlayerScoreAsync 사용
        var leaderboardTask = LeaderboardsService.Instance.AddPlayerScoreAsync(
            LEADERBOARD_ID, candidateScore);

        try
        {
            await Task.WhenAll(saveTask, leaderboardTask);
            Debug.Log($"🏆 리더보드 반영 완료: {candidateScore}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ 최고 점수 저장 실패: {e.Message}");
        }
    }

    // =========================================================
    // 7) 리더보드 Top10
    // =========================================================
    public async Task<string> GetTop10RankingStringAsync()
    {
        try
        {
            var res = await LeaderboardsService.Instance.GetScoresAsync(
                LEADERBOARD_ID, new GetScoresOptions { Limit = 10 });

            if (res?.Results == null || res.Results.Count == 0)
                return "랭킹 데이터가 없습니다.";

            var sb = new StringBuilder();
            int rank = 1;
            foreach (var r in res.Results)
            {
                sb.AppendLine($"{rank}위 {r.PlayerName} - {r.Score}");
                rank++;
            }
            return sb.ToString();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ 랭킹 로드 실패: {e.Message}");
            return "랭킹을 불러올 수 없습니다.";
        }
    }

    // =========================================================
    // 8) 저장 유틸 (경고 없는 Player 스코프)
    // =========================================================
    private async Task SaveUserDataAsync()
    {
        string json = JsonConvert.SerializeObject(loadedUserData);
        var data = new Dictionary<string, object> { { CLOUD_SAVE_USER_DATA_KEY, json } };

        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
    }

    private string NormalizeNicknameLower(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        return s.Trim().ToLowerInvariant();
    }
}

public class UserData
{
    public string Nickname { get; set; }
    public string FirstStartTime { get; set; }
    public int HighScore { get; set; }
    public long GameOpenedCount { get; set; }
    public long GameStartCount { get; set; }
}
