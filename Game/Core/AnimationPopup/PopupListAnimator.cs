using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Phát popup theo từng nhóm GameObject một cách tuần tự, có delay và điểm chờ tín hiệu.
    /// </summary>
    public class PopupListAnimator : MonoBehaviour
    {
        /// <summary>Một nhóm object được kích hoạt cùng lúc trong chuỗi.</summary>
        [Serializable]
        public class PopupGroup
        {
            [Tooltip("All objects in this group activate at the same time.")]
            public List<GameObject> objects = new List<GameObject>();

            [Tooltip("Seconds to wait after this group before starting the next one.")]
            public float delay = 0.2f;

            [Tooltip("Pause after this group until Continue(id) is called with this id. Leave empty to not wait.")]
            public string waitForId = "";
        }

        [SerializeField]
        private List<PopupGroup> groups = new List<PopupGroup>();

        [Tooltip("Automatically run the sequence when this GameObject is enabled.")]
        [SerializeField]
        private bool autoStartOnEnable = true;

        [Tooltip("Use unscaled (real) time for delays.")]
        [SerializeField]
        private bool ignoreTimeScale = false;

        private Coroutine _activeCoroutine;
        private readonly HashSet<string> _triggeredIds = new HashSet<string>();

        private void OnEnable()
        {
            _triggeredIds.Clear();
            if (autoStartOnEnable)
            {
                HideAll();
                PlayAll();
            }
        }

        private void OnDisable()
        {
            StopSequence();
        }

        public void PlayAll()
        {
            StopSequence();
            _activeCoroutine = StartCoroutine(PlaySequenceRoutine());
        }

        public void Continue(string id)
        {
            _triggeredIds.Add(id);
        }

        public void HideAll()
        {
            StopSequence();
            foreach (PopupGroup group in groups)
            {
                foreach (GameObject @object in group.objects)
                {
                    if (@object != null)
                    {
                        @object.SetActive(false);
                    }
                }
            }
        }

        private IEnumerator PlaySequenceRoutine()
        {
            foreach (PopupGroup group in groups)
            {
                if (group.delay > 0f)
                {
                    if (ignoreTimeScale)
                    {
                        yield return new WaitForSecondsRealtime(group.delay);
                    }
                    else
                    {
                        yield return new WaitForSeconds(group.delay);
                    }
                }

                // Chờ tín hiệu Continue(id) nếu nhóm có waitForId.
                if (!string.IsNullOrEmpty(group.waitForId))
                {
                    yield return new WaitUntil(() => _triggeredIds.Contains(group.waitForId));
                    _triggeredIds.Remove(group.waitForId);
                }

                foreach (GameObject obj in group.objects)
                {
                    if (obj != null)
                    {
                        // Tắt rồi bật lại để kích hoạt popup OnEnable.
                        obj.SetActive(false);
                        obj.SetActive(true);
                        AnimationPopup popup = obj.GetComponent<AnimationPopup>();
                        if (popup != null && !popup.PlayOnEnable)
                        {
                            popup.PlayPopup();
                        }
                    }
                }
            }
            _activeCoroutine = null;
        }

        private void StopSequence()
        {
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }
        }
    }
}
