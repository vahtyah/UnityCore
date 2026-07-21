using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Config/GameConfig", order = 0)]
public class GameConfig : ScriptableObject
{
    public LevelDatabaseConfig LevelDatabaseConfig;
}