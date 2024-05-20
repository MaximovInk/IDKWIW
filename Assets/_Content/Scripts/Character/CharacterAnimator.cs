using System;
using Unity.Netcode;
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class CharacterAnimator : NetworkBehaviour
    {
        public Animator Animator => _animator;

        private Animator _animator;
        private CharacterController _controller;

        private const float StandardSmoothTransition = 0.1f;

        public bool IsPlayingState(string state) => _animator.GetCurrentAnimatorStateInfo(0).IsName(state);

        [ServerRpc(RequireOwnership = false)]
        public void PlayStateServerRPC(string state, int layer = 0, float smoothTransition = 0f)
        {
            var currentState = _animator.GetCurrentAnimatorStateInfo(layer);

            var nextState = _animator.GetNextAnimatorStateInfo(layer);

            if (currentState.IsName(state))
                return;

            if (nextState.IsName(state)) 
                return;

            if(smoothTransition < 0.01f)
                _animator.Play(state, layer);
            else
                _animator.CrossFade(state, smoothTransition, layer);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetBoolServerRPC(string name, bool value)
        {
            _animator.SetBool(name,value);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetFloatServerRPC(string name, float value)
        {
            _animator.SetFloat(name, value);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetSpeedPlaybackServerRPC(float value)
        {
            _animator.speed = value;
        }

        private void AirAnimate()
        {
            PlayStateServerRPC("FallingLoop" , smoothTransition: StandardSmoothTransition);
        }

        private void CrouchAnimate()
        {
            if (_controller.IsCrouch)
            {
                PlayStateServerRPC(_controller.IsMoving ? "CrouchWalk" : "CrouchIdle", smoothTransition: StandardSmoothTransition);
            }

        }

        private float _playbackSpeed = 1f;

        private void GroundAnimate()
        {
            if (_controller.IsCrouch)
            {
                CrouchAnimate();

                return;
            }

            if (_controller.IsMoving)
            {
                bool backward = _controller.CurrentInput.MoveValue.y < -0.5f && !_controller.Graphics.IsLookingToMove;

                PlayStateServerRPC(backward ? "RunBackward" : "RunForward", smoothTransition: StandardSmoothTransition);
            }
            else
            {
                //SpeedMultiplier
                PlayStateServerRPC("Idle", smoothTransition: StandardSmoothTransition);
            }

            if (Math.Abs(_playbackSpeed - _controller.SpeedMultiplier) > 0.01f)
            {
                _playbackSpeed = _controller.SpeedMultiplier;

                SetSpeedPlaybackServerRPC(_controller.SpeedMultiplier);
            }
        }

        private void ItemHandleAnimate()
        {
            if (_controller.WeaponSystem.IsAnyHandling)
            {
                switch (_controller.WeaponSystem.HandleType)
                {
                    case HandleType.None:
                        PlayStateServerRPC("Empty", 1, smoothTransition: StandardSmoothTransition);
                        break;
                    case HandleType.Default:

                        break;
                    case HandleType.Pistol:

                        break;
                    case HandleType.Rifle:
                        PlayStateServerRPC(_controller.Aim.IsAiming ? "RifleAim" : "RifleIdle", 1, smoothTransition: StandardSmoothTransition);


                        break;
                }
            }
        }

        public void UpdateAnimation()
        {
            if (!_controller.IsGround)
            {
                AirAnimate();
            }
            else
            {
                GroundAnimate();
            }

            ItemHandleAnimate();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _animator = GetComponentInChildren<Animator>();
            _controller = GetComponent<CharacterController>();
        }
    }
}
