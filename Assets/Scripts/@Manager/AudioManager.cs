using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource uiSfxSource;
    [SerializeField] private AudioSource inGameSfxSource;
    [SerializeField] private AudioSource windStartSource;

    [Header("Clips & Settings")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private List<AudioClip> comboClips;
    [SerializeField, Range(0f, 2f)] private float uiVolume = 1.0f; // (GameManager가 아닌 여기서 관리)

    public void Initiate(List<ButtonSound> buttonSounds)
    {
        // 1. BGM 설정
        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.volume = 0.3f;
            bgmSource.Play();
        }

        // 2. 버튼 사운드 연결 (GameManager의 Start()에서 가져옴)
        foreach (var btnSound in buttonSounds)
        {
            if (btnSound != null && btnSound.button != null && uiSfxSource != null)
            {
                btnSound.button.onClick.AddListener(() =>
                {
                    // 볼륨 설정 적용
                    uiSfxSource.PlayOneShot(btnSound.clickSfx, uiVolume);
                });
            }
        }
    }

    public void PauseBGM(bool pause)
    {
        if (bgmSource == null) return;
        if (pause)
            bgmSource.Pause();
        else
            bgmSource.UnPause();
    }

    public void PlayComboSound(int combo)
    {
        if (combo == 0) return;
        int index = combo - 1;
        index = Mathf.Clamp(index, 0, comboClips.Count - 1);
        inGameSfxSource.PlayOneShot(comboClips[index]); 
    }

    public void PlayStartWindSound(Wind wind) {
        if(wind.power == 0) return;
        windStartSource.Play();
    }
}