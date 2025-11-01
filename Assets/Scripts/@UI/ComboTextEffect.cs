using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Image 사용을 위해 추가

public class ComboTextEffect : MonoBehaviour
{
    [Header("연결 요소")]
    [SerializeField] Image comboImage;
    [SerializeField] List<Sprite> comboSprites;

    [Header("애니메이션 설정")]
    [SerializeField] float animationDuration = 0.5f; 
    [SerializeField] float initialScale = 0.5f; 
    [SerializeField] float peakScale = 1.5f; 
    [SerializeField] float fadeOutDelay = 0.1f;

    [SerializeField] Vector3 screenSpaceOffset = new Vector3(0, 0, 0);

    [Header("이미지 표시 위치")]
    [SerializeField] RectTransform targetRectTransform;

    private Camera mainCamera;
    private Coroutine runningComboCoroutine;
    private RectTransform comboRectTransform; 

    public void Initiate(Camera mainCamera)
    {
        this.mainCamera = mainCamera;
        
        if (comboImage != null)
        {
            comboRectTransform = comboImage.GetComponent<RectTransform>();
            
            // 앵커(Pivot)는 요청대로 우측 하단(1, 0)으로 설정
            comboRectTransform.pivot = new Vector2(1, 0);

            comboImage.gameObject.SetActive(false);
        }
    }

    // GameManager가 PlayerController.OnCombo 이벤트를 받아서 호출해줄 함수
    public void Show(int combo, Vector3 worldPosition)
    {
        if (combo == 0) return;
        
        if (comboRectTransform == null)
        {
            Debug.LogWarning("Combo Image의 RectTransform이 설정되지 않았습니다");
            return;
        }
        
        if (comboSprites.Count == 0)
        {
            Debug.LogWarning("Combo Sprites가 설정되지 않았습니다. 효과를 표시할 수 없습니다.");
            return;
        }

        // 콤보 수에 따라 적절한 Sprite를 선택
        int spriteIndex = Mathf.Clamp(combo - 1, 0, comboSprites.Count - 1);
        comboImage.sprite = comboSprites[spriteIndex];

        // [핵심 수정]
        // SetNativeSize()를 호출하지 않습니다
        // 대신 에디터에서 'Preserve Aspect'를 켜주세요
        // comboImage.SetNativeSize(); // <-- 이 줄 삭제

        // 이미지 위치 설정 (고정 위치 복사)
        if (targetRectTransform != null)
        {
            comboRectTransform.position = targetRectTransform.position;
            comboRectTransform.position += screenSpaceOffset;
        }

        // 기존 코루틴이 있다면 중지
        if (runningComboCoroutine != null) StopCoroutine(runningComboCoroutine);

        // 이미지 초기 설정
        comboImage.color = new Color(comboImage.color.r, comboImage.color.g, comboImage.color.b, 1f); 
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
            float scaleT = 1f - Mathf.Pow(1f - t, 2);

            float currentScale = Mathf.Lerp(initialScale, peakScale, scaleT);
            
            comboRectTransform.localScale = Vector3.one * currentScale;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 애니메이션 완료 후 최종 크기 설정
        comboRectTransform.localScale = Vector3.one * peakScale;

        // 2. 투명화 딜레이
        if (fadeOutDelay > 0)
        {
            yield return new WaitForSeconds(fadeOutDelay);
        }

        // 3. 투명화 애니메이션 (사라지기)
        elapsedTime = 0f;
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
        comboRectTransform.localScale = Vector3.one * initialScale;
        runningComboCoroutine = null;
    }
}

