using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WindChangeEffect : MonoBehaviour
{
    [SerializeField] private List<Sprite> windEffectSprites;
    private Image windEffectImage;

    [SerializeField] private float windEffectChangeDelay_three = 0.3f;
    [SerializeField] private float windEffectChangeDelay_two = 0.7f;
    [SerializeField] private float windEffectChangeDelay_one = 1.2f;

    private Coroutine windChangeCoroutine;
    private float currentDelay;

    public void Initiate()
    {
        windEffectImage = GetComponent<Image>();
        if (windEffectImage != null)
        {
            windEffectImage.enabled = false;
            windEffectImage.sprite = null;
        }
    }

    // GameManager가 호출해줄 함수
    public void StartWindChangeEffect(Wind wind)
    {
        // 1. 기존 코루틴 중지
        if (windChangeCoroutine != null) StopCoroutine(windChangeCoroutine);

        // 2. 바람 세기에 따라 딜레이 설정
        switch (wind.power)
        {
            case 0:
                currentDelay = 0f;
                // 바람이 0이면 이미지 비활성화
                if (windEffectImage != null)
                {
                    windEffectImage.enabled = false;
                }
                return; // 애니메이션 코루틴 시작 방지

            case 1:
                currentDelay = windEffectChangeDelay_one;
                break;
            case 2:
                currentDelay = windEffectChangeDelay_two;
                break;
            case 3:
                currentDelay = windEffectChangeDelay_three;
                break;
            default:
                // 정의되지 않은 세기의 바람이 들어왔을 경우
                Debug.LogWarning($"정의되지 않은 바람 세기({wind.power}). 약한 바람 딜레이 적용.");
                currentDelay = windEffectChangeDelay_one;
                break;
        }

        // 3. 바람 애니메이션 코루틴 시작
        windChangeCoroutine = StartCoroutine(AnimateWindEffect());
    }

    private IEnumerator AnimateWindEffect()
    {
        // 이미지 컴포넌트와 스프라이트 리스트 유효성 검사
        if (windEffectImage == null || windEffectSprites == null || windEffectSprites.Count == 0)
        {
            Debug.LogWarning("Wind Effect Image 또는 Sprites가 설정되지 않았습니다. 애니메이션을 실행할 수 없습니다.");
            windChangeCoroutine = null;
            yield break;
        }

        windEffectImage.enabled = true;

        int currentIndex = 0;

        while (true) // 바람이 멈출 때까지 무한 순환
        {
            // 1. 현재 순서의 스프라이트를 Image 컴포넌트에 할당 (교체)
            windEffectImage.sprite = windEffectSprites[currentIndex];

            // 2. 다음 순서로 인덱스 업데이트 (순환)
            currentIndex = (currentIndex + 1) % windEffectSprites.Count;

            // 3. 설정된 딜레이만큼 대기
            yield return new WaitForSeconds(currentDelay);
        }
    }
}
