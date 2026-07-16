using UnityEngine;

[CreateAssetMenu(fileName = "LevelDatabaseConfig", menuName = "Config/Level/LevelDatabaseConfig", order = 0)]
public class LevelDatabaseConfig : ScriptableObject
{
    public LevelData[] Levels;
}