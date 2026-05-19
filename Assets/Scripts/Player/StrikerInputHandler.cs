using UnityEngine;
using UnityEngine.InputSystem;

public class StrikerInputHandler : MonoBehaviour
{

    public Vector2 DpadInput { get; private set; }

    public bool LBPressed { get; private set; }

    public bool RBPressed { get; private set; }

    public bool AnyCallPressed => LBPressed || RBPressed;


    public void OnStrikerSelect(InputAction.CallbackContext ctx)
    {
        if (ctx.performed || ctx.started)
            DpadInput = ctx.ReadValue<Vector2>();
        if (ctx.canceled)
            DpadInput = Vector2.zero;
    }

    public void OnStrikerCallLB(InputAction.CallbackContext ctx)
    {
        if (ctx.started) LBPressed = true;
    }

    public void OnStrikerCallRB(InputAction.CallbackContext ctx)
    {
        if (ctx.started) RBPressed = true;
    }


    public void ConsumeFrameInputs()
    {
        LBPressed = false;
        RBPressed = false;
    }

    public int DpadToSlotIndex()
    {
        if (DpadInput.sqrMagnitude < 0.5f) return -1;

        if (Mathf.Abs(DpadInput.y) >= Mathf.Abs(DpadInput.x))
            return DpadInput.y > 0f ? 0 : 2;   // Up = 0, Down = 2
        else
            return DpadInput.x > 0f ? 1 : 3;   // Right = 1, Left = 3
    }
}
