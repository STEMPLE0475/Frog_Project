using UnityEngine;

// 이 스크립트는 BoxCollider가 있는 'Block' 오브젝트에 추가해야 합니다.
[RequireComponent(typeof(BoxCollider))]
public class BlockTargetZone : MonoBehaviour
{
    [Tooltip("자식 오브젝트에 있는 Perfect 존 시각적 표시 오브젝트")]
    [SerializeField] private GameObject targetZoneVisual;
    private float perfectThreshold;

    public void EnableTargetZone(float perfectThreshold)
    {
        this.perfectThreshold = perfectThreshold;
        BoxCollider blockCollider = GetComponent<BoxCollider>();
        SetupTargetZone(blockCollider, perfectThreshold);
    }

    /// <summary>
    /// 가져온 정보를 바탕으로 Target Zone의 크기와 위치를 설정합니다.
    /// </summary>
    private void SetupTargetZone(BoxCollider collider, float threshold)
    {
        // 1. 블록 콜라이더의 로컬 크기를 가져옵니다.
        Vector3 blockSize = collider.size;

        // 2. Perfect 존의 실제 크기를 계산합니다.
        float zoneSizeX = blockSize.x * threshold;
        float zoneSizeZ = blockSize.z * threshold;

        // 3. Plane은 기본 크기가 10x10이므로, 계산된 크기에 맞게 스케일을 조절합니다.
        //    (기본 Plane 1유닛 = 실제 크기 10)
        targetZoneVisual.transform.localScale = new Vector3(zoneSizeX / 10f, 1f, zoneSizeZ / 10f);

        // 4. 블록의 윗면보다 살짝 위에 위치시켜 겹치지 않게 합니다(Z-fighting 방지).
        targetZoneVisual.transform.localPosition = new Vector3(0, (blockSize.y / 2f) + 0.01f, 0);
    }
}
