using UnityEngine;
using System;

public class ScoreManager : MonoBehaviour
{
    // 점수 변경 시 UI가 업데이트할 수 있도록 이벤트 발생
    public event Action<int> OnScoreChanged;
    public event Action<int> OnMaxScoreChanged;
    // public event Action<int> OnComboChanged; // (필요하다면)

    private int score = 0;
    private int max_score = 0;
    private int combo = 0; // (GameManager의 콤보 로직을 가져옴)

    // DB에서 로드된 최고 점수를 설정
    public void SetInitialMaxScore(UserData data)
    {
        if (data == null) return;
        max_score = data.HighScore;
        OnMaxScoreChanged?.Invoke(max_score); // UI 갱신 요청
    }

    // DB 매니저를 외부에서 주입받음
    public void Initiate()
    {
    }

    // PlayerController가 Land 이벤트를 발생시키면 GameManager가 이 함수를 호출
    public void HandleLanding(LandingAccuracy accuracy)
    {
        // (GameManager의 Land 로직을 가져옴)
        // (이 로직은 PlayerController 쪽 콤보 로직과 중복되는 것 같아 보임.
        //  PlayerController가 콤보를 관리한다면, 이 함수는 점수만 관리해야 함)

        // 여기서는 GameManager의 Land 로직을 그대로 가져왔다고 가정
        switch (accuracy)
        {
            case LandingAccuracy.Bad: // 0
                combo = 0; // 콤보 리셋
                break;
            case LandingAccuracy.Good: // 1
                combo = 0;
                break;
            case LandingAccuracy.Perfect: // 2
                combo += 1;
                PlusScore(10 * combo); // 예: 퍼펙트
                // canvasManager.PlayIllustAnimation(0); // 이 호출은 GameManager가 해야 함
                break;
        }
        // OnComboChanged?.Invoke(combo);
    }

    public void PlusScore(int addScore)
    {
        this.score += addScore;
        OnScoreChanged?.Invoke(score); // 점수가 변경되었음을 알림
    }

    public void ResetScore()
    {
        score = 0;
        combo = 0;
        OnScoreChanged?.Invoke(score);
        OnMaxScoreChanged?.Invoke(max_score); // 최고 점수 UI도 리셋
    }

    public void SaveScore()
    {
        if (score > max_score)
        {
            max_score = score;
            OnMaxScoreChanged?.Invoke(max_score);
        }
    }

    public int GetMaxScore()
    {
        return max_score;
    }
}