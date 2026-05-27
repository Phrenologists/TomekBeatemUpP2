

public enum EnemyStateID
{
    Idle = 0,   // brief pause on spawn before brain takes over
    Wandering = 1,   // slow idle walk (brain: Wandering)
    Chasing = 2,   // moving toward player or flank position (brain: Pursuing)
    Windup = 3,   // attack telegraph
    Attacking = 4,   // active hit window
    Recovery = 5,   // post-attack, vulnerable
    Blocking = 6,   // block stance (brain: Blocking)
    Taunting = 7,   // taunt animation (brain: Taunting)
    Hurt = 8,   // stagger
    KnockedDown = 9,   // on the floor
    GetUp = 10,  // rising
    Dead = 11
}
