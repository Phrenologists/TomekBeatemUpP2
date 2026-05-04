using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackAnimationHandler : MonoBehaviour
{
    public AttackHitbox attackHitbox1;
    public AttackHitbox attackHitbox2;
    
    void StartActivateHitbox1()
    {
        attackHitbox1.ActivateHitbox();
    }

    void StartActivateHitbox2() 
    {
        attackHitbox2.ActivateHitbox();
    }

    private void StartDeactivateHitbox1()
    {
        attackHitbox1.DeactivateHitbox();
    }
    private void StartDeactivateHitbox2()
    {
        attackHitbox2.DeactivateHitbox();
    }

}
