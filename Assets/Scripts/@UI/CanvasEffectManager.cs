using System.Collections.Generic;
using UnityEngine;

public class CanvasEffectManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> illustEffectList = new List<GameObject>();
    // --- (AudioClip, AudioSource 참조 제거) ---

    public void Initiate()
    {
        // --- (PlayerController 구독 제거) ---
        // --- (AudioSource 초기화 제거) ---

        // 게임 시작 시 모든 연출 Panel을 미리 비활성화
        foreach (GameObject effect in illustEffectList)
        {
            if (effect != null)
            {
                effect.SetActive(false);
            }
        }
    }

    // GameManager가 PlayerController.OnCombo 이벤트를 받아서 호출해줄 함수
    public void PlayIllustEffect(int combo)
    {
        if (combo == 0) return;

        // 콤보 1일 때 -> 0번 인덱스
        int index = combo - 1;

        if (index >= illustEffectList.Count)
        {
            index = illustEffectList.Count - 1;
        }

        if (index < 0) return; // (안전장치)

        GameObject selectedEffect = illustEffectList[index];
        DirectionalPanelAnimation animation = selectedEffect.GetComponent<DirectionalPanelAnimation>();

        if (animation != null)
        {
            animation.StartDirectionalAnimation();
        }
        else
        {
            Debug.LogError($"[CanvasEffectManager] {selectedEffect.name}에 DirectionalPanelAnimation 스크립트가 없습니다.");
        }
    }

    // --- (PlaySound 메서드 제거) ---
}