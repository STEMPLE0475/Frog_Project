using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Image 사용을 위해 추가

public class ComboTextEffect : MonoBehaviour
{
    [Header("연결 요소")]
    // 콤보 이미지가 표시될 UI Image 컴포넌트
    [SerializeField] Image comboImage;
    // 콤보 이미지로 사용할 Sprite 리스트 (List<Image> 대신 List<Sprite>를 사용해야 합니다)
    [SerializeField] List<Sprite> comboSprites;

    [Header("애니메이션 설정")]
    [SerializeField] float animationDuration = 0.5f; // 애니메이션 지속 시간 (짧게 설정)
    [SerializeField] float initialScale = 0.5f; // 시작 시 스케일 (최소 크기)
    [SerializeField] float peakScale = 1.5f; // 가장 커졌을 때의 스케일 (펑 터지는 효과)
    [SerializeField] float fadeOutDelay = 0.1f; // 커지는 애니메이션 후 투명화 시작까지의 딜레이

    [SerializeField] Vector3 screenSpaceOffset = new Vector3(0, 0, 0);

    [Header("이미지 표시 위치")]
    [SerializeField] RectTransform targetRectTransform;

    private Camera mainCamera;
    private Coroutine runningComboCoroutine;

    public void Initiate(Camera mainCamera)
    {
        this.mainCamera = mainCamera;
        // 콤보 이미지는 처음엔 비활성화
        comboImage.gameObject.SetActive(false);
    }

    // GameManager가 PlayerController.OnCombo 이벤트를 받아서 호출해줄 함수
    public void Show(int combo, Vector3 worldPosition)
    {
        if (combo == 0) return;

        // 유효한 Sprite가 있는지 확인
        if (comboSprites.Count == 0)
        {
            Debug.LogWarning("Combo Sprites가 설정되지 않았습니다. 효과를 표시할 수 없습니다.");
            return;
        }

        // 콤보 수에 따라 적절한 Sprite를 선택합니다.
        int spriteIndex = Mathf.Clamp(combo - 1, 0, comboSprites.Count - 1);
        comboImage.sprite = comboSprites[spriteIndex];

        // 이미지 위치 설정 (고정 위치 복사)
        if (targetRectTransform != null)
        {
            // comboImage의 위치를 targetRectTransform의 현재 위치로 설정합니다.
            // Canvas 종류에 따라 position 또는 anchoredPosition을 사용해야 할 수 있지만, 
            // 여기서는 가장 일반적인 position(월드 좌표)을 사용합니다.
            comboImage.transform.position = targetRectTransform.position;

            // 고정 위치에서 추가적인 화면 좌표 오프셋을 적용합니다.
            comboImage.transform.position += screenSpaceOffset;
        }

        // 기존 코루틴이 있다면 중지
        if (runningComboCoroutine != null) StopCoroutine(runningComboCoroutine);

        // 이미지 초기 설정
        comboImage.color = new Color(comboImage.color.r, comboImage.color.g, comboImage.color.b, 1f); // 투명도 1로 설정
        comboImage.gameObject.SetActive(true);

        runningComboCoroutine = StartCoroutine(ShowComboEffect());
    }

    private IEnumerator ShowComboEffect()
    {
        float elapsedTime = 0f;

        // 1. 크기 애니메이션 (최소 -> 최대 펑!)
        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration;
            // Ease-out 효과를 위해 t를 부드럽게 변환 (예: 2차 함수)
            float scaleT = 1f - Mathf.Pow(1f - t, 2);

            // 크기 보간
            float currentScale = Mathf.Lerp(initialScale, peakScale, scaleT);
            comboImage.transform.localScale = Vector3.one * currentScale;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 애니메이션 완료 후 최종 크기 설정
        comboImage.transform.localScale = Vector3.one * peakScale;

        // 2. 투명화 딜레이
        if (fadeOutDelay > 0)
        {
            yield return new WaitForSeconds(fadeOutDelay);
        }

        // 3. 투명화 애니메이션 (사라지기)
        elapsedTime = 0f;
        // 투명화 애니메이션 시간을 크기 애니메이션 시간과 다르게 설정 가능 (여기서는 동일하게 사용)
        float fadeDuration = animationDuration / 2f;
        Color startColor = comboImage.color;

        while (elapsedTime < fadeDuration)
        {
            float t = elapsedTime / fadeDuration;
            float newAlpha = Mathf.Lerp(1f, 0f, t);
            comboImage.color = new Color(startColor.r, startColor.g, startColor.b, newAlpha);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 4. 비활성화 및 정리
        comboImage.gameObject.SetActive(false);
        // 스케일 초기화 (다음 번 사용을 위해)
        comboImage.transform.localScale = Vector3.one * initialScale;
        runningComboCoroutine = null;
    }
}