using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 게임의 전체적인 로직을 관리하는 스크립트

    // 해당하는 변수를 Editor에서 할당해줄 필요가 있음. (이것만은 반드시 해줘야함! 안하면 오류남)
    // 이 스크립트 오류만 없으면 다른 세부적인 부분 (PlayerContorller 등에서 설정 안해도 작동하도록 깔끔하게 해야함)

    [Header("Class")]
    [SerializeField] private CinemachineCameraScript CinemachineCameraScript;
    [SerializeField] private EffectTestPlayerController PlayerController;

    [Header("Variable")]
    [SerializeField] private Transform playerSpawnTransform;


    // 본 프로젝트의 모든 Awake()나 Start()는 사용 금지.
    // 모든 프로세스의 시작을 분명히 하기 위해서. 모든 로직은 GameManager을 통해서 시작된다.
    private void Awake()
    {
        CinemachineCameraScript.Initiate(PlayerController);
        PlayerController.Initiate(playerSpawnTransform);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
