using UnityEngine;

// 플레이의 핵심 상태를 관리하는 컴포넌트
public class PlayerState : MonoBehaviour
{
    public bool IsAirborne { get; private set; } = false;
    public bool IsFloating { get; private set; } = false;
    private Wind wind;


    public void SetAirborne(bool state) => IsAirborne = state;
    public void SetFloating(bool state) => IsFloating = state;
    public void SetWind(Wind wind) => this.wind = wind;
    public Wind GetWind() => wind;

    public void ResetState()
    {
        IsAirborne = false;
        IsFloating = false;
    }
}

// 착지 정확도를 나타내는 열거형
public enum LandingAccuracy
{
    Bad = 0,
    Good = 1,
    Perfect = 2,
    Excep = 3,
}