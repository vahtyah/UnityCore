using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VahTyah.Inspector;

namespace VahTyah
{
    [CreateAssetMenu(menuName = "VahTyah/Modules/Tutorial", fileName = "Module_Tutorial")]
    [ModuleRequires(typeof(ModuleSave))]
    public sealed class ModuleTutorial : Module
    {
        [Serializable]
        public class LevelTutorial
        {
            public int Level = 1;
            public GameObject Prefab;
        }

        [BoxGroup("Tutorials")]
        [SerializeField] private List<LevelTutorial> _tutorials = new List<LevelTutorial>();

        private const string SaveKey = "tutorial";

        private TutorialSaveData _save;
        private GameObject _current;
        private int _currentLevel;

        public override UniTask InitializeAsync(Transform holder)
        {
            _save = Services.Get<SaveService>().Load<TutorialSaveData>(SaveKey);
            return UniTask.CompletedTask;
        }

        public override void Subscribe()
        {
            EventBus.On<LevelStarted>(OnLevelStarted);
            EventBus.On<TutorialFinished>(OnFinished);
        }

        private void OnLevelStarted(LevelStarted e)
        {
            int level = 0;
            EventBus.Publish(new LevelGet { Reply = v => level = v }).Forget();
            TryStart(level);
        }

        private void TryStart(int level)
        {
            if (_save.IsDone(level)) return;

            var prefab = Find(level);
            if (prefab == null) return;

            _currentLevel = level;
            _current = UnityEngine.Object.Instantiate(prefab); // vào scene gameplay, tự dọn khi đổi scene

            var tut = _current.GetComponent<Tutorial>();
            if (tut == null)
            {
                Debug.LogError($"[Tutorial] Prefab '{prefab.name}' thiếu component Tutorial.");
                UnityEngine.Object.Destroy(_current);
                _current = null;
                return;
            }

            tut.StartTutorial();
        }

        private void OnFinished(TutorialFinished e)
        {
            if (_current != null)
            {
                var tut = _current.GetComponent<Tutorial>();
                if (tut != null) tut.Dispose();
                UnityEngine.Object.Destroy(_current);
                _current = null;
            }

            _save.MarkDone(_currentLevel);
            Services.Get<SaveService>().Set(SaveKey, _save);
        }

        private GameObject Find(int level)
        {
            for (int i = 0; i < _tutorials.Count; i++)
            {
                if (_tutorials[i].Level == level && _tutorials[i].Prefab != null)
                    return _tutorials[i].Prefab;
            }
            return null;
        }
    }
}
