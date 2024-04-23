using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public struct CharacterInput
    {
        public Vector2 MoveValue;
        public Vector2 LookValue;

        public bool IsSprint;
        public bool IsCrouch;

        public bool IsInvokeJump;

        public bool InvokeChangeCamera;
        public bool LookAround;

        public float ScrollDelta;

        public bool IsAiming;
        public bool IsFire;
    }
}