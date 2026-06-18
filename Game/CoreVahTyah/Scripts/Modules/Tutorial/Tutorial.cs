using UnityEngine;

namespace VahTyah
{
    public abstract class Tutorial : MonoBehaviour
    {
        public abstract void StartTutorial();

        protected void Finish() => EventBus.Publish(new TutorialFinished());

        public virtual void Dispose() { }
    }
}
