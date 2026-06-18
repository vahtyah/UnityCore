using System;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Data/LevelData", order = 0)]
public class LevelData : ScriptableObject
{
    public LevelDetail levelDetail = new LevelDetail();

    
#if UNITY_EDITOR
    public LevelData Clone()
    {
        var clone = CreateInstance<LevelData>();
        UnityEditor.EditorUtility.CopySerialized(this, clone);
        return clone;
    }
#endif
}