using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CinemachineCameraScript : MonoBehaviour
{
    private CinemachineCamera cam;
    private PlayerController playerController;

    private float zoomChangeAmount = 1f; // FOV를 줄일 양 (현재 값에서 -1)
    private float zoomDefaultFOV = 5f;
    private float zoomEndFOV = 4.7f;
    private float zoomDuration = 0.5f; // 줌 인/아웃에 걸리는 시간

    public void Initiate(PlayerController playerController)
    {
        this.playerController = playerController;
        //카메라가 player을 Follow하도록 합니다.
        cam = GetComponent<CinemachineCamera>();
        cam.Follow = playerController.transform;
        cam.Lens.OrthographicSize = zoomDefaultFOV;
        BindEvents();
    }

    void BindEvents()
    {
        playerController.OnJumpStart += OnZoomStart;
    }

    private void OnDestroy()
    {
        if (playerController != null)
        {
            playerController.OnJumpStart -= OnZoomStart;
        }
    }

    public void OnZoomStart()
    {
        float targetFOV = cam.Lens.OrthographicSize - zoomChangeAmount;
        StartCoroutine(SmoothZoom(zoomEndFOV));
    }

    private IEnumerator SmoothZoom(float targetFOV)
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
        lensAfterZoomIn.OrthographicSize = targetFOV;
        cam.Lens = lensAfterZoomIn;


        // 2. 대기 (Hold / Wait)
        // 점프 지속 시간 - 줌 인/아웃 시간을 뺀 나머지 시간 동안 대기
        float waitTime = playerController.jumpDuration - (2 * zoomDuration);
        if (waitTime > 0)
        {
            float holdTime = 0f;
            while (holdTime < waitTime)
            {
                holdTime += Time.deltaTime;
                yield return null;
            }
        }

        // 3. 줌 아웃 (Zoom Out)
        float zoomOutTime = 0f;
        while (zoomOutTime < zoomDuration)
        {
            // 0부터 1까지 진행되는 비율: zoomOutTime / zoomDuration
            float t = zoomOutTime / zoomDuration;
            // targetFOV(줌 엔드)에서 zoomDefaultFOV(시작 값)으로 보간
            float newFOV = Mathf.Lerp(targetFOV, zoomDefaultFOV, t);

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
    }
}
