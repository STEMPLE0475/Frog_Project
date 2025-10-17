using UnityEditor.TerrainTools;
using UnityEngine;

public class Sea : MonoBehaviour
{
    [Header("파도 설정")]
    [Tooltip("파도의 최대 높이 (중심에서 얼마나 올라갈지)")]
    [SerializeField] private float waveHeight = 0.2f;

    [Tooltip("파도가 얼마나 빠르게 움직일지")]
    [SerializeField] private float waveSpeed = 2.0f;

    [Tooltip("Offset을 조절할 Material")]
    public Material targetMaterial;
    [Tooltip("X, Y 축 스크롤 속도 (음수면 감소)")]
    public Vector2 scrollSpeed = new Vector2(-0.1f, -0.1f);

    // 쉐이더의 메인 텍스처 Offset 속성 이름
    private const string mainTexProperty = "_MainTex";

    // --- 내부 변수 ---
    private Vector3 initialPosition; // 오브젝트의 초기 위치를 저장할 변수

    // 스크립트가 시작될 때 한 번 호출됩니다.
    void Start()
    {
        // 현재 위치를 초기 위치로 저장해둡니다.
        initialPosition = transform.position;
        targetMaterial = GetComponent<Renderer>().materials[0];
    }

    // 매 프레임마다 호출됩니다.
    void Update()
    {
        // 1. 시간에 따라 부드럽게 -1과 1 사이를 반복하는 사인파 값을 계산합니다.
        //    Time.time * waveSpeed : 시간에 따라 파도의 속도를 조절합니다.
        /*float waveOffset = Mathf.Sin(Time.time * waveSpeed) * waveHeight;

        // 2. 계산된 파도 높낮이(waveOffset)를 초기 Y 위치에 더해 새로운 위치를 만듭니다.
        transform.position = new Vector3(
            initialPosition.x,
            initialPosition.y + waveOffset,
            initialPosition.z
        );*/

        MoveSea();
    }

    void MoveSea()
    {
        Vector2 currentOffset = targetMaterial.GetTextureOffset(mainTexProperty);

        float offsetX = currentOffset.x + scrollSpeed.x * Time.deltaTime;
        float offsetY = currentOffset.y + scrollSpeed.y * Time.deltaTime;

        targetMaterial.SetTextureOffset(mainTexProperty, new Vector2(offsetX, offsetY));
    }
}
