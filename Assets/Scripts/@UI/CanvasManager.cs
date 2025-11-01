using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private GameObject Header;
    [SerializeField] private TextMeshProUGUI Header_CurrentScoreTMP;
    [SerializeField] private TextMeshProUGUI Header_MaxScoreTMP;
    [SerializeField] private TextMeshProUGUI windIcon;

    [Header("ETC")]
    [SerializeField] private TextMeshProUGUI gameOverScoreTMP;
    [SerializeField] private TextMeshProUGUI gameOverScoreBestTMP;
    [SerializeField] private TutorialImage tutorialImage;
    [SerializeField] private WindowWindEffect windowWindEffect;

    public void Initiate()
    {
        SetActive_Header(false);

        // 게임 시작 시 점수판 초기화
        Update_Header_CurrentScore(0);
        Update_GameOverCurrentScore(0);
        Update_GameOverMaxScore(0);
        windowWindEffect.Initiate();
    }
    public void StartTutorialImageBlink() => tutorialImage.StartBlink();

    // 헤더 UI
    public void SetActive_Header(bool isActive) => Header.SetActive(isActive);
    public void Update_Header_CurrentScore(int score) => Header_CurrentScoreTMP.text = score.ToString() + "점";
    public void Update_Header_MaxScore(int score) => Header_MaxScoreTMP.text = score.ToString() + "점";

    // 게임 종료 UI
    public void Update_GameOverCurrentScore(int currentScore) => gameOverScoreTMP.text = "현재 점수 : " + currentScore.ToString() + "점";
    public void Update_GameOverMaxScore(int maxScore) => gameOverScoreBestTMP.text = "최고 점수 : " + maxScore.ToString() + "점";

    // Wind 관련
    public void UpdateWind(Wind wind)
    {
        Debug.Log("바람 방향 : " + wind.direction.ToString());
        Debug.Log("바람 힘 " + wind.power.ToString());

        // 1. 바람 텍스트 설정
        if (windIcon != null)
        {
            windIcon.text = GetWindText(wind);
        }
        else
        {
            Debug.LogError("windIcon (TextMeshProUGUI)이 할당되지 않았습니다.");
        }

        // 바람 변화 효과 사용 중지
        // windowWindEffect.StartWindChangeEffect(wind);
        
    }
    private string GetWindText(Wind wind)
    {
        // 1. 바람 힘이 0일 경우 (바람 없음) -> "-" 표시
        if (wind.power == 0)
        {
            return "-";
        }

        // 2. 힘을 1, 2, 3으로 제한 (안전 장치)
        // Mathf.Clamp는 Unity의 UnityEngine 네임스페이스에 있습니다.
        int power = UnityEngine.Mathf.Clamp(wind.power, 1, 3);

        // 3. 바람 방향에 따른 문자열 생성
        string arrowString = "";

        if (wind.direction == 1) // 방향 1 (우측)
        {
            // 힘 1 -> ">", 힘 2 -> ">>", 힘 3 -> ">>>"
            for (int i = 0; i < power; i++)
            {
                arrowString += ">";
            }
        }
        else if (wind.direction == -1) // 방향 -1 (좌측)
        {
            // 힘 1 -> "<", 힘 2 -> "<<", 힘 3 -> "<<<"
            for (int i = 0; i < power; i++)
            {
                arrowString += "<";
            }
        }
        else // 방향이 0이거나 기타 값일 경우 (예: 바람 없음으로 처리되지 않은 상태)
        {
            // 안전을 위해 - 반환
            return "-";
        }

        return arrowString;
    }
}