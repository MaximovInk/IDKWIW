
using UnityEngine;

namespace MaximovInk
{
public class CarCamera : BaseCameraController
{
    CameraLookInput input;
    [SerializeField] private Vector2 _mouseSens = Vector2.one;

    private void Update()
    {
        input.LookAround = Input.GetKey(KeyCode.LeftAlt);
        input.InvokeChangeCamera = Input.GetKeyDown(KeyCode.C);

        input.LookValue = new Vector2(Input.GetAxisRaw("Mouse X") * _mouseSens.x, -Input.GetAxisRaw("Mouse Y") * _mouseSens.y);

        input.ScrollDelta -= Input.mouseScrollDelta.y;

        HandleInput(input);
    }

    protected override bool RotateFirstPersonY()
    {
        return true;
    }
}


}