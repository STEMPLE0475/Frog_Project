using System.Collections;
using TMPro;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    private CanvasEffectManager effectManager;
    private PlayerController playerManager;

    [Header("연결 요소")]
    [SerializeField] TextMeshProUGUI comboTMP;
    [SerializeField] GameObject player;        // 2. 위치의 기준이 될 플레이어
    [SerializeField] Camera mainCamera;        // 3. 월드-스크린 변환에 사용할 메인 카메라

    [Header("애니메이션 설정")]
    [SerializeField] float animationDuration = 1.5f; // 효과 지속 시간
    [SerializeField] float moveDistanceUp = 150;    // 위로 올라갈 거리 (스크린 픽셀)
    [SerializeField] Vector3 worldSpaceOffset = new Vector3(0, 2f, 0); // "살짝 위" (플레이어 기준 월드 좌표 오프셋)
    [SerializeField] Vector3 screenSpaceOffset = new Vector3(50f, 0, 0);  // 스폰 위치의 스크린 좌표 추가 오프셋 (예: 왼쪽으로 -100)

    [SerializeField] public TextMeshProUGUI InGameScoreTMP;
    [SerializeField] public TextMeshProUGUI gameOverScoreTMP;
    [SerializeField] public TextMeshProUGUI gameOverScoreBestTMP;
    private Coroutine runningComboCoroutine;

    public void Initiate(PlayerController playerController)
    {
        this.playerManager = playerController;
        effectManager = GetComponentInChildren<CanvasEffectManager>();
        effectManager.Initiate(playerController);
        playerController.OnCombo += ComboHandler;

        comboTMP.gameObject.SetActive(false);
    }

    public void PlayIllustAnimation(int index)
    {
        effectManager.PlayIllustEffect(index);
    }

    public void ComboHandler(int combo)
    {
        if (combo == 0) return;

        // 1. 만약 이전 콤보 코루틴이 실행 중이었다면 즉시 중지
        if (runningComboCoroutine != null)
        {
            StopCoroutine(runningComboCoroutine);
        }

        // 2. 텍스트 설정
        comboTMP.text = "COMBO " + combo;

        // 3. 위치 계산 (가장 중요!)
        // (1) 플레이어의 월드 위치 + "살짝 위" 오프셋
        Vector3 worldPos = player.transform.position + worldSpaceOffset;

        // (2) 월드 좌표를 스크린 좌표로 변환
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

        // (3) RectTransform의 position에 스크린 좌표 적용
        //     (localPosition이 아님!)
        //     (추가 스크린 오프셋 적용)
        comboTMP.transform.position = screenPos + screenSpaceOffset;


        // 4. 알파값(투명도)을 1(불투명)로 리셋
        Color startColor = comboTMP.color;
        comboTMP.color = new Color(startColor.r, startColor.g, startColor.b, 1f);

        // 5. 텍스트 활성화
        comboTMP.gameObject.SetActive(true);

        // 6. 애니메이션 코루틴 시작
        runningComboCoroutine = StartCoroutine(ShowComboEffect());
    }

    private IEnumerator ShowComboEffect()
    {
        float elapsedTime = 0f;

        // .position 사용 (localPosition 아님)
        Vector3 startPosition = comboTMP.transform.position;
        Vector3 targetPosition = startPosition + Vector3.up * moveDistanceUp;
        Color startColor = comboTMP.color;

        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration;

            // .position 사용 (localPosition 아님)
            comboTMP.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            // 흐려지기
            float newAlpha = Mathf.Lerp(1f, 0f, t);
            comboTMP.color = new Color(startColor.r, startColor.g, startColor.b, newAlpha);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // --- 코루틴 종료 ---

        // 1. 텍스트 비활성화
        comboTMP.gameObject.SetActive(false);

        // 2. 위치 리셋은 더 이상 필요 없음 (다음 호출 시 새로 계산)
        // comboTMP.transform.localPosition = initialPosition; (X)

        runningComboCoroutine = null;
    }
}
