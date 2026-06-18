using System.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    public abstract class Module : ScriptableObject
    {
        public virtual Task InitializeAsync(Transform holder) => Task.CompletedTask;
        public virtual void Subscribe() { }
        public virtual void Unsubscribe() { }
    }
}
