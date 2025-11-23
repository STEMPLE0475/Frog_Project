using System;

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
    }
}