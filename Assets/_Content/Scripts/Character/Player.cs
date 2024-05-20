
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class Player : CharacterController, IPlayer
    {

        protected override void Update()
        {
            if (!IsOwner) return;

            base.Update();

        }

        public PlayerInputType GetInputType()
        {
            return PlayerInputType.Character;
        }

        public void HandleInput(CharacterInput input)
        {
            ProcessInput(input);
        }
    }
}
