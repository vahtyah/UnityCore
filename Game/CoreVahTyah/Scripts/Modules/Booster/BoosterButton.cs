using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VahTyah.Inspector;

namespace VahTyah
{
    public class BoosterButton : MonoBehaviour
    {
        [BoxGroup("Booster")]
        [Required("Booster key not set → the button is disabled (Activate is skipped).")]
        [Tooltip("Booster item key (matches a Key in ModuleBooster/ModuleItem).")]
        [SerializeField] private string _boosterKey;

        [BoxGroup("Booster")]
        [Tooltip("Invoked when the booster activates successfully (matching key). Hook your game action here (e.g. Undo()).")]
        public UnityEvent OnSuccess;

        [BoxGroup("States")]
        [SerializeField] private GameObject _normal;
        [BoxGroup("States")]
        [SerializeField] private GameObject _locked;
        [BoxGroup("States")]
        [SerializeField] private GameObject _max;
        [BoxGroup("States")]
        [SerializeField] private GameObject _free;
        [BoxGroup("States")]
        [SerializeField] private GameObject _icon;
        [BoxGroup("States")]
        [SerializeField] private GameObject _objectHave;
        [BoxGroup("States")]
        [SerializeField] private GameObject _plus;

        [BoxGroup("Labels")]
        [SerializeField] private TextMeshProUGUI _lockLabel;
        [BoxGroup("Labels")]
        [SerializeField] private string _lockPrefix = "LVL ";

        private void Awake()
        {
            var btn = GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(Activate);
        }

        private void OnEnable()
        {
            this.On<BoosterChanged>(e => { if (e.Key == _boosterKey) Refresh(); });
            this.On<BoosterActivated>(OnActivated);
            this.On<ItemChanged>(e => { if (e.Key == _boosterKey) Refresh(); });
            this.On<LevelLoadRequest>(_ => Refresh(), -51);
            Refresh();
        }

        private void OnActivated(BoosterActivated e)
        {
            if (e.Key != _boosterKey) return;
            Refresh();
            OnSuccess?.Invoke();
        }

        public void Activate()
        {
            if (string.IsNullOrEmpty(_boosterKey)) return;
            EventBus.Publish(new BoosterActivate { Key = _boosterKey }).Forget();
        }

        private void Refresh()
        {
            if (string.IsNullOrEmpty(_boosterKey)) return;

            BoosterState st = default;
            EventBus.Publish(new BoosterGetState { Key = _boosterKey, Reply = s => st = s }).Forget();
            if (!st.Exists) return;

            bool locked = st.Locked;
            bool maxed = st.Maxed && !locked;
            bool normal = !locked && !maxed;
            bool free = normal && st.Free;

            if (_locked != null) _locked.SetActive(locked);
            if (_icon != null) _icon.SetActive(!locked);
            if (_max != null) _max.SetActive(maxed);
            if (_normal != null) _normal.SetActive(normal && !free);
            if (_free != null) _free.SetActive(free);

            if (locked && _lockLabel != null)
                _lockLabel.SetText(_lockPrefix + st.UnlockLevel);

            bool payable = normal && !free;
            if (payable)
            {
                int have = GetItem(_boosterKey);
                if (_objectHave != null) _objectHave.SetActive(have > 0);
                if (_plus != null) _plus.SetActive(have <= 0);
            }
            else
            {
                if (_objectHave != null) _objectHave.SetActive(false);
                if (_plus != null) _plus.SetActive(false);
            }
        }

        private static int GetItem(string key)
        {
            int v = 0;
            EventBus.Publish(new ItemGet { Key = key, Reply = r => v = r }).Forget();
            return v;
        }
    }
}
