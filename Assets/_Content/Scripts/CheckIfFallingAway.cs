using System;
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class CheckIfFallingAway : MonoBehaviour
    {
        private const float _deadY = -100f;

        private void Update()
        {
            if (transform.position.y < _deadY)
            {
                var pos = transform.position;

                pos.y = 100;

                transform.position = pos;
            }
        }
    }
}
