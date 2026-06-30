using UnityEngine;

[CreateAssetMenu(menuName = "Stage/WorldData")]
public class WorldData : ScriptableObject
{
    public string worldName;

    public StageData[] stages;
}