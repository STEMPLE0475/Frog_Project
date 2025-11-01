using System;

[Serializable]
public class UserData
{
    public string Nickname { get; set; }
    public int HighScore { get; set; }
    public long Coin { get; set; }
    public long GameOpenedCount { get; set; }
    public long SessionStartCount { get; set; }

    public UserData(string nickname)
    {
        Nickname = nickname;
        HighScore = 0;
        Coin = 0;
        GameOpenedCount = 1; // ? 
        SessionStartCount = 0; 
    }
}