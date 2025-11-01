using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WindowComboEffect : MonoBehaviour
{
    private Image effectImage;

    [Header("콤보 이펙트 설정")]
    [SerializeField] private float visibleDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private Coroutine effectCoroutine;

    // [신규] 인스펙터에서 설정한 초기 알파 값을 저장할 변수
    private float initialAlpha = 1.0f;

    public void Initiate()
    {
        effectImage = GetComponent<Image>();
        if (effectImage != null)
        {
            // [수정] 1. (비활성화 전) 초기 알파 값 저장
            initialAlpha = effectImage.color.a;

            // [수정] 2. 시작 시 투명하게 만들고 비활성화
            Color c = effectImage.color;
            c.a = 0f;
            effectImage.color = c;
            effectImage.enabled = false;
        }
    }

    // [수정] GameManager가 호출해줄 함수
    public void StartComboEffect(int combo)
    {
        // 1. 콤보 0 이하면 무시
        if (combo <= 0)
        {
            return;
        }

        // 2. 스프라이트 유효성 검사 (Image 컴포넌트만)
        if (effectImage == null)
        {
            Debug.LogWarning("Combo Effect Image가 없습니다.");
            return;
        }

        // 3. 기존 코루틴 중지 (새 콤보가 들어오면 이전 이펙트 중단)
        if (effectCoroutine != null) StopCoroutine(effectCoroutine);

        // 4. 새 코루틴 시작
        effectCoroutine = StartCoroutine(ShowAndFadeOut());
    }

    // 이펙트 강제 중지
    public void StopEffect()
    {
        if (effectCoroutine != null) StopCoroutine(effectCoroutine);
        effectCoroutine = null;
        if (effectImage != null)
        {
            effectImage.enabled = false;
        }
    }

    // [수정] "즉시 보이기 -> 대기 -> 페이드아웃" 코루틴
    private IEnumerator ShowAndFadeOut()
    {
        // 1. 즉시 활성화 및 [저장해둔 초기 알파 값]으로 설정
        effectImage.enabled = true;
        Color color = effectImage.color;
        color.a = initialAlpha; // 1.0f 대신 initialAlpha 사용
        effectImage.color = color;

        // 2. 'delay' (visibleDuration) 시간만큼 대기
        yield return new WaitForSeconds(visibleDuration);

        // 3. 'fadeoutdelay' (fadeOutDuration) 시간만큼 서서히 투명하게
        float timer = 0f;

        // [수정] 사라지기 시작할 때의 알파 값도 initialAlpha로
        float startAlpha = initialAlpha;

        while (timer < fadeOutDuration)
        {
            // 알파 값을 [initialAlpha]에서 0으로 서서히 변경
            color.a = Mathf.Lerp(startAlpha, 0f, timer / fadeOutDuration);
            effectImage.color = color;

            timer += Time.deltaTime;
            yield return null;
        }

        // 4. 완료 후 확실하게 알파 0 및 비활성화
        color.a = 0f;
        effectImage.color = color;
        effectImage.enabled = false;
        effectCoroutine = null;
    }
}