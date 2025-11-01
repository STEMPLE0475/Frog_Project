using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WindowComboEffect : MonoBehaviour
{
    // [복구] 스프라이트 리스트 (인스펙터에 2개 할당 필요)
    [SerializeField] private List<Sprite> comboEffectSprites;

    private Image effectImage;

    [Header("콤보 이펙트 설정")]
    [SerializeField] private float effectDuration = 1.0f;  // 1. 총 재생 시간
    [SerializeField] private float maxBlinkDelay = 1.6f; // 2. 최대 딜레이 (콤보 1일때)
    [SerializeField] private float minBlinkDelay = 0.3f; // 3. 최소 딜레이 (콤보 10일때)
    [SerializeField] private int maxComboForCalc = 10;   // 4. 최대 콤보 (이 이상은 10으로 취급)

    private Coroutine spriteSwapCoroutine;

    public void Initiate()
    {
        effectImage = GetComponent<Image>();
        if (effectImage != null)
        {
            effectImage.enabled = false;
        }
    }

    // [수정] GameManager가 호출해줄 함수
    public void StartComboEffect(int combo)
    {
        // 1. 콤보 0이면 무시 (이펙트 중지)
        if (combo <= 0)
        {
            StopEffect();
            return;
        }

        // 2. 스프라이트 유효성 검사
        if (comboEffectSprites == null || comboEffectSprites.Count < 2)
        {
            Debug.LogWarning("Combo Effect Sprites가 2개 미만입니다. 애니메이션을 실행할 수 없습니다.");
            return;
        }

        // 3. 기존 코루틴 중지
        if (spriteSwapCoroutine != null) StopCoroutine(spriteSwapCoroutine);

        // 4. 콤보 값 제한 (1 ~ 10)
        int clampedCombo = Mathf.Clamp(combo, 1, maxComboForCalc);

        // 5. 딜레이 계산
        float normalizedCombo;
        if (maxComboForCalc <= 1)
            normalizedCombo = 1.0f;
        else
            normalizedCombo = (float)(clampedCombo - 1) / (maxComboForCalc - 1);

        float currentDelay = Mathf.Lerp(maxBlinkDelay, minBlinkDelay, normalizedCombo);

        // 6. 스프라이트 교체 코루틴 시작
        spriteSwapCoroutine = StartCoroutine(AnimateSpriteSwap(currentDelay));
    }

    // 이펙트 강제 중지
    public void StopEffect()
    {
        if (spriteSwapCoroutine != null) StopCoroutine(spriteSwapCoroutine);
        spriteSwapCoroutine = null;
        if (effectImage != null)
        {
            effectImage.enabled = false;
        }
    }

    // [수정] 스프라이트 교체(깜빡임) 코루틴
    private IEnumerator AnimateSpriteSwap(float delay)
    {
        if (effectImage == null)
        {
            spriteSwapCoroutine = null;
            yield break;
        }

        // 2초 뒤 멈출 시간 계산
        float stopTime = Time.time + effectDuration;

        effectImage.enabled = true;
        int spriteIndex = 0;

        // 2초가 다 될 때까지 루프
        while (Time.time < stopTime)
        {
            // 1. 스프라이트 교체 (0번 -> 1번 -> 0번 ...)
            effectImage.sprite = comboEffectSprites[spriteIndex];
            spriteIndex = (spriteIndex + 1) % 2; // 0과 1만 반복

            // 2. 계산된 딜레이만큼 대기
            yield return new WaitForSeconds(delay);
        }

        // 3. 2초가 지나면 코루틴 종료 및 이미지 비활성화
        effectImage.enabled = false;
        spriteSwapCoroutine = null;
    }
}