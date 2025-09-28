using Unity.Cinemachine;
using UnityEngine;

public class CinemachineCameraScript : MonoBehaviour
{
    private CinemachineCamera cam;

    public void Initiate(EffectTestPlayerController playerController)
    {
        //카메라가 player을 Follow하도록 합니다.
        cam = GetComponent<CinemachineCamera>();
        cam.Follow = playerController.transform;
    }
}
