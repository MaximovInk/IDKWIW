using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public struct CharacterComponents
    {
        public readonly Camera Camera => CameraController.ActiveCamera;

        public CharacterController Controller;


        public CharacterGraphics Graphics;
        public CharacterAnimator Animator;
        public CameraController CameraController;
        public CharacterWeaponSystem WeaponSystem;

        public CharacterAim Aim;

        public readonly bool IsInitialized => Controller != null;
    }
}