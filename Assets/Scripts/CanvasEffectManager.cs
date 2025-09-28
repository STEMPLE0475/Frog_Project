using System.Collections.Generic;
using UnityEngine;

public class CanvasEffectManager : MonoBehaviour
{
    [Tooltip("관리할 연출 효과 Panel들의 리스트")]
    [SerializeField]private List<GameObject> illustEffectList = new List<GameObject>();

    public void Initiate()
    {
        // 게임 시작 시 모든 연출 Panel을 미리 비활성화
        foreach (GameObject effect in illustEffectList)
        {
            if (effect != null)
            {
                effect.SetActive(false);
            }
        }
    }

    public void PlayIllustEffect(int index)
    {
        // 인덱스에 해당하는 리스트(일러스트)가 존재하는지 검증
        if (index < 0 || index >= illustEffectList.Count)
        {
            Debug.LogError($"[CanvasEffectManager] 잘못된 이펙트 인덱스({index})가 호출되었습니다.");
            return;
        }

        GameObject selectedEffect = illustEffectList[index];

        // Panel에서 애니메이션 스크립트를 가져옵니다.
        DirectionalPanelAnimation animation = selectedEffect.GetComponent<DirectionalPanelAnimation>();

        if (animation != null)
        {
            // 4. 애니메이션 스크립트의 재생함수 호출
            animation.StartDirectionalAnimation();
        }
        else
        {
            Debug.LogError($"[CanvasEffectManager] {selectedEffect.name}에 DirectionalPanelAnimation 스크립트가 없습니다.");
        }
    }
}
