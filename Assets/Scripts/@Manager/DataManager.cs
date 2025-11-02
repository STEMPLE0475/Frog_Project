using UnityEngine;
using System;

public class DataManager : MonoBehaviour
{
    public event Action<int> OnScoreChanged;
    public event Action<int> OnMaxScoreChanged;
    public event Action<int> OnComboChanged;

    private string version;

    private SessionData _currentSession;
    private int _currentCombo = 0;
    private int _maxScore = 0;
    //private int score = 0;

    public void Initiate(string version) { this.version = version; }

    // NetworkManager에서 UserData 로드 완료 시 호출
    public void SetInitialUserData(UserData data)
    {
        if (data == null) return;
        _maxScore = data.HighScore;
        OnMaxScoreChanged?.Invoke(_maxScore); 
    }

    // GameMager 게임 시작 시 호출
    public string StartNewSession()
    {
        _currentSession = new SessionData(version);
        ResetCombo();
        _currentSession.TotalCheckPoint = 0;

        // UI 리셋
        OnScoreChanged?.Invoke(0);
        OnComboChanged?.Invoke(0);
        OnMaxScoreChanged?.Invoke(_maxScore);

        return _currentSession.SessionId;
    }

    // PlayerController가 Land 이벤트를 발생시키면 GameManager가 이 함수를 호출
    public void HandleLanding(LandingAccuracy accuracy)
    {
        if (_currentSession == null) return;

        _currentSession.TotalLandings++;

        switch (accuracy)
        {
            case LandingAccuracy.Bad: 
                _currentSession.BadLandings++;
                ResetCombo();
                break;
            case LandingAccuracy.Good: 
                _currentSession.GoodLandings++;
                ResetCombo();
                PlusScore(10);
                break;
            case LandingAccuracy.Perfect: 
                _currentSession.PerfectLandings++;
                PlusCombo();
                PlusScore(10 * _currentCombo);
                break;
        }
    }

    public void HandleCheckPoint(int checkPointNum)
    {
        _currentSession.TotalCheckPoint = checkPointNum;
        PlusScore(150 * checkPointNum);
    }

    public void LogDeath(Vector3 deathPosition)
    {
        if (_currentSession == null) return;
        _currentSession.DeathPosition = deathPosition;
    }

    public void PlusScore(int addScore)
    {
        if (_currentSession == null) return;

        _currentSession.FinalScore += addScore;
        OnScoreChanged?.Invoke(_currentSession.FinalScore); // 점수가 변경되었음을 알림
    }

    public void PlusCombo()
    {
        if (_currentSession == null) return;
        _currentCombo++;
        OnComboChanged?.Invoke(_currentCombo);
        CheckBestCombo();
    }

    public void CheckBestCombo()
    {
        if (_currentCombo > _currentSession.BestCombo)
        {
            _currentSession.BestCombo = _currentCombo;
        }
    }

    public void ResetCombo()
    {
        _currentCombo = 0;
        OnComboChanged?.Invoke(_currentCombo);
    }

    //Network Manager을 통해 UGS에 Event로 세션 데이터를 전송
    public SessionData EndSessionAndGetResults()
    {
        if (_currentSession == null) return null;

        // 최고 점수 로컬 갱신
        if (_currentSession.FinalScore > _maxScore)
        {
            _maxScore = _currentSession.FinalScore;
            OnMaxScoreChanged?.Invoke(_maxScore);
        }
        return _currentSession;
    }

    public int GetMaxScore()
    {
        return _maxScore;
    }
}