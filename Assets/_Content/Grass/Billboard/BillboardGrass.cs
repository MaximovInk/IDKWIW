using System;
using MaximovInk.IDKWIW;
using UnityEngine;

public class BillboardGrass : MonoBehaviour
{
    private Camera _targetCamera;
    private LODGroup _lodGroup;

    private bool _isVisible;

    private void Awake()
    {
        _targetCamera = Camera.main;

        GameManager.Instance.OnCameraChanged += Instance_OnCameraChanged;

        _isVisible = GetComponent<Renderer>().isVisible;
    }

    private void Instance_OnCameraChanged(Camera obj)
    {
        _targetCamera = obj;
    }

    private void Update()
    {
        if(!_isVisible)return;

        Vector3 targetForward = _targetCamera.transform.forward;
        targetForward.y = 0.01f;   // Not zero to avoid issues.
        transform.rotation = Quaternion.LookRotation(targetForward.normalized, Vector3.up);
    }

    private void OnBecameInvisible()
    {
        _isVisible = false;
    }

    private void OnBecameVisible()
    {
        _isVisible = true;
    }
}
