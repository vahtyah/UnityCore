using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    public abstract class Module : ScriptableObject
    {
        public virtual UniTask InitializeAsync(Transform holder) => UniTask.CompletedTask;
        public virtual void Subscribe() { }
        public virtual void Unsubscribe() { }
    }
}
