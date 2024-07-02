using System;
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class CheckIfFallingAway : MonoBehaviour
    {
        private const float _deadY = -100f;

        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb == null)
                _rb = GetComponentInChildren<Rigidbody>();

        }

        private void Update()
        {
            if (transform.position.y < _deadY)
            {
                var pos = transform.position;

                pos.y = 100;

                transform.position = pos;

                if (_rb != null)
                    _rb.velocity = Vector3.zero;
            }
        }
    }
}
