// DEPENDENCIES:
//   DOTween
//   AttackHitbox  — child GameObject with BoxCollider2D trigger
//   Animator      — optional, plays attack animation via trigger

using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class StrikerController : MonoBehaviour
{
    
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private AttackHitbox hitbox;

    private StrikerData _data;
    private Sequence _sequence;
    private bool _isActive;

    private static readonly int AnimAttackTrigger = Animator.StringToHash("attack");

    public void Activate(StrikerData data, Vector2 spawnPosition, bool isLB, Action onComplete = null)
    {
        if (_isActive) return;

        _data = data;
        _isActive = true;

        transform.SetParent(null);

        Vector2 entryStart = spawnPosition + data.entryStartOffset;
        transform.position = new Vector3(entryStart.x, entryStart.y, 0f);
        gameObject.SetActive(true);

        Vector3 scale = transform.localScale;
        scale.x = data.faceLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;

        if (hitbox != null) hitbox.DeactivateHitbox();

        AttackData attack = data.GetAttack(isLB);

        _sequence = BuildSequence(spawnPosition, attack, onComplete);
        _sequence.Play();
    }


    public void ForceCancel()
    {
        _sequence?.Kill();
        Deactivate();
    }


    private Sequence BuildSequence(Vector2 spawnPos, AttackData attack, Action onComplete)
    {
        Sequence seq = DOTween.Sequence();

        if (_data.flashOnEntry)
        {
            seq.Append(spriteRenderer.DOFade(0f, 0f));
            float flashStep = _data.entryDuration / (_data.flashCount * 2f);
            for (int i = 0; i < _data.flashCount; i++)
            {
                seq.Append(spriteRenderer.DOFade(1f, flashStep));
                seq.Append(spriteRenderer.DOFade(0f, flashStep));
            }
            seq.Append(spriteRenderer.DOFade(1f, 0f));
            seq.AppendCallback(() =>
                transform.position = new Vector3(spawnPos.x, spawnPos.y, 0f));
        }
        else
        {
            seq.Append(
                transform.DOMoveX(spawnPos.x, _data.entryDuration)
                         .SetEase(_data.entryEase)
            );
        }

        if (_data.preAttackDelay > 0f)
            seq.AppendInterval(_data.preAttackDelay);

        seq.AppendCallback(() => StartAttack(attack));

        float attackWait = attack != null ? attack.TotalDuration : 0.5f;
        seq.AppendInterval(attackWait);

        Vector2 exitTarget = spawnPos + _data.exitEndOffset;
        seq.Append(
            transform.DOMoveX(exitTarget.x, _data.exitDuration)
                     .SetEase(_data.exitEase)
        );

        seq.AppendCallback(() =>
        {
            Deactivate();
            onComplete?.Invoke();
        });

        return seq;
    }



    private void StartAttack(AttackData attack)
    {
        if (attack == null) return;

        if (animator != null && !string.IsNullOrEmpty(attack.animTriggerName))
            animator.SetTrigger(Animator.StringToHash(attack.animTriggerName));

        StartCoroutine(RunAttackPhases(attack));
    }

    private IEnumerator RunAttackPhases(AttackData attack)
    {
        yield return new WaitForSeconds(attack.StartupDuration);

        if (hitbox != null)
        {
            hitbox.damage = attack.damage;
            hitbox.causesKnockdown = attack.causesKnockdown;

            Vector2 kb = attack.knockback;
            kb.x = _data.faceLeft ? -Mathf.Abs(kb.x) : Mathf.Abs(kb.x);
            hitbox.knockback = kb;

            Vector2 offset = attack.hitboxOffset;
            if (_data.faceLeft) offset.x = -offset.x;
            hitbox.transform.localPosition = new Vector3(offset.x, offset.y, 0f);

            var col = hitbox.GetComponent<BoxCollider2D>();
            if (col != null) col.size = attack.hitboxSize;

            hitbox.ActivateHitbox();
        }

        yield return new WaitForSeconds(attack.ActiveDuration);

        if (hitbox != null) hitbox.DeactivateHitbox();

    }



    private void Deactivate()
    {
        StopAllCoroutines();
        if (hitbox != null) hitbox.DeactivateHitbox();
        _isActive = false;
        gameObject.SetActive(false);
    }
}
