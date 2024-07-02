using System;

namespace MaximovInk
{
    public class MKCharacterManager : MonoBehaviourSingleton<MKCharacterManager>
    {
        public event Action <MKCharacter> OnInitialized;

        public MKCharacter Current
        {
            get => _current;
            set
            {
                
                _current = value;

                if (value != null)
                {
                    OnInitialized?.Invoke(value);
                }
            }
        }

        public bool IsValid => _current != null;

        private MKCharacter _current;
    }
}
