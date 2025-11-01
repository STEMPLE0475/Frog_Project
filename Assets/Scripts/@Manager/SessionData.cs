using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 한 판(세션) 동안의 임시 기록용 클래스.
/// (Analytics 전송 대상)
/// </summary>
public class SessionData
{
    public string Version { get; set; }
    public string SessionId { get; private set; }
    public DateTime StartTime { get; private set; }

    // 1. 점수 및 콤보
    public int FinalScore { get; set; }
    public int BestCombo { get; set; }

    // 2. 착지 관련
    public int TotalLandings { get; set; } // 총 착지 횟수
    public int PerfectLandings { get; set; }
    public int GoodLandings { get; set; }
    public int BadLandings { get; set; }

    // 3. 죽음
    public Vector3? DeathPosition { get; set; } // 죽은 위치 (죽지 않고 종료 시 null)

    /// <summary>
    /// 새 세션 시작 시 초기화
    /// </summary>
    public SessionData(string version)
    {
        Version = version;
        SessionId = Guid.NewGuid().ToString("N");
        StartTime = DateTime.Now;

        FinalScore = 0;
        BestCombo = 0;
        TotalLandings = 0;
        PerfectLandings = 0;
        GoodLandings = 0;
        BadLandings = 0;
        DeathPosition = null;
    }
}