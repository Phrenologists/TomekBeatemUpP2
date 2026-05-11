
public enum AttackPhase
{
    Startup,    // animation begins, no hitbox yet
    Active,     // hitbox is drawn, can deal damage
    Recovery    // hitbox gone, combo input window is open
}
