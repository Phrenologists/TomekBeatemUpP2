
using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [Header("Attack Data")]
    public int damage = 15;

    public Vector2 knockback = new Vector2(4f, 3f);

    public bool causesKnockdown = false;

    [Header("References")]
    public PlayerController owner;

    // Enemies that were already hit
    private readonly HashSet<Collider2D> _hitTargets = new HashSet<Collider2D>();

    public void ActivateHitbox()
    {
        _hitTargets.Clear();
        gameObject.SetActive(true);
    }

    public void DeactivateHitbox()
    {
        gameObject.SetActive(false);
    }

    //Collision handling

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore if target was already hit
        if (_hitTargets.Contains(other)) return;

        //if (!other.CompareTag("Enemy")) return;

        _hitTargets.Add(other);

        Vector2 kb = knockback;
        if (owner != null)
        {
            bool ownerFacingRight = owner.transform.localScale.x > 0;
            kb.x = ownerFacingRight ? Mathf.Abs(kb.x) : -Mathf.Abs(kb.x);
        }

        // Tries to apply damage to the target
        if (other.CompareTag("Enemy"))
        {
            //Debug.Log("Hit enemy");
            var enemy = other.GetComponent<IDamageable>();
            enemy?.TakeDamage(damage, kb, causesKnockdown);
        }
        if(other.CompareTag("Player"))
        {
            //Debug.Log("Hit player");
            var player = other.GetComponent<IDamageable>();
            PlayerController playerController = other.GetComponent<PlayerController>();
            playerController?.TakeDamage(damage, kb, causesKnockdown);
        }
    }
}

//Interface for anything that can get hit
public interface IDamageable
{
    void TakeDamage(int amount, Vector2 knockback, bool knockdown = false);
}
