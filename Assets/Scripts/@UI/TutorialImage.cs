using System.Collections;
using UnityEngine;

public class TutorialImage : MonoBehaviour
{
    [SerializeField] private GameObject SpaceOffImage;
    [SerializeField] private GameObject SpaceOnImage;
    [SerializeField] private float showPeriod = 3f;
    [SerializeField] private float blinkPeriod = 0.4f;
    private bool isActive = false;
    private Coroutine blinkCoroutine;

    public void StartBlink()
    {
        if(blinkCoroutine != null ) { StopCoroutine(blinkCoroutine); }

        isActive = true;
        SpaceOffImage.SetActive(true);
        SpaceOnImage.SetActive(false);

        StartCoroutine(BlinkRepeat());
    }

    IEnumerator BlinkRepeat()
    {
        float startTime = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup < startTime + showPeriod)
        {
            yield return new WaitForSecondsRealtime(blinkPeriod);

            // 이미지 상태를 반전시켜 깜빡이게 함
            SpaceOffImage.SetActive(!SpaceOffImage.activeSelf);
            SpaceOnImage.SetActive(!SpaceOnImage.activeSelf);
        }

        isActive = false;
        SpaceOffImage.SetActive(false);
        SpaceOnImage.SetActive(false);
    }

}
