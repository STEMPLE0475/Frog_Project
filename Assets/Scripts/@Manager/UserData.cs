using System;
using System.Collections.Generic;

[Serializable]
public class UserData
{
    public string Nickname { get; set; }
    public int HighScore { get; set; }
    public long Coin { get; set; }
    public long GameOpenedCount { get; set; }
    public long SessionStartCount { get; set; }
    public bool isClear { get; set; }
    public int ConsecutiveLoginDays { get; set; } // 연속 접속 횟수
    public string LastLoginDate { get; set; } // 최근 접속일 (yyyy-MM-dd 형식 문자열)
    public int MaxConsecutiveLoginDays { get; set; } // 최대 연속 접속일
    public int LoginTimes;

    //스킨 관련
    public HashSet<int> AcquiredSkinList { get; set; } // 보유 스킨 리스트
    public int SelectedSkin;

    //이벤트 출석 관련
    public int EventLoginTimes { get; set; }

    public UserData(string nickname)
    {
        Nickname = nickname;
        HighScore = 0;
        Coin = 0;
        GameOpenedCount = 1; // ? 
        SessionStartCount = 0; 
        isClear = false;
        ConsecutiveLoginDays = 1; // 새 유저는 1일차
        MaxConsecutiveLoginDays = 1; // 최대 연속 접속일
        LastLoginDate = DateTime.Today.ToString("yyyy-MM-dd");


        AcquiredSkinList = new HashSet<int>();
        AcquiredSkinList.Add(0);
        SelectedSkin = 0;
    }
}