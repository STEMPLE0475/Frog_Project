using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NextCharacter : MonoBehaviour
{
    //내부 변수
    private GameObject spawnedCharacterObj;

    //참조 변수
    [SerializeField] private List<GameObject> skinPrefabs;
    [SerializeField] private Transform nextCharacterSpawnPos; // 시작 소환 장소
    [SerializeField] private Transform nextCharacterEndPos; // 엔딩 소환 장소

    //변수
    [SerializeField] private float jumpPower = 10f;
    [SerializeField] private float StartDelayTime = 1f;
    

    public void Initiate()
    {
        SpawnCharacter(0);
    }

    public void SpawnCharacter(int skinIndex)
    {
        spawnedCharacterObj = Instantiate(skinPrefabs[0], nextCharacterSpawnPos);
    }

    public void StartAnimation()
    {
        StartCoroutine(ParabolicJump(jumpPower));
    }

    public void EndAnimation()
    {

    }

    [Header("점프 설정")]
    private Vector3 jumpDirection = new Vector3(1f, 0f, 1f);
    public float jumpDuration = 1.2f;
    public float moveDuration = 3f;
    private float heightMultiplier = 1f;

    private IEnumerator ParabolicJump(float jumpForce)
    {
        yield return new WaitForSeconds(StartDelayTime);

        Vector3 startPos = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 endPos = startPos + jumpDirection.normalized * jumpForce + Vector3.up * -2.5f;

        float elapsedTime = 0f;
        float jumpHeight = jumpForce * heightMultiplier;

        Vector3 accumulatedWindMovement = Vector3.zero;

        while (elapsedTime < jumpDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / jumpDuration);

            float currentAngle = 360f * progress;
            transform.rotation = startRotation * Quaternion.Euler(currentAngle, 0, 0);

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * jumpHeight;

            transform.position = currentPos + accumulatedWindMovement;

            yield return null;
        }
        transform.rotation = startRotation;

        yield return new WaitForSecondsRealtime(0.3f);

        elapsedTime = 0f;
        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            transform.position = transform.position + new Vector3(1, 0, 1) * 20f * Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}
