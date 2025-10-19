using System.Collections.Generic;
using UnityEngine;

public class CanvasEffectManager : MonoBehaviour
{
    [Tooltip("관리할 연출 효과 Panel들의 리스트")]
    [SerializeField]private List<GameObject> illustEffectList = new List<GameObject>();
    [SerializeField]private List<AudioClip> soundClips = new List<AudioClip>();
    private AudioSource audioSource;

    public void Initiate(PlayerController playerController)
    {
        audioSource = GetComponent<AudioSource>();
        // 게임 시작 시 모든 연출 Panel을 미리 비활성화
        foreach (GameObject effect in illustEffectList)
        {
            if (effect != null)
            {
                effect.SetActive(false);
            }
        }

        playerController.OnCombo += PlayIllustEffect;
        playerController.OnCombo += PlaySound;
    }

    public void PlayIllustEffect(int combo)
    {
        if (combo == 0) return;
        int index = combo - 1;
        int illustEffectListSize = illustEffectList.Count;
       
        if (index >= illustEffectList.Count)
        {
            index = illustEffectList.Count - 1;
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

    public void PlaySound(int combo)
    {
        if (combo == 0) return;
        int index = combo ;
        int size = soundClips.Count;

        if (index >= soundClips.Count)
        {
            index = soundClips.Count - 1;
        }
        audioSource.PlayOneShot(soundClips[index]);
    }
}
