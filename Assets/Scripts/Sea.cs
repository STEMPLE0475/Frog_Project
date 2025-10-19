using UnityEngine;

public class Sea : MonoBehaviour
{

    [Tooltip("Offset을 조절할 Material")]
    public Material targetMaterial;
    [Tooltip("X, Y 축 스크롤 속도 (음수면 감소)")]
    public Vector2 scrollSpeed = new Vector2(-0.2f, -0.2f);

    // 쉐이더의 메인 텍스처 Offset 속성 이름
    private const string mainTexProperty = "_BaseMap";

    // --- 내부 변수 ---
    private Vector3 initialPosition; // 오브젝트의 초기 위치를 저장할 변수

    // 스크립트가 시작될 때 한 번 호출됩니다.
    void Start()
    {
        // 현재 위치를 초기 위치로 저장해둡니다.
        initialPosition = transform.position;
        targetMaterial = GetComponent<Renderer>().material;
    }

    // 매 프레임마다 호출됩니다.
    void Update()
    {


        MoveSea();
    }

    void MoveSea()
    {
        Vector2 currentOffset = targetMaterial.GetTextureOffset(mainTexProperty);

        //float offsetX = currentOffset.x - scrollSpeed.x * Time.deltaTime;
        float offsetY = currentOffset.y - scrollSpeed.y * Time.deltaTime;

        targetMaterial.SetTextureOffset(mainTexProperty, new Vector2(currentOffset.x, offsetY));
    }
}
