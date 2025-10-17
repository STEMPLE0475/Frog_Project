using UnityEngine;

public class OceanController : MonoBehaviour
{
    // 바다 표면에 적용된 머티리얼
    private Material oceanMaterial;

    // 쉐이더 프로퍼티 이름과 ID (미리 가져오면 성능에 유리)
    private static readonly int RippleCenterID = Shader.PropertyToID("_RippleCenter");
    private static readonly int RippleStartTimeID = Shader.PropertyToID("_RippleStartTime");

    // 리플 효과의 지속 시간
    public float rippleDuration = 2.0f;
    private float lastRippleTime = -100f; // 마지막 리플 시간 (초기값은 충분히 작게)

    void Start()
    {
        // 렌더러에서 머티리얼을 가져옴 (인스턴스가 생성됨)
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            oceanMaterial = renderer.material;
        }
    }

    // Is Trigger가 켜진 Collider에 다른 Collider가 들어왔을 때 호출됨
    private void OnTriggerEnter(Collider other)
    {
        // 충돌 지점을 찾음 (가장 가까운 포인트)
        Vector3 impactPoint = other.ClosestPoint(transform.position);

        // 현재 시간을 기록
        lastRippleTime = Time.time;

        // 쉐이더에 충돌 지점과 시간을 전달
        if (oceanMaterial != null)
        {
            oceanMaterial.SetVector(RippleCenterID, impactPoint);
            oceanMaterial.SetFloat(RippleStartTimeID, lastRippleTime);
        }
    }

    void Update()
    {
        // 리플 효과가 시간이 지나면 자연스럽게 사라지도록 처리 (선택사항)
        // 쉐이더에서 자체적으로 처리할 수도 있지만, 스크립트에서 값을 초기화하면 더 확실합니다.
        if (Time.time - lastRippleTime > rippleDuration)
        {
            // 효과를 없애기 위해 시작 시간을 아주 먼 과거로 보냄
            oceanMaterial.SetFloat(RippleStartTimeID, -100f);
        }
    }
}