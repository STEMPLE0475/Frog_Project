using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    private Transform playerTransform;
    private CinemachineImpulseSource impulseSource;
    Coroutine zoomCoroutine;

    [Header("카메라 오브젝트")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private Camera brightEffectCamera;

    [Header("환경 변수")]
    [SerializeField] private float defaultFOV = 5f;
    [SerializeField] private float zoomEndFOV = 4.7f;
    [SerializeField] private float zoomDuration = 0.2f; // 줌 인/아웃에 걸리는 시간
    [SerializeField] private float deathZoomFov = 4f;
    [SerializeField] private float deathZoomDuration = 3f;
    [SerializeField] private float baseIntensityPerLevel = 0.1f; //흔들림 강도 계수

    public void Initiate(Transform playerTransform)
    {
        this.playerTransform = playerTransform;
        impulseSource = cinemachineCamera.GetComponent<CinemachineImpulseSource>();
        cinemachineCamera = cinemachineCamera.GetComponent<CinemachineCamera>();
        FollowPlayer();
        SetAllCameraFOV(defaultFOV);
    }

    public void FollowPlayer()
    {
        cinemachineCamera.Follow = playerTransform;
    }

    public void OnFollowStart(Transform targetTransform)
    {
        cinemachineCamera.Follow = targetTransform;
    }

    public void OnZoomStart(float jumpDuration)
    {
        if (zoomCoroutine != null)
        {
            StopCoroutine(zoomCoroutine);
        }
        zoomCoroutine = StartCoroutine(SmoothZoom(jumpDuration));
    }

    private IEnumerator SmoothZoom(float jumpDuration)
    {
        float startFOV = defaultFOV;

        //줌 인
        float zoomInTime = 0f;
        while (zoomInTime < zoomDuration)
        {
            // Lerp를 사용하여 시작 값에서 목표 값으로 부드럽게 보간
            float t = zoomInTime / zoomDuration;
            float newFOV = Mathf.Lerp(defaultFOV, zoomEndFOV, t);

            // Cinemachine 카메라의 FOV(렌즈) 값 업데이트
            SetAllCameraFOV(newFOV);

            zoomInTime += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        // 정확한 목표 값으로 최종 설정
        var lensAfterZoomIn = cinemachineCamera.Lens;
        lensAfterZoomIn.OrthographicSize = zoomEndFOV;
        cinemachineCamera.Lens = lensAfterZoomIn;


        // 2. 대기 (Hold / Wait)
        // 점프 지속 시간 - 줌 인/아웃 시간을 뺀 나머지 시간 동안 대기
        float waitTime = jumpDuration - (2 * zoomDuration);
        if (waitTime > 0)
        {
            yield return new WaitForSeconds(waitTime);
        }

        // 3. 줌 아웃 (Zoom Out)
        float zoomOutTime = 0f;
        while (zoomOutTime < zoomDuration)
        {
            float t = zoomOutTime / zoomDuration;
            float newFOV = Mathf.Lerp(zoomEndFOV, defaultFOV, t);

            SetAllCameraFOV(newFOV);

            zoomOutTime += Time.deltaTime;
            yield return null;
        }

        // 줌 아웃 완료: 원래의 시작 FOV로 정확히 설정
        var finalLens = cinemachineCamera.Lens;
        SetAllCameraFOV(defaultFOV);
        cinemachineCamera.Lens = finalLens;

        zoomCoroutine = null;
    }

    public void ShakeCamera(int combo)
    {
        // 1. Level을 float 강도 계수로 변환
        // 예: Level 8 * 0.1f = 최종 강도 0.8f
        if (combo == 0) return;
        float finalIntensity = combo * baseIntensityPerLevel;

        // 2. 최종 강도 값이 너무 낮거나 높지 않도록 Clamp (선택 사항)
        // finalIntensity = Mathf.Clamp(finalIntensity, 0f, 1.5f); 

        // 3. GenerateImpulse()에 계산된 강도(Velocity)를 전달하여 스케일 조절
        impulseSource.GenerateImpulse(finalIntensity);
    }

    public void DeathZoomStart() => zoomCoroutine = StartCoroutine(DeathZoomCoroutine());
    private IEnumerator DeathZoomCoroutine()
    {
        float elapsedTime = 0f;
        while (elapsedTime < deathZoomDuration)
        {
            float t = elapsedTime / deathZoomDuration;
            SetAllCameraFOV(Mathf.Lerp(defaultFOV, deathZoomFov, t));

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        SetAllCameraFOV(deathZoomFov);
        zoomCoroutine = null;
    }

    public void ResetCamera()
    {
        FollowPlayer();
        if (zoomCoroutine != null)
        {
            StopCoroutine(zoomCoroutine);
        }
        SetAllCameraFOV(defaultFOV);
    }

    //유틸리티
    private void SetAllCameraFOV(float targetFOV)
    {
        cinemachineCamera.Lens.OrthographicSize = targetFOV;
        brightEffectCamera.orthographicSize = targetFOV;
    }
}
