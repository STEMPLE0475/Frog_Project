using TMPro;
using UnityEngine;

public class LoginCanvas_Controller : MonoBehaviour
{
    [SerializeField] private TMP_InputField input_field;
    [SerializeField] private MainCanvasManager canvasManager;

    public void Initiate(MainCanvasManager canvasManager)
    {
        this.canvasManager = canvasManager;
    }

    public void OnClickOKButton()
    {
        string txt = input_field.text;
        // 입력칸에 입력된 문자가 없는 경우 에러처리 코드
        Debug.Log(txt + " 입력");
        canvasManager.OnNickNameWrite?.Invoke(txt);
    }
}
