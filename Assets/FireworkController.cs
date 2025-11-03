using UnityEngine;

public class FireworkController : MonoBehaviour
{
    private ParticleSystem fireworkParticle;
    public void Initiate()
    {
        fireworkParticle = GetComponent<ParticleSystem>();
    }

    public void PlayEffect()
    {
        fireworkParticle.Play();
    }

}
