using System;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;


public class WindManager : MonoBehaviour
{
    int landTempCount = 0;

    [SerializeField] private int baseChangeWindCycleTerm = 3;
    [SerializeField] private int maxWindPower = 3;
    [SerializeField] private int minWindPower = 0;

    [SerializeField] private float makeNewWindDelay = 1f;

    public Wind wind;

    public event Action<Wind> OnWindChanged;

    public void Initiate()
    {
        wind = new Wind(1, 1);
    }
    public void ResetWindMangaer() {
        ResetLandCount();
        ResetWind();
        OnWindChanged?.Invoke(wind);
    }

    // 1. GameManager에서 Land시 호출 -> 점프 카운트 갱신
    public void PlusLandCount() => landTempCount++;

    // 2. GameManager에서 점프시 호출 -> 딜레이 후 새로운 바람 생성
    public void StartMakeNewWind()
    {
        if (landTempCount >= baseChangeWindCycleTerm) StartCoroutine(MakeNewWindCoroutine());
    }
    IEnumerator MakeNewWindCoroutine()
    {
        yield return new WaitForSecondsRealtime(makeNewWindDelay);
        ResetLandCount();
        wind.direction = (UnityEngine.Random.Range(0, 2) == 0) ? -1 : 1;
        wind.power = UnityEngine.Random.Range(minWindPower, maxWindPower + 1);
        OnWindChanged?.Invoke(wind);
    }

    // 유틸리티
    public void ResetLandCount() => landTempCount = 0;
    public void ResetWind() { wind.power = 0; wind.direction = 1; }
    public Wind GetWind() => wind;
}

public class Wind
{
    public int direction;
    public int power;

    public Wind(int direction, int power)
    {
        this.direction = direction;
        this.power = power;
    }
    
}