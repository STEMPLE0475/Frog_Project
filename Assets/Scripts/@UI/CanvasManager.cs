using System.Collections.Generic;
using TMPro;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    [Header("점수판 UI")]
    [SerializeField] private TextMeshProUGUI InGameScoreTMP;
    [SerializeField] private TextMeshProUGUI gameOverScoreTMP;
    [SerializeField] private TextMeshProUGUI gameOverScoreBestTMP;
    [SerializeField] private TutorialImage tutorialImage;
    [SerializeField] private WindChangeEffect windChangeEffect;
    [SerializeField] private Image windUI;
    [SerializeField] private List<Sprite> windUISprites;

    public void Initiate()
    {
        InGameScoreTMP.gameObject.SetActive(false);

        // 게임 시작 시 점수판 초기화
        UpdateInGameScore(0);
        UpdateGameOverCurrentScore(0);
        UpdateGameOverMaxScore(0);
        windChangeEffect.Initiate();
    }
    public void StartTutorialImageBlink()
    {
        tutorialImage.StartBlink();
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

    public void UpdateWind(Wind wind)
    {
        Debug.Log("바람 방향 : " + wind.direction.ToString());
        Debug.Log("바람 힘 " + wind.power.ToString());
        int spriteIndex = GetWindSpriteIndex(wind);

        // UI 이미지에 스프라이트 적용
        if (windUI != null && windUISprites != null && spriteIndex >= 0 && spriteIndex < windUISprites.Count)
        {
            windUI.sprite = windUISprites[spriteIndex];

            // 바람 힘이 0일 때 (Index 0)는 Image를 비활성화하거나 투명하게 처리할 수도 있습니다.
            // 여기서는 스프라이트만 할당하고, 바람의 힘이 0일 때 '바람 없음' 스프라이트가 표시되도록 가정합니다.
            windUI.enabled = true; // Image 컴포넌트 활성화
        }
        else if (windUI != null)
        {
            // 스프라이트 리스트가 부족하거나 잘못된 인덱스일 경우 대비
            windUI.enabled = false;
            Debug.LogError("바람 스프라이트 설정이 잘못되었습니다. Index: " + spriteIndex);
        }


        windChangeEffect.StartWindChangeEffect(wind);
    }

    private int GetWindSpriteIndex(Wind wind)
    {
        // 1. 바람 힘이 0일 경우 (바람 없음) -> Index 0
        if (wind.power == 0)
        {
            return 0;
        }

        // 2. 힘을 1, 2, 3으로 제한 (안전 장치)
        int power = Mathf.Clamp(wind.power, 1, 3);

        // 3. 인덱스 계산
        if (wind.direction == 1) // 방향 1 (우측): Index 1, 2, 3
        {
            // 힘 1 -> Index 1, 힘 2 -> Index 2, 힘 3 -> Index 3
            return power;
        }
        else // 방향 -1 (좌측): Index 4, 5, 6 (wind.direction이 -1일 경우)
        {
            // 힘 1 -> Index 4 (3 + 1)
            // 힘 2 -> Index 5 (3 + 2)
            // 힘 3 -> Index 6 (3 + 3)
            return 3 + power;
        }
    }
}