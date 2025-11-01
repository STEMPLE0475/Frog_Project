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

public class NetworkManager : MonoBehaviour
{
    public event Action<UserData> OnUserDataLoaded; // 데이터 로드 완료 이벤트

    private UserData loadedUserData;

    private string currentUserID_Normalized;    // "mynick" (표준화된 ID)
    private string currentUserNickname_Display; // "MyNick" (표시용 닉네임)

    private const string CLOUD_SAVE_USER_DATA_KEY = "USER_DATA";
    private const string LEADERBOARD_ID = "Frog_Jump"; 

    // 1) UGS 초기화 (GameManager에서 await networkManager.Initiate();)
    public async Task Initiate()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            AnalyticsService.Instance.StartDataCollection();
            Debug.Log($"UGS 로그인 | PlayerID={AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"UGS 초기화 실패: {e.Message}");
        }
    }

    // 2) 로그인/유저 데이터 로드 
    public void HandleUserAuthentication(string inputNickname)
    {
        if (string.IsNullOrWhiteSpace(inputNickname))
        {
            Debug.LogError("로그인 실패: 닉네임이 비어있습니다.");
            return;
        }

        currentUserNickname_Display = inputNickname.Trim();
        currentUserID_Normalized = NormalizeNicknameLower(inputNickname);

        _ = ProcessDataByUserIDAsync(currentUserID_Normalized, currentUserNickname_Display);
    }

    // 내부 비동기 구현
    private async Task ProcessDataByUserIDAsync(string normalizedUserID, string displayNickname)
    {
        try
        {
            var dict = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { CLOUD_SAVE_USER_DATA_KEY });

            loadedUserData = null;

            if (dict != null && dict.TryGetValue(CLOUD_SAVE_USER_DATA_KEY, out var item) && item.Value != null)
            {
                string json = item.Value.GetAsString();
                if (!string.IsNullOrEmpty(json))
                {
                    loadedUserData = JsonConvert.DeserializeObject<UserData>(json);
                }
            }

            if (loadedUserData == null)
            {
                loadedUserData = new UserData(displayNickname);
            }
            else
            {
                loadedUserData.GameOpenedCount++;
                loadedUserData.Nickname = displayNickname;
            }

            // 저장 및 닉네임 반영
            await SaveUserDataAsync();
            await AuthenticationService.Instance.UpdatePlayerNameAsync(displayNickname);

            // 콜백
            OnUserDataLoaded?.Invoke(loadedUserData);
            Debug.Log("유저 데이터 로드/갱신 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"ProcessDataByUserIDAsync 실패: {e.Message}");
        }
    }

    // 4) 세션 시작/종료 (Analytics 이벤트 전송)
    public void StartNewSession(string sessionId, string startReason = "start_button")
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.LogWarning("StartNewSession: sessionId가 비어있습니다.");
            return;
        }

        if (loadedUserData != null)
        {
            loadedUserData.SessionStartCount++;
            _ = SaveUserDataAsync();
        }
        else
        {
            Debug.LogWarning("StartNewSession: loadedUserData가 null이라 SessionStartCount를 저장할 수 없습니다.");
        }

        // 'game_start' 이벤트 전송
        AnalyticsService.Instance.RecordEvent(new CustomEvent("session_start")
        {
            { "sessionId", sessionId },
            { "startReason", startReason }
        });
    }

    public void EndCurrentSession(SessionData sessionData)
    {
        if (sessionData == null || string.IsNullOrEmpty(sessionData.SessionId))
        {
            Debug.LogWarning("EndCurrentSession: sessionData가 null입니다.");
            return;
        }

        // 'game_end' 요약 이벤트 전송
        AnalyticsService.Instance.RecordEvent(new CustomEvent("session_result_0")
        {
            { "version", sessionData.Version },
            { "sessionId_str", sessionData.SessionId },
            { "finalScore", sessionData.FinalScore },
            { "landCount", sessionData.TotalLandings },
            { "bestCombo", sessionData.BestCombo },
            { "perfectCount", sessionData.PerfectLandings },
            { "goodCount", sessionData.GoodLandings },
            { "badCount", sessionData.BadLandings },
            { "lastDeath_X", sessionData.DeathPosition?.x ?? 0f }
        });
    }

    // 5) 최고점 저장 + 리더보드 반영
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
            Debug.Log($"리더보드 반영 완료: {candidateScore}");
        }
        catch (Exception e)
        {
            Debug.LogError($"최고 점수 저장 실패: {e.Message}");
        }
    }

    // 6) 리더보드 Top10
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
            Debug.LogError($"랭킹 로드 실패: {e.Message}");
            return "랭킹을 불러올 수 없습니다.";
        }
    }

    // 8) 저장 유틸 (경고 없는 Player 스코프)
    private async Task SaveUserDataAsync()
    {
        if (loadedUserData == null)
        {
            Debug.LogWarning("SaveUserDataAsync: loadedUserData가 null이라 저장할 수 없습니다.");
            return;
        }

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

