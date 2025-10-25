using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerEffects : MonoBehaviour
{
    [Header("파티클")]
    [SerializeField] private List<ParticleSystem> starParticles;
    [SerializeField] private GameObject groundParticle;

    [Header("메시 렌더러")]
    [SerializeField] private MeshRenderer PlayerMesh;
    [SerializeField] private MeshRenderer PlayerShadow;

    [Header("트레일")]
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private List<Material> trailRendererMaterials;

    [Header("사운드")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip jumpSfx;
    [SerializeField] private AudioClip landSfx;
    [SerializeField] private AudioClip waterSplashSfx;
    [SerializeField] private AudioClip gameOverSfx;

    [Header("애니메이션 설정")]
    [SerializeField] private float squashAmount = 0.1f;
    [SerializeField] private float stretchAmount = 1.5f; 
    [SerializeField] private float animationSpeed = 2.2f;

    private Vector3 originalScale;
    private Coroutine scaleAnimationCoroutine;

    public void Initiate()
    {
        originalScale = transform.localScale;
    }

    // --- 애니메이션 ---
    public void PlayChargeAnimation()
    {
        if (scaleAnimationCoroutine != null) StopCoroutine(scaleAnimationCoroutine);
        Vector3 squashScale = new Vector3(originalScale.x, originalScale.y * squashAmount, originalScale.z);
        scaleAnimationCoroutine = StartCoroutine(AnimateScale(squashScale, animationSpeed));
    }

    public void PlayJumpAnimation()
    {
        if (scaleAnimationCoroutine != null) StopCoroutine(scaleAnimationCoroutine);
        // 점프 시 늘어났다가 돌아오는 애니메이션
        scaleAnimationCoroutine = StartCoroutine(AnimateJumpStretch());
    }

    public void ResetScale()
    {
        if (scaleAnimationCoroutine != null) StopCoroutine(scaleAnimationCoroutine);
        transform.localScale = originalScale;
    }

    private IEnumerator AnimateScale(Vector3 targetScale, float speed)
    {
        while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
            yield return null;
        }
        transform.localScale = targetScale;
    }

    private IEnumerator AnimateJumpStretch()
    {
        // 1. 빠르게 늘어남
/*        Vector3 stretchScale = new Vector3(originalScale.x, originalScale.y * stretchAmount, originalScale.z);
        yield return StartCoroutine(AnimateScale(stretchScale, animationSpeed * 1f));*/

        // 2. 원래대로 돌아옴
        yield return StartCoroutine(AnimateScale(originalScale, animationSpeed * 2.0f));
    }

    // --- 사운드 ---
    public void PlayJumpSound()
    {
        if (sfxSource != null && jumpSfx != null)
            sfxSource.PlayOneShot(jumpSfx);
    }

    public void PlayLandSound()
    {
        // (착지 사운드 재생 로직)
        if (sfxSource != null && landSfx != null)
            sfxSource.PlayOneShot(landSfx);
    }

    public void PlaySeaCollisionSound()
    {
        if(waterSplashSfx != null && gameOverSfx != null)
        {
            sfxSource.PlayOneShot(waterSplashSfx);
            sfxSource.PlayOneShot(gameOverSfx);
        }
    }

    // --- 파티클 ---
    public void PlayLandParticles(LandingAccuracy accuracy)
    {
        AllParticleStop();
        switch (accuracy)
        {
            case LandingAccuracy.Perfect:
                if (starParticles.Count > 2 && starParticles[2] != null) starParticles[2].Play();
                break;
            case LandingAccuracy.Good:
                if (starParticles.Count > 1 && starParticles[1] != null) starParticles[1].Play();
                break;
            case LandingAccuracy.Bad:
                // (Bad 파티클이 있다면 여기에)
                break;
            case LandingAccuracy.Excep:
                if (starParticles.Count > 1 && starParticles[1] != null) starParticles[1].Play();
                break;
        }

        // 바닥 파티클 재생
        var obj = Instantiate(groundParticle, new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z), groundParticle.transform.rotation);
        obj.GetComponent<ParticleSystem>().Play();
    }

    private void AllParticleStop()
    {
        foreach (var particle in starParticles)
        {
            if (particle != null)
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    // --- 트레일 ---
    public void UpdateTrail(int combo)
    {
        int maxIndex = trailRendererMaterials.Count - 1;
        int trailIndex = Mathf.Clamp(combo, 0, maxIndex);

        Material[] newMaterials = trailRenderer.materials;
        newMaterials[0] = trailRendererMaterials[trailIndex];
        trailRenderer.materials = newMaterials;
    }

    public void SetTrail(bool enabled)
    {
        trailRenderer.enabled = enabled;
    }

    // --- 메시 렌더러 ---
    public void SetPlayerMesh(bool isShadowMode)
    {
        if (isShadowMode)
        {
            PlayerMesh.enabled = false;
            PlayerShadow.enabled = true;
            PlayerShadow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
        else
        {
            PlayerMesh.enabled = true;
            PlayerShadow.enabled = true;
            PlayerShadow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }
}