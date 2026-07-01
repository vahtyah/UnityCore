using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    public sealed class SaveRunner : MonoBehaviour
    {
        private SaveService _service;
        private Coroutine _autoSave;

        public void Bind(SaveService service, float autoSaveInterval = 0f)
        {
            _service = service;

            if (autoSaveInterval > 0f)
                _autoSave = StartCoroutine(AutoSaveLoop(autoSaveInterval));
        }

        private IEnumerator AutoSaveLoop(float interval)
        {
            var wait = new WaitForSeconds(interval);
            while (true)
            {
                yield return wait;
                if (_service.IsDirty)
                    _service.SaveAllAsync().Forget();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _service != null)
                _service.SaveAllAsync().Forget();
        }

        private void OnApplicationQuit()
        {
            _service?.SaveAllAsync().Forget();
        }

        private void OnDestroy()
        {
            if (_autoSave != null)
                StopCoroutine(_autoSave);
        }
    }
}
