//Uses new input system

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public bool JumpPressed { get; private set; }
    //public bool AttackPressed { get; private set; }
    public bool DashPressed { get; private set; }
    //public bool SpecialPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool LightAttackPressed { get; private set; }
    public bool HeavyAttackPressed { get; private set; }
    public bool LeftTriggerPressed { get; private set; }
    public bool RightTriggerPressed { get; private set; }
    public string LastAttackActionPressed { get; private set; }



    //private bool _jumpConsumed;
    //private bool _attackConsumed;
    //private bool _dashConsumed;
    //private bool _specialConsumed;


    public void OnMove(InputAction.CallbackContext ctx)
    {
        MoveInput = ctx.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.started) { JumpPressed = true; JumpHeld = true; }
        if (ctx.canceled) { JumpHeld = false; }
    }
    public bool AnyAttackPressed =>
    LightAttackPressed || HeavyAttackPressed ||
    LeftTriggerPressed || RightTriggerPressed;

    /*
    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (ctx.started) AttackPressed = true;
    }
    */

    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (ctx.started) DashPressed = true;
    }

    /*
    public void OnSpecial(InputAction.CallbackContext ctx)
    {
        if (ctx.started) SpecialPressed = true;
    }
    */

    public void OnLightAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;
        LightAttackPressed = true;
        LastAttackActionPressed = "LightAttack";
    }

    public void OnHeavyAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;
        HeavyAttackPressed = true;
        LastAttackActionPressed = "HeavyAttack";
    }

    public void OnLeftTrigger(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;
        LeftTriggerPressed = true;
        LastAttackActionPressed = "LeftTrigger";
    }

    public void OnRightTrigger(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;
        RightTriggerPressed = true;
        LastAttackActionPressed = "RightTrigger";
    }
    public void ConsumeFrameInputs()
    {
        JumpPressed = false;
        //AttackPressed = false;
        DashPressed = false;
        //SpecialPressed = false;
        LightAttackPressed = false;
        HeavyAttackPressed = false;
        LeftTriggerPressed = false;
        RightTriggerPressed = false;
        LastAttackActionPressed = null;
    }
}
