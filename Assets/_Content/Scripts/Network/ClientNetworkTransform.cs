using Unity.Netcode.Components;

namespace MaximovInk.IDKWIW.Network
{
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }

    }
}
