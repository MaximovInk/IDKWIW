using UnityEngine;

namespace MaximovInk
{
    public interface IInteractable
    {
        public void Interact(MKCharacter character);

        public string GetInfoText();

        public Vector3 GetPosition();
    }
}
