using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    public abstract class Tutorial : MonoBehaviour
    {
        public abstract void StartTutorial();

        protected void Finish() => EventBus.Publish(new TutorialFinished()).Forget();

        public virtual void Dispose() { }
    }
}
