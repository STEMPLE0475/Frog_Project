using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    private CanvasEffectManager effectManager;
    
    public void Initiate()
    {
        effectManager = GetComponentInChildren<CanvasEffectManager>();
        effectManager.Initiate();

    }

    public void PlayIllustAnimation(int index)
    {
        effectManager.PlayIllustEffect(index);
    }
}
