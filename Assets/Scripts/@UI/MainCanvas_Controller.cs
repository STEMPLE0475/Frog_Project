using UnityEngine;

public class MainCanvas_Controller : MonoBehaviour
{
    [SerializeField] private TutorialImage tutorialImage;
    

    public void Initiate()
    {

    }

    public void TutorialBlinkStart()
    {
        tutorialImage.StartBlink();
    }
}
