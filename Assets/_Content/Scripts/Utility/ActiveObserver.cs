using System;
using UnityEngine;

public class ActiveObserver : MonoBehaviour
{
    public event Action OnEnableEvent;
    public event Action OnDisableEvent;

    private void OnEnable()
    {
        OnEnableEvent?.Invoke();
    }

    private void OnDisable()
    {
        OnDisableEvent?.Invoke();
    }
}

