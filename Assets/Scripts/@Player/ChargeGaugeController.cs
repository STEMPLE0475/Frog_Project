using UnityEngine;
using UnityEngine.UI;

public class ChargeGaugeController : MonoBehaviour
{
    [SerializeField] private Image chargeGaugeImage; // 체력바처럼 'Filled' 타입 이미지

    // 0. 초기화 (GameManager가 호출)
    public void Initiate()
    {
        if (chargeGaugeImage == null)
            chargeGaugeImage = GetComponent<Image>();

        // 시작 시 게이지 숨기기
        chargeGaugeImage.fillAmount = 0;
        gameObject.SetActive(false);
    }

    // 1. OnChargeStarted 이벤트가 호출할 함수
    public void HandleChargeStarted()
    {
        chargeGaugeImage.fillAmount = 0;
        gameObject.SetActive(true);
    }

    // 2. OnChargeUpdated 이벤트가 호출할 함수
    public void HandleChargeUpdated(float normalizedValue)
    {
        // 0~1 사이의 정규화된 값을 받아서 그대로 fillAmount에 적용
        chargeGaugeImage.fillAmount = normalizedValue;
    }

    // 3. OnChargeStopped 이벤트가 호출할 함수
    public void HandleChargeStopped()
    {
        chargeGaugeImage.fillAmount = 0;
        gameObject.SetActive(false);
    }
}