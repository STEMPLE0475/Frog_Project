using System.Collections.Generic;
using UnityEngine;

public class SeaManager : MonoBehaviour
{
    [SerializeField] private List<Sea> seas;
    [SerializeField] private float seaSpeed = 5.0f;

    public void Initiate() { }

    public void SetSeaSpeed(Wind wind)
    {
        float speed = wind.power * wind.direction * seaSpeed;
        foreach (var sea in seas)
        {
            sea.SetSpeed(speed);
        }
    }
}
