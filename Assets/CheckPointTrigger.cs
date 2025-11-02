using System;
using UnityEngine;

public class CheckPointTrigger : MonoBehaviour
{
    [SerializeField] private int checkPointNumber;

    public event Action<int> OnEnterCheckPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            OnEnterCheckPoint?.Invoke(checkPointNumber);
        }
    }
}