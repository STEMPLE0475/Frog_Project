using System.Collections;
using TMPro;
using UnityEngine;

public class ComboTextEffect : MonoBehaviour
{
    [Header("연결 요소")]
    [SerializeField] TextMeshProUGUI comboTMP;

    [Header("애니메이션 설정")]
    [SerializeField] float animationDuration = 1.5f;
    [SerializeField] float moveDistanceUp = 150;
    [SerializeField] Vector3 worldSpaceOffset = new Vector3(0, 2f, 0);
    [SerializeField] Vector3 screenSpaceOffset = new Vector3(50f, 0, 0);

    private Camera mainCamera;

    private Coroutine runningComboCoroutine;

    public void Initiate(Camera mainCamera)
    {
        this.mainCamera = mainCamera;
        // 콤보 텍스트는 처음엔 비활성화
        comboTMP.gameObject.SetActive(false);
    }

    // GameManager가 PlayerController.OnCombo 이벤트를 받아서 호출해줄 함수
    public void Show(int combo, Vector3 worldPosition)
    {
        if (combo == 0) return;
        if (runningComboCoroutine != null) StopCoroutine(runningComboCoroutine);

        comboTMP.text = "COMBO " + combo;

        Vector3 worldPos = worldPosition + worldSpaceOffset;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        comboTMP.transform.position = screenPos + screenSpaceOffset;

        Color startColor = comboTMP.color;
        comboTMP.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
        comboTMP.gameObject.SetActive(true);

        runningComboCoroutine = StartCoroutine(ShowComboEffect());
    }

    private IEnumerator ShowComboEffect()
    {
        // (기존 CanvasManager의 ShowComboEffect 코루틴과 동일)
        // ... (생략) ...
        float elapsedTime = 0f;
        Vector3 startPosition = comboTMP.transform.position;
        Vector3 targetPosition = startPosition + Vector3.up * moveDistanceUp;
        Color startColor = comboTMP.color;

        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration;
            comboTMP.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            float newAlpha = Mathf.Lerp(1f, 0f, t);
            comboTMP.color = new Color(startColor.r, startColor.g, startColor.b, newAlpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        comboTMP.gameObject.SetActive(false);
        runningComboCoroutine = null;
    }
}