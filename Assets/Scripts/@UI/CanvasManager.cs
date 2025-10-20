using TMPro;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    [Header("점수판 UI")]
    [SerializeField] private TextMeshProUGUI InGameScoreTMP;
    [SerializeField] private TextMeshProUGUI gameOverScoreTMP;
    [SerializeField] private TextMeshProUGUI gameOverScoreBestTMP;

    public void Initiate()
    {
        InGameScoreTMP.gameObject.SetActive(false);

        // 게임 시작 시 점수판 초기화
        UpdateInGameScore(0);
        UpdateGameOverCurrentScore(0);
        UpdateGameOverMaxScore(0); 
    }

    // === ScoreManager가 호출할 공개 메서드들 ===

    public void UpdateInGameScore(int score)
    {
        InGameScoreTMP.text = score.ToString() + "점";
    }

    public void UpdateGameOverCurrentScore(int currentScore)
    {
        gameOverScoreTMP.text = "현재 점수 : " + currentScore.ToString() + "점";
    }

    public void UpdateGameOverMaxScore(int maxScore)
    {
        gameOverScoreBestTMP.text = "최고 점수 : " + maxScore.ToString() + "점";
    }

    public void SetInGameScoreActive(bool isActive)
    {
        InGameScoreTMP.gameObject.SetActive(isActive);
    }
}