using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class DirectionalPanelAnimation : MonoBehaviour
{
    // --- 인스펙터에서 설정할 변수들 (변경 없음) ---
    [Tooltip("연출 이미지가 화면을 채우는 데 걸리는 시간")]
    [SerializeField] private float slideInDuration = 0.2f;

    [Tooltip("연출 이미지가 화면에 머무는 시간")]
    [SerializeField] private float holdDuration = 1.0f;

    [Tooltip("연출 이미지가 사라지는 데 걸리는 시간")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Tooltip("최대 투명도")]
    [SerializeField] private float maxOpacity = 0.9f;

    // --- 내부에서 사용할 변수들 (변경 없음) ---
    private RectTransform panelRectTransform;
    private CanvasGroup panelCanvasGroup;
    private Coroutine currentAnimationCoroutine;

    private void Awake()
    {
        // 필요한 컴포넌트들을 미리 찾아 변수에 할당해 둡니다.
        panelRectTransform = GetComponent<RectTransform>();
        panelCanvasGroup = GetComponent<CanvasGroup>();

        // 시작할 때는 보이지 않도록 비활성화합니다. (이 부분은 그대로 둡니다)
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 연출 애니메이션을 시작하는 메인 함수입니다.
    /// </summary>
    public void StartDirectionalAnimation()
    {
        // 코루틴을 실행하기 전에 자기 자신을 먼저 활성화합니다.
        gameObject.SetActive(true);

        // 만약 이전에 실행 중인 애니메이션이 있다면 중지시킵니다.
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
        }
        // 새로운 애니메이션 코루틴을 시작하고 변수에 저장합니다.
        currentAnimationCoroutine = StartCoroutine(AnimatePanel());
    }

    /// <summary>
    /// 실제 애니메이션 처리를 담당하는 코루틴입니다.
    /// </summary>
    private IEnumerator AnimatePanel()
    {
        // 1. 초기 상태 설정
        panelCanvasGroup.alpha = maxOpacity;

        // 화면 오른쪽 밖에서 시작하도록 위치를 잡습니다.
        float panelWidth = panelRectTransform.rect.width;
        Vector2 startPosition = new Vector2(panelWidth, 0);
        Vector2 endPosition = Vector2.zero;

        // 2. 우측에서 화면 중앙으로 이동 (Slide In)
        float elapsedTime = 0f;
        while (elapsedTime < slideInDuration)
        {
            panelRectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, elapsedTime / slideInDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        panelRectTransform.anchoredPosition = endPosition;

        // 3. 지정된 시간만큼 대기 (Hold)
        yield return new WaitForSeconds(holdDuration);

        // 4. 서서히 투명해지며 사라지기 (Fade Out)
        elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            panelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        panelCanvasGroup.alpha = 0f;

        // 5. 애니메이션 종료 후 비활성화
        gameObject.SetActive(false);
    }
}

