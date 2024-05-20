using MaximovInk.IDKWIW;
namespace MaximovInk
{
    public enum PlayerInputType
    {
        None,
        Vehicle,
        Character
    }


    public interface IPlayer
    {
        PlayerInputType GetInputType();

        void HandleInput(CharacterInput input);

    }
}
