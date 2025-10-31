using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GlobalVolumeController : MonoBehaviour
{
    private Volume globalVolume;
    private ColorAdjustments colorAdjustments; // 제어할 포스트 프로세싱 효과
    private float originalExposure;            // 원래 노출(밝기) 값
    private Coroutine runningEffectCoroutine;  // 실행 중인 코루틴

    [Header("효과 설정")]
    [SerializeField] AnimationCurve intensityCurve;

    [Header("환경 변수")]
    [SerializeField] float effectDuration = 0.5f; // 어두움 효과가 지속되는 시간


    [Tooltip("콤보 1일 때의 기본 어두움 정도")]
    [SerializeField] float baseMinExposure = -1.5f;

    [Tooltip("최대 콤보일 때의 가장 어두운 정도")]
    [SerializeField] float maxMinExposure = -4.5f; // (기존 minExposure에서 이름 변경)

    [Tooltip("이 콤보 수치에서 최대 밝기 효과(maxMinExposure)가 적용됩니다.")]
    [SerializeField] int maxComboForEffect = 10;

    public void Initiate()
    {
        globalVolume = GetComponent<Volume>();
        if (globalVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            originalExposure = colorAdjustments.postExposure.value;
        }
        else
        {
            Debug.LogError("Global Volume Profile에 ColorAdjustments가 없습니다!");
            return;
        }
    }

    /// OnCombo 이벤트가 호출될 때 실행되는 함수
    public void ComboFadeInOut(int combo)
    {
        if (combo == 0) return;

        // (2. 콤보 비율 계산 (0.0 ~ 1.0))
        // 콤보 1일 때 0% (0 / 9 = 0)
        // 콤보 10일 때 100% (9 / 9 = 1)
        float denominator = Mathf.Max(1.0f, maxComboForEffect - 1);
        float comboIntensity = (float)(combo - 1) / denominator;
        comboIntensity = Mathf.Clamp01(comboIntensity); // 1.0을 넘지 않도록

        // (3. 목표 밝기를 (기본 어두움)과 (최대 어두움) 사이에서 보간)
        // comboIntensity가 0이면 baseMinExposure (1콤보)
        // comboIntensity가 1이면 maxMinExposure (10콤보)
        float targetExposure = Mathf.Lerp(baseMinExposure, maxMinExposure, comboIntensity);

        // (4. 시작 밝기는 현재 밝기)
        float startExposure = colorAdjustments.postExposure.value;

        if (runningEffectCoroutine != null)
        {
            StopCoroutine(runningEffectCoroutine);
        }

        // (5. 계산된 '시작 밝기'와 '목표 밝기'를 코루틴에 전달)
        runningEffectCoroutine = StartCoroutine(ComboFadeInAndOutCoroutine(startExposure, targetExposure));
    }

    private IEnumerator ComboFadeInAndOutCoroutine(float startExposure, float targetExposure)
    {
        float elapsedTime = 0f;

        while (elapsedTime < effectDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / effectDuration;
            float curveValue = intensityCurve.Evaluate(t);

            // 전달받은 startExposure와 targetExposure 사이를 보간
            colorAdjustments.postExposure.value = Mathf.Lerp(startExposure, targetExposure, curveValue);

            yield return null;
        }

        // 효과가 끝나면 원래 값으로 복구
        colorAdjustments.postExposure.value = originalExposure;
        runningEffectCoroutine = null;
    }

    void OnDestroy()
    {
        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = originalExposure;
        }
    }
}