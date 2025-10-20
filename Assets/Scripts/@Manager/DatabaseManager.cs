using Firebase.Extensions;
using Firebase.Firestore;
using System;
using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    public event Action<UserData> OnUserDataLoaded; // 데이터 로드 완료 이벤트

    private FirebaseFirestore db;
    private const string DEVICE_ID_KEY = "LocalDeviceID";
    private string currentDeviceID;
    private UserData loadedUserData;

    public void Initiate()
    {
        db = FirebaseFirestore.DefaultInstance;
        currentDeviceID = GetOrCreateDeviceID();

        // 시작하자마자 인증 및 데이터 로드 시도
        // (닉네임 입력란이 있다면, 그 시점에 HandleUserAuthentication 호출)
        // 여기서는 닉네임 없이 기기 ID로만 처리하는 것을 기본으로 함
        HandleUserAuthentication("");
    }

    public string GetOrCreateDeviceID()
    {
        string deviceID = PlayerPrefs.GetString(DEVICE_ID_KEY, "");
        if (string.IsNullOrEmpty(deviceID))
        {
            deviceID = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString(DEVICE_ID_KEY, deviceID);
            PlayerPrefs.Save();
            Debug.Log($"🔑 New Device ID Created: {deviceID}");
        }
        else
        {
            Debug.Log($"🔑 Existing Device ID Loaded: {deviceID}");
        }
        return deviceID;
    }

    public void HandleUserAuthentication(string inputNickname)
    {
        // (기존 GameManager의 HandleUserAuthentication 로직 동일하게 여기에 붙여넣기)
        // ... (생략) ...
        if (string.IsNullOrEmpty(inputNickname))
        {
            ProcessDataByDeviceID(currentDeviceID, inputNickname);
            return;
        }

        db.Collection("users")
            .WhereEqualTo("Nickname", inputNickname)
            .Limit(1)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(queryTask =>
            {
                QuerySnapshot snapshot = queryTask.Result;

                if (snapshot.Count > 0)
                {
                    // 닉네임 일치 시 ID 교체
                    string existingDocId = snapshot[0].Id;
                    PlayerPrefs.SetString(DEVICE_ID_KEY, existingDocId);
                    PlayerPrefs.Save();
                    currentDeviceID = existingDocId;

                    ProcessDataByDeviceID(existingDocId, inputNickname);
                }
                else
                {
                    ProcessDataByDeviceID(currentDeviceID, inputNickname);
                }
            });
        // ProcessDataByDeviceID가 성공적으로 loadedUserData를 채웠을 때
        // OnUserDataLoaded?.Invoke(loadedUserData); 를 호출해야 함.
        // (기존 코드에서는 ProcessDataByDeviceID 마지막에 이벤트를 발생시켜야 함)
    }

    private void ProcessDataByDeviceID(string docId, string inputNickname)
    {
        DocumentReference docRef = db.Collection("users").Document(docId);
        string finalNickname = string.IsNullOrEmpty(inputNickname) ?
                               "User_" + docId.Substring(0, 5).ToUpper() :
                               inputNickname;

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            DocumentSnapshot snapshot = task.Result;
            if (snapshot.Exists)
            {
                docRef.UpdateAsync("GameOpenedCount", FieldValue.Increment(1));
                loadedUserData = snapshot.ConvertTo<UserData>();
            }
            else
            {
                UserData initialData = new UserData
                {
                    Nickname = finalNickname,
                    FirstStartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    HighScore = 0,
                    GameOpenedCount = 1,
                    GameStartCount = 0
                };
                docRef.SetAsync(initialData);
                loadedUserData = initialData;
            }

            // ⭐ 데이터 로드/생성이 완료되었음을 외부에 알림
            OnUserDataLoaded?.Invoke(loadedUserData);
        });
    }

    // 게임 시작 시
    public void IncrementGameStartCount()
    {
        if (string.IsNullOrEmpty(currentDeviceID)) return;
        db.Collection("users")
          .Document(currentDeviceID)
          .UpdateAsync("GameStartCount", FieldValue.Increment(1));
    }

    // 최고 점수 저장 시
    public void SaveHighScore(int newHighScore)
    {
        if (loadedUserData == null || newHighScore <= loadedUserData.HighScore) return;

        loadedUserData.HighScore = newHighScore; // 로컬 데이터 갱신

        if (string.IsNullOrEmpty(currentDeviceID)) return;
        DocumentReference docRef = db.Collection("users").Document(currentDeviceID);
        docRef.UpdateAsync("HighScore", newHighScore);
    }
}

[FirestoreData]
public class UserData
{
    [FirestoreProperty]
    public string Nickname { get; set; }

    [FirestoreProperty]
    public string FirstStartTime { get; set; }

    [FirestoreProperty]
    public int HighScore { get; set; }

    [FirestoreProperty]
    public long GameOpenedCount { get; set; }

    [FirestoreProperty]
    public long GameStartCount { get; set; }
}