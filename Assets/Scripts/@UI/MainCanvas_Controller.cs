using UnityEngine;
using UnityEngine.UI;

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
