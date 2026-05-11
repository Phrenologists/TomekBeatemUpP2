using UnityEngine;

[CreateAssetMenu(menuName = "BeatEmUp/AttackMap")]
public class AttackMap : ScriptableObject
{
    [Header("Ground Attacks")]
    public AttackData lightAttackGround;
    public AttackData heavyAttackGround;
    public AttackData leftTriggerGround;
    public AttackData rightTriggerGround;

    [Header("Air Attacks")]
    public AttackData lightAttackAir;
    public AttackData heavyAttackAir;
    public AttackData leftTriggerAir;
    public AttackData rightTriggerAir;

    public AttackData GetAttack(string inputActionName, bool grounded)
    {
        if (grounded)
        {
            return inputActionName switch
            {
                "LightAttack" => lightAttackGround,
                "HeavyAttack" => heavyAttackGround,
                "LeftTrigger" => leftTriggerGround,
                "RightTrigger" => rightTriggerGround,
                _ => null
            };
        }
        else
        {
            return inputActionName switch
            {
                "LightAttack" => lightAttackAir,
                "HeavyAttack" => heavyAttackAir,
                "LeftTrigger" => leftTriggerAir,
                "RightTrigger" => rightTriggerAir,
                _ => null
            };
        }
    }
}
