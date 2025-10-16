using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CinemachineCameraScript : MonoBehaviour
{
    private CinemachineCamera cam;
    private PlayerController playerController;

    private float zoomChangeAmount = 1f; // FOV를 줄일 양 (현재 값에서 -1)
    private float zoomFOV = 5f;
    private float zoomEndFOV = 4.7f;
    private float zoomDuration = 0.5f; // 줌 인/아웃에 걸리는 시간

    public void Initiate(PlayerController playerController)
    {
        this.playerController = playerController;
        //카메라가 player을 Follow하도록 합니다.
        cam = GetComponent<CinemachineCamera>();
        cam.Follow = playerController.transform;
        cam.Lens.OrthographicSize = zoomFOV;
        BindEvents();
    }

    void BindEvents()
    {
        playerController.OnJumpStart += OnZoomStart;
        playerController.OnLand += OnZoomEnd;
    }

    private void OnDestroy()
    {
        if (playerController != null)
        {
            playerController.OnJumpStart -= OnZoomStart;
            playerController.OnLand -= OnZoomEnd;
        }
    }

    public void OnZoomStart()
    {
        float targetFOV = cam.Lens.OrthographicSize - zoomChangeAmount;
        StartCoroutine(SmoothZoom(zoomEndFOV));
    }

    public void OnZoomEnd()
    {
        float targetFOV = cam.Lens.OrthographicSize + zoomChangeAmount;
        StartCoroutine(SmoothZoom(zoomFOV));
    }

    private IEnumerator SmoothZoom(float targetFOV)
    {
        float startFOV = cam.Lens.OrthographicSize;
        float elapsedTime = 0f;

        while (elapsedTime < zoomDuration)
        {
            // Lerp를 사용하여 시작 값에서 목표 값으로 부드럽게 보간
            float newFOV = Mathf.Lerp(startFOV, targetFOV, elapsedTime / zoomDuration);

            // Cinemachine 카메라의 FOV(렌즈) 값 업데이트
            var lens = cam.Lens;
            lens.OrthographicSize = newFOV;
            cam.Lens = lens;

            elapsedTime += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        // 정확한 목표 값으로 최종 설정
        var finalLens = cam.Lens;
        finalLens.OrthographicSize = targetFOV;
        cam.Lens = finalLens;
    }
}
