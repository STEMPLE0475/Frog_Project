using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CinemachineCameraManager : MonoBehaviour
{
    private CinemachineCamera cam;
    private Transform playerTransform;

    private Coroutine runningZoomCoroutine;

    [SerializeField] private float zoomDefaultFOV = 5f;
    [SerializeField] private float zoomEndFOV = 4.7f;
    [SerializeField] private float zoomDuration = 0.2f; // 줌 인/아웃에 걸리는 시간

    public void Initiate(Transform playerTransform)
    {
        this.playerTransform = playerTransform;
        cam = GetComponent<CinemachineCamera>();
        OnFollowStart();
        cam.Lens.OrthographicSize = zoomDefaultFOV;
    }

    private void OnFollowStart() => cam.Follow = playerTransform;

    public void OnZoomStart(float jumpDuration)
    {
        if (runningZoomCoroutine != null)
        {
            StopCoroutine(runningZoomCoroutine);
        }
        StartCoroutine(SmoothZoom(jumpDuration));
    }

    private IEnumerator SmoothZoom(float jumpDuration)
    {
        float startFOV = cam.Lens.OrthographicSize;

        //줌 인
        float zoomInTime = 0f;
        while (zoomInTime < zoomDuration)
        {
            // Lerp를 사용하여 시작 값에서 목표 값으로 부드럽게 보간
            float t = zoomInTime / zoomDuration;
            float newFOV = Mathf.Lerp(zoomDefaultFOV, zoomEndFOV, t);

            // Cinemachine 카메라의 FOV(렌즈) 값 업데이트
            var lens = cam.Lens;
            lens.OrthographicSize = newFOV;
            cam.Lens = lens;

            zoomInTime += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        // 정확한 목표 값으로 최종 설정
        var lensAfterZoomIn = cam.Lens;
        lensAfterZoomIn.OrthographicSize = zoomEndFOV;
        cam.Lens = lensAfterZoomIn;


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
            float newFOV = Mathf.Lerp(zoomEndFOV, zoomDefaultFOV, t);

            var lens = cam.Lens;
            lens.OrthographicSize = newFOV;
            cam.Lens = lens;

            zoomOutTime += Time.deltaTime;
            yield return null;
        }

        // 줌 아웃 완료: 원래의 시작 FOV로 정확히 설정
        var finalLens = cam.Lens;
        finalLens.OrthographicSize = zoomDefaultFOV; // 원래 기본값으로 돌아감
        cam.Lens = finalLens;

        runningZoomCoroutine = null;
    }
}
