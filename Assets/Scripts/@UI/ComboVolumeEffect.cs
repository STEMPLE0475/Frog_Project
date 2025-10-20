using System.Collections;
using UnityEngine;
using UnityEngine.Rendering; // 볼륨 사용을 위해 필요
using UnityEngine.Rendering.Universal; // URP의 ColorAdjustments 사용 (HDRP라면 HDRP 네임스페이스)

public class ComboVolumeEffect : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("OnCombo 이벤트를 발생시키는 플레이어 컨트롤러")]
    [SerializeField] PlayerController playerController;

    [Header("효과 설정")]
    [Tooltip("어두워졌다가 밝아지는 애니메이션 커브. (0~1 사이 값)")]
    [SerializeField] AnimationCurve intensityCurve;

    [Tooltip("효과가 지속되는 총 시간 (초)")]
    [SerializeField] float effectDuration = 0.5f;

    [Tooltip("가장 어두워졌을 때의 Post Exposure 값")]
    [SerializeField] float minExposure = -2.0f;

    private Volume globalVolume;
    private ColorAdjustments colorAdjustments; // 제어할 포스트 프로세싱 효과
    private float originalExposure;            // 원래 노출(밝기) 값
    private Coroutine runningEffectCoroutine;  // 실행 중인 코루틴

    void Start()
    {
        // 1. 볼륨 컴포넌트 가져오기
        globalVolume = GetComponent<Volume>();

        // 2. 볼륨 프로필에서 ColorAdjustments 찾아오기
        if (globalVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            // 3. 원래 노출 값 저장
            originalExposure = colorAdjustments.postExposure.value;
        }
        else
        {
            Debug.LogError("Global Volume Profile에 ColorAdjustments가 없습니다!");
            return;
        }

        // 4. PlayerController의 OnCombo 이벤트 구독
        if (playerController != null)
        {
            playerController.OnCombo += TriggerEffect;
        }
    }

    void OnDestroy()
    {
        // 5. 오브젝트 파괴 시 이벤트 구독 해제 (메모리 누수 방지)
        if (playerController != null)
        {
            playerController.OnCombo -= TriggerEffect;
        }

        // (안전장치) 만약 꺼졌을 때 원래 값으로 복구
        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = originalExposure;
        }
    }

    /// <summary>
    /// OnCombo 이벤트가 호출될 때 실행되는 함수
    /// </summary>
    private void TriggerEffect(int combo)
    {
        if (combo == 0) return;
        // 콤보 수(combo)를 사용해서 효과를 다르게 줄 수도 있습니다.
        // 예: if (combo < 5) return;

        // 이미 실행 중인 효과가 있다면 중지
        if (runningEffectCoroutine != null)
        {
            StopCoroutine(runningEffectCoroutine);
        }

        // 새로운 효과 코루틴 시작
        runningEffectCoroutine = StartCoroutine(PlayEffect());
    }

    /// <summary>
    /// 실제로 화면을 어둡게 했다가 밝게 하는 코루틴
    /// </summary>
    private IEnumerator PlayEffect()
    {
        float elapsedTime = 0f;

        while (elapsedTime < effectDuration)
        {
            elapsedTime += Time.deltaTime;

            // 1. 경과 시간을 0~1 사이의 비율(t)로 변환
            float t = elapsedTime / effectDuration;

            // 2. AnimationCurve에서 현재 시점의 강도(0~1)를 가져옴
            float curveValue = intensityCurve.Evaluate(t);

            // 3. Post Exposure 값을 (원래 값)과 (최소 값) 사이에서 보간
            colorAdjustments.postExposure.value = Mathf.Lerp(originalExposure, minExposure, curveValue);

            yield return null; // 다음 프레임까지 대기
        }

        // 4. 효과가 끝나면 정확히 원래 값으로 복구
        colorAdjustments.postExposure.value = originalExposure;
        runningEffectCoroutine = null;
    }
}