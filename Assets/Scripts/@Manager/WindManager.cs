using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;


public class WindManager : MonoBehaviour
{
    [SerializeField] private int baseChangeWindCycleTerm = 3;
    [SerializeField] private int maxWindPower = 3;
    [SerializeField] private int minWindPower = 0;

    [SerializeField] private float makeNewWindDelay = 0.5f;

    private int currentSessionLandCount = 0;

    [Header("바람 난이도 설정")]
    // 착지 횟수에 비례하여 바람 강도 증가에 영향을 주는 계수
    [SerializeField] private float windDifficultyFactor = 0.05f;
    // '바람 없음(Power 0)'의 기본 가중치 (값이 높을수록 초반에 바람이 안 불 확률 증가)
    [SerializeField] private float noWindBaseWeight = 6f;

    public Wind wind;

    public event Action<Wind> OnWindChanged;

    public void Initiate()
    {
        currentSessionLandCount = 0;
        wind = new Wind(1, 0);
        OnWindChanged?.Invoke(wind);
    }
    public void ResetWindMangaer() {
        currentSessionLandCount = 0;
        ResetWind();
        OnWindChanged?.Invoke(wind);
    }

    // 1. GameManager에서 Land시 호출 -> 점프 카운트 갱신
    public void SetLandCount(int sessionLandCount)
    {
        currentSessionLandCount = sessionLandCount;
    }

    // GameManager에서 Jump 시작시 호출?
    public void StartMakeNewWind() {
        if (currentSessionLandCount > 0 && currentSessionLandCount % baseChangeWindCycleTerm == 0)
        {
            StartCoroutine(MakeNewWindCoroutine(currentSessionLandCount));
        }
    }



    IEnumerator MakeNewWindCoroutine(int currentLandCount)
    {
        
        yield return new WaitForSecondsRealtime(makeNewWindDelay);
        wind.direction = (UnityEngine.Random.Range(0, 2) == 0) ? -1 : 1;
        wind.power = GetWeightedRandomPower(currentLandCount);
        OnWindChanged?.Invoke(wind);
    }

    private int GetWeightedRandomPower(int currentLandCount)
    {
        List<float> weights = new List<float>();
        float totalWeight = 0f;

        for (int powerLevel = minWindPower; powerLevel <= maxWindPower; powerLevel++)
        {
            float currentWeight = 0f;

            if (powerLevel == 0)
            {
                currentWeight = noWindBaseWeight;
            }
            else
            {
                // --- 여기가 수정 지점 ---

                // 1. powerLevel에 따라 기본 가중치를 차등 설정
                // (maxWindPower = 3일 때)
                // P1 -> (3 - 1) + 1f = 3f
                // P2 -> (3 - 2) + 1f = 2f
                // P3 -> (3 - 3) + 1f = 1f
                float baseWeight = (maxWindPower - powerLevel) + 1f;

                // 2. 보너스 가중치는 LandCount에만 비례하도록 변경 (powerLevel 곱셈 제거)
                // (powerLevel을 곱하면 P3가 P1보다 보너스를 3배 받아 역전 현상 발생)
                float bonusWeight = (currentLandCount * windDifficultyFactor);

                // 3. 최종 가중치 = 기본 가중치 + 보너스 가중치
                currentWeight = baseWeight + bonusWeight;

                // --- 수정 끝 ---
            }

            if (currentWeight < 0) currentWeight = 0;

            weights.Add(currentWeight);
            totalWeight += currentWeight;
        }

        // 4. (예외 처리) 모든 가중치가 0일 경우 0을 반환
        if (totalWeight <= 0)
        {
            return minWindPower;
        }

        // ... (이하 가중치 추첨 로직은 동일) ...
        float randomValue = UnityEngine.Random.Range(0, totalWeight);

        for (int i = 0; i < weights.Count; i++)
        {
            if (randomValue < weights[i])
            {
                return minWindPower + i;
            }
            randomValue -= weights[i];
        }

        return maxWindPower;
    }

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