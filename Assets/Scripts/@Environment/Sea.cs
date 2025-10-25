using System.Collections.Generic;
using UnityEngine;

public class Sea : MonoBehaviour
{
    [Tooltip("Offset을 조절할 Material")]
    public Material targetMaterial;
    [Tooltip("X, Y 축 스크롤 속도 (음수면 감소)")]
    public float windSpeed = 1.0f;
    
    public Vector2 scrollSpeed = new Vector2(-0.1f, -0.1f);

    // 쉐이더의 메인 텍스처 Offset 속성 이름
    private const string mainTexProperty = "_BaseMap";

    // 스크립트가 시작될 때 한 번 호출됩니다.
    void Start()
    {
        targetMaterial = GetComponent<Renderer>().material;
    }

    public void SetSpeed(float speed) => this.windSpeed = speed;

    void Update()
    {
        MoveSea();
    }

    void MoveSea()
    {
        Vector2 currentOffset = targetMaterial.GetTextureOffset(mainTexProperty);

        float offsetY = currentOffset.y - scrollSpeed.y * Time.deltaTime * windSpeed;

        targetMaterial.SetTextureOffset(mainTexProperty, new Vector2(currentOffset.x, offsetY));
    }
}
