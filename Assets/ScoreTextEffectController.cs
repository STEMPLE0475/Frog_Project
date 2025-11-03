using TMPro;
using UnityEngine;
using System.Collections; // 코루틴을 위해 추가

// TextMeshProUGUI는 RectTransform이 필수입니다.
[RequireComponent(typeof(RectTransform))]
public class ScoreTextEffectController : MonoBehaviour
{
    private TextMeshProUGUI tmp;
    private RectTransform rectTransform; // UI 위치 제어를 위해 필요
    private Transform playerTransform;
    private Camera mainCam;

    // 인스펙터에서 애니메이션 시간 조절
    [Header("애니메이션 설정")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 2f, 0); // 플레이어 머리 위 오프셋
    [SerializeField] private float scaleUpDuration = 0.1f;  // 커지는 시간
    [SerializeField] private float holdDuration = 0.5f;     // 유지 시간
    [SerializeField] private float scaleDownDuration = 0.1f; // 작아지는 시간

    private Coroutine animationCoroutine; // 실행 중인 코루틴을 저장

    public void Initiate(Transform playerTransform)
    {
        tmp = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        this.playerTransform = playerTransform;
        this.mainCam = Camera.main; // 메인 카메라 캐싱

        gameObject.SetActive(false); // 시작할 땐 꺼둠
    }

    /// <summary>
    /// 점수 텍스트 애니메이션을 재생합니다.
    /// </summary>
    /// <param name="score">표시할 점수</param>
    public void Show(int score)
    {
        // 플레이어 정보가 없으면 실행 중지
        if (playerTransform == null || mainCam == null)
        {
            Debug.LogWarning("PlayerTransform 또는 MainCamera가 설정되지 않았습니다.");
            return;
        }

        // 1. (중요) 이전에 실행 중이던 애니메이션이 있다면 중지
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        // 2. 텍스트 설정 (ex: "+100", "-50", "0")
        tmp.text = score.ToString("+#;-#;0");

        // 2-1. 점수에 따라 색상 변경 (요청 사항)
        if (score >= 1000)
        {
            tmp.color = Color.blue; // 1000점 이상: 빨간색
        }
        else if (score >= 500)
        {
            tmp.color = Color.red; // 500점 이상: 주황색
        }
        else if (score >= 100)
        {
            tmp.color = Color.yellow; // 100점 이상: 노랑색
        }
        else
        {
            tmp.color = Color.white; // 그 외: 흰색 (기본값)
        }

        // 3. 위치 설정 (월드 좌표 -> 스크린 좌표)
        Vector3 worldPos = playerTransform.position + worldOffset;
        Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

        // UI(RectTransform)의 위치(position)를 스크린 좌표로 설정
        rectTransform.position = screenPos;

        // 4. 활성화 및 코루틴 시작
        gameObject.SetActive(true);
        animationCoroutine = StartCoroutine(AnimateScale());
    }

    /// <summary>
    /// "뿅" 하고 나타났다가 사라지는 스케일 애니메이션 코루틴
    /// </summary>
    private IEnumerator AnimateScale()
    {
        float elapsedTime = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one;

        // --- 1. 커지기 (0.1초) ---
        while (elapsedTime < scaleUpDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / scaleUpDuration);

            // Lerp를 사용해 부드럽게 스케일 업
            rectTransform.localScale = Vector3.Lerp(startScale, endScale, progress);
            yield return null;
        }
        rectTransform.localScale = endScale; // 정확히 1로 맞춤

        // --- 2. 유지 (0.5초) ---
        yield return new WaitForSeconds(holdDuration);

        // --- 3. 작아지기 (0.1초) ---
        elapsedTime = 0f; // 타이머 리셋
                          // startScale은 이미 endScale (Vector3.one) 상태임

        while (elapsedTime < scaleDownDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / scaleDownDuration);

            // Lerp를 사용해 부드럽게 스케일 다운
            rectTransform.localScale = Vector3.Lerp(endScale, startScale, progress);
            yield return null;
        }
        rectTransform.localScale = startScale; // 정확히 0으로 맞춤

        // --- 4. 비활성화 ---
        gameObject.SetActive(false);
        animationCoroutine = null; // 코루틴 완료
    }
}