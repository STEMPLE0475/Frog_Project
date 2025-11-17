using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;


public class WindManager : MonoBehaviour
{
    [SerializeField] private int baseChangeWindCycleTerm = 3;

    // ▼▼▼ 새 로직에서 사용되지 않으므로 주석 처리 (또는 삭제) ▼▼▼
    // [SerializeField] private int maxWindPower = 3;
    // [SerializeField] private int minWindPower = 0;

    [SerializeField] private float makeNewWindDelay = 0.5f;

    private int currentSessionLandCount = 0;

    // ▼▼▼ 새 로직에서 사용되지 않으므로 주석 처리 (또는 삭제) ▼▼▼
    // [Header("바람 난이도 설정")]
    // [SerializeField] private float windDifficultyFactor = 0.05f;
    // [SerializeField] private float noWindBaseWeight = 6f;

    // 파티클 시스템
    [Header("Effects (Assign in Editor)")]
    [SerializeField] private ParticleSystem windEffect;

    public Wind wind;

    public event Action<Wind> OnWindChanged;

    public void Initiate()
    {
        currentSessionLandCount = 0;
        wind = new Wind(1, 0);
        OnWindChanged?.Invoke(wind);
    }
    public void ResetWindMangaer()
    {
        currentSessionLandCount = 0;
        ResetWind();
        OnWindChanged?.Invoke(wind);
    }

    // 1. GameManager에서 Land시 호출 -> 점프 카운트 갱신
    public void SetLandCount(int sessionLandCount)
    {
        currentSessionLandCount = sessionLandCount;
    }

    // GameManager에서 Jump 시작시 호출
    public void StartMakeNewWind()
    {
        if (currentSessionLandCount > 0 && currentSessionLandCount % baseChangeWindCycleTerm == 0)
        {
            StartCoroutine(MakeNewWindCoroutine(currentSessionLandCount));
        }
    }

    IEnumerator MakeNewWindCoroutine(int currentLandCount)
    {
        yield return new WaitForSecondsRealtime(makeNewWindDelay);

        // 방향은 항상 랜덤
        wind.direction = (UnityEngine.Random.Range(0, 2) == 0) ? -1 : 1;

        // ▼▼▼ 수정된 로직 호출 ▼▼▼
        wind.power = GetWeightedRandomPower(currentLandCount);

        OnWindChanged?.Invoke(wind);
    }

    // ▼▼▼ 요청하신 대로 로직 전체 수정 ▼▼▼
    private int GetWeightedRandomPower(int currentLandCount)
    {
        // 15회 이하: 0 또는 1
        if (currentLandCount <= 15)
        {
            // Random.Range(int min, int max)는 max가 제외됩니다.
            return UnityEngine.Random.Range(0, 2); // 0, 1 중에서 랜덤
        }
        // 30회 이하: 0, 1, 2
        else if (currentLandCount <= 30)
        {
            return UnityEngine.Random.Range(0, 3); // 0, 1, 2 중에서 랜덤
        }
        // 50회 이하: 0, 1, 2, 3
        else if (currentLandCount <= 50)
        {
            return UnityEngine.Random.Range(0, 4); // 0, 1, 2, 3 중에서 랜덤
        }
        // 50회 초과: 1, 2, 3
        else
        {
            return UnityEngine.Random.Range(1, 4); // 1, 2, 3 중에서 랜덤
        }
    }
    // ▲▲▲ 수정된 로직 끝 ▲▲▲


    // 유틸리티
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