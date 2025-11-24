using UnityEngine;

[CreateAssetMenu(fileName = "SkinData", menuName = "Scriptable Objects/SkinData")]
public class SkinData : ScriptableObject
{
    public int Index;
    public string Name;
    public string Context;
    public Material material;
}
