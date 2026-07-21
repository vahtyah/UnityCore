#if UNITY_EDITOR
using UnityEngine;

public class ObjectEditor<T> : MonoBehaviour
    where T : Component
{
    private T _owner;

    public T Owner => _owner ??= GetComponent<T>();
}

#endif