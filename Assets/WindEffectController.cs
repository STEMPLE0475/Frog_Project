using UnityEngine;

/// <summary>
/// 바람 파티클 이펙트를 제어하고 플레이어를 따라다닙니다.
/// GameManager에 의해 관리됩니다.
/// </summary>
public class WindEffectController : MonoBehaviour
{
    // 1. 파티클 시스템 (인스펙터에서 할당)
    [SerializeField] private ParticleSystem windEffectParticle;
    [SerializeField] private int windEffectAmount = 35;

    // 2. 따라다닐 대상
    private Transform playerToFollow;


    public void Initiate(Transform playerTransform)
    {
        gameObject.SetActive(true);
        this.playerToFollow = playerTransform;

        if (windEffectParticle == null)
        {
            windEffectParticle = GetComponentInChildren<ParticleSystem>();
        }

        if (windEffectParticle == null)
        {
            Debug.LogError("WindEffectController에 할당된 ParticleSystem이 없습니다!", this);
        }
    }

    /// <summary>
    /// 매 프레임 플레이어의 위치를 정확히 따라다닙니다.
    /// </summary>
    private void Update()
    {
        if (playerToFollow != null)
        {
            transform.position = playerToFollow.position;
        }
    }

    /// <summary>
    /// [핵심] 바람 변경 시 GameManager가 호출하는 함수
    /// </summary>
    /// <param name="wind">WindManager가 보낸 새로운 바람 정보</param>
    public void UpdateWindEffect(Wind wind)
    {
        if (windEffectParticle == null) return;

        // --- 1. 방향(Rotation) 처리 ---
        // (이펙트 오브젝트의 로컬 Y축 회전을 0도 또는 180도로 설정)
        Vector3 currentLocalRotation = transform.localEulerAngles;

        if (wind.direction < 0)
        {
            // 방향이 음수(-1)이면 Y축 180도 회전 (왼쪽)
            transform.localEulerAngles = new Vector3(currentLocalRotation.x, 45f, currentLocalRotation.z);
        }
        else
        {
            // 방향이 양수(1)이면 Y축 0도 회전 (오른쪽/기본값)
            transform.localEulerAngles = new Vector3(currentLocalRotation.x, 225f, currentLocalRotation.z);
        }

        // --- 2. 힘(Particle Emission) 처리 ---
        var emission = windEffectParticle.emission;


        if (wind.power == 0) { StopEffect(); }
        else
        {
            float newRate = wind.power * windEffectAmount;
            emission.rateOverTime = newRate;
            PlayEffect();
        }
    }

    void StopEffect()
    {
        windEffectParticle.Stop();
        windEffectParticle.Clear();
    }

    void PlayEffect() => windEffectParticle.Play();
}