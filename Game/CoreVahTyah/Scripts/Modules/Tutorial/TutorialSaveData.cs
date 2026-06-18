using System;
using System.Collections.Generic;

namespace VahTyah
{
    [Serializable]
    public class TutorialSaveData
    {
        public List<int> CompletedLevels = new List<int>();

        public bool IsDone(int level) => CompletedLevels.Contains(level);

        public void MarkDone(int level)
        {
            if (!CompletedLevels.Contains(level))
                CompletedLevels.Add(level);
        }
    }
}
