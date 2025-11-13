using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NextCharacter : MonoBehaviour
{
    //내부 변수
    private GameObject spawnedCharacterObj;
    private GameObject spawnedEndCharacterObj;

    //참조 변수
    [SerializeField] private List<GameObject> skinPrefabs;
    [SerializeField] private Transform nextCharacterSpawnPos; // 시작 소환 장소
    [SerializeField] private Transform nextCharacterEndPos; // 엔딩 소환 장소

    [Header("점프 설정")]
    private Vector3 jumpDirection = new Vector3(1f, 0f, 1f);
    public float jumpDuration = 1.2f;
    public float jumpEndDuration = 2f;
    public float moveDuration = 3f;
    private float heightMultiplier = 1f;

    //시작 애니메이션 변수
    [SerializeField] private float jumpPower = 4f;
    [SerializeField] private float StartDelayTime = 1f;

    //끝 애니메이션 변수
    [SerializeField] private float jumpPowerEnd = 10f;
    private float turnaroundTime = 1f;
    private float turnaroundAndWaitDelay = 1f;
    public Action<Transform> OnCharacterAnimationEnd;

    public void Initiate()
    {
        
    }

    //GameManger -> ResetGame
    public void SpawnFrog()
    {
        SpawnCharacterSpawnPos(0);
        SpawnCharacterEndPos(0);
    }

    public void SpawnCharacterSpawnPos(int skinIndex)
    {
        if (spawnedCharacterObj != null)
        {
            spawnedCharacterObj = null; 
            Destroy(spawnedCharacterObj);
        }
        
        spawnedCharacterObj = Instantiate(skinPrefabs[0], nextCharacterSpawnPos);
    }
    public void SpawnCharacterEndPos(int skinIndex)
    {
        if (spawnedEndCharacterObj != null) {
            spawnedEndCharacterObj = null; 
            Destroy(spawnedEndCharacterObj);
        } 
        spawnedEndCharacterObj = Instantiate(skinPrefabs[0], nextCharacterEndPos);
    }

    public void PlayStartAnimation()
    {
        StartCoroutine(ParabolicJump(jumpPower));
    }

    public void PlayEndAnimation()
    {
        StartCoroutine(EndAnimationCoroutine(jumpPowerEnd));
    }

    private IEnumerator EndAnimationCoroutine(float jumpForce)
    {
        yield return new WaitForSeconds(1f);

        if (turnaroundTime <= 0) turnaroundTime = 0.1f;

        float elapsedTime = 0f;
        Quaternion startQuaternion = spawnedEndCharacterObj.transform.rotation;

        Quaternion targetQuaternion = Quaternion.Euler(0, -225, 0);

        while (elapsedTime < turnaroundTime)
        {
            elapsedTime += Time.deltaTime;
            float percent = elapsedTime / turnaroundTime;

            spawnedEndCharacterObj.transform.rotation = Quaternion.Slerp(startQuaternion, targetQuaternion, percent);

            yield return null;
        }
        spawnedEndCharacterObj.transform.rotation = targetQuaternion;


        OnCharacterAnimationEnd?.Invoke(spawnedEndCharacterObj.transform);
        yield return new WaitForSecondsRealtime(turnaroundAndWaitDelay);


        Vector3 startPos = spawnedEndCharacterObj.transform.position;
        Quaternion startRotation = spawnedEndCharacterObj.transform.rotation;
        Vector3 endPos = startPos + jumpDirection.normalized * jumpForce + Vector3.up * 0.1f;

        elapsedTime = 0f;
        float jumpHeight = jumpForce * heightMultiplier;

        while (elapsedTime < jumpEndDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / jumpEndDuration); // 1.0을 넘지 않게

            float currentAngle = 360f * progress;
            spawnedEndCharacterObj.transform.rotation = startRotation * Quaternion.Euler(currentAngle, 0, 0);

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            spawnedEndCharacterObj.transform.position = currentPos;

            yield return null;
        }

        spawnedEndCharacterObj.transform.position = endPos;
        spawnedEndCharacterObj.transform.rotation = startRotation;
    }

    

    private IEnumerator ParabolicJump(float jumpForce)
    {
        yield return new WaitForSeconds(StartDelayTime);

        Vector3 startPos = spawnedCharacterObj.transform.position;
        Quaternion startRotation = spawnedCharacterObj.transform.rotation;
        Vector3 endPos = startPos + jumpDirection.normalized * jumpForce + Vector3.up * -2.5f;

        float elapsedTime = 0f;
        float jumpHeight = jumpForce * heightMultiplier;

        Vector3 accumulatedWindMovement = Vector3.zero;

        while (elapsedTime < jumpDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / jumpDuration);

            float currentAngle = 360f * progress;
            spawnedCharacterObj.transform.rotation = startRotation * Quaternion.Euler(currentAngle, 0, 0);

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * jumpHeight;

            spawnedCharacterObj.transform.position = currentPos + accumulatedWindMovement;

            yield return null;
        }
        spawnedCharacterObj.transform.rotation = startRotation;

        yield return new WaitForSecondsRealtime(0.3f);

        elapsedTime = 0f;
        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            spawnedCharacterObj.transform.position = spawnedCharacterObj.transform.position + new Vector3(1, 0, 1) * 20f * Time.deltaTime;
            yield return null;
        }
        Destroy(spawnedCharacterObj);
    }
}
