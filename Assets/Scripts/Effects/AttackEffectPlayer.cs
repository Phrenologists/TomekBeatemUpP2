using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackEffectPlayer : MonoBehaviour
{
    [Header("Attachment Points")]
    [Tooltip("Named slots effects can be placed at. " +
             "Drag child Transforms from the hierarchy into each Point field.")]
    [SerializeField] private List<AttachmentPoint> attachmentPoints = new List<AttachmentPoint>();

    private readonly List<ActiveEffectInstance> _active = new List<ActiveEffectInstance>();

    private readonly Dictionary<GameObject, Queue<GameObject>> _pool
        = new Dictionary<GameObject, Queue<GameObject>>();

    private readonly List<Coroutine> _pendingTriggers = new List<Coroutine>();


    public void TickEffects(
        List<AttackEffectEntry> effects,
        AttackPhase currentPhase,
        float phaseElapsed,
        bool facingRight,
        HashSet<int> alreadyTriggered)
    {
        if (effects == null) return;

        for (int i = 0; i < effects.Count; i++)
        {
            if (alreadyTriggered.Contains(i)) continue;

            var entry = effects[i];
            if (entry?.effectData == null) continue;
            if (entry.triggerPhase != currentPhase) continue;
            if (phaseElapsed < entry.delayWithinPhase) continue;

            SpawnEffect(entry, facingRight);
            alreadyTriggered.Add(i);
        }
    }
    public void TriggerEffectsForPhase(
        List<AttackEffectEntry> effects,
        AttackPhase phase,
        bool facingRight)
    {
        if (effects == null) return;

        foreach (var entry in effects)
        {
            if (entry?.effectData == null) continue;
            if (entry.triggerPhase != phase) continue;

            if (entry.delayWithinPhase <= 0f)
            {
                SpawnEffect(entry, facingRight);
            }
            else
            {
                var c = StartCoroutine(DelayedSpawn(entry, facingRight, entry.delayWithinPhase));
                _pendingTriggers.Add(c);
            }
        }
    }

    public void CancelAttackBoundEffects()
    {
        foreach (var c in _pendingTriggers)
            if (c != null) StopCoroutine(c);
        _pendingTriggers.Clear();

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i].cancelWithAttack)
            {
                ReturnToPool(_active[i]);
                _active.RemoveAt(i);
            }
        }
    }

    public void CancelAllEffects()
    {
        foreach (var c in _pendingTriggers)
            if (c != null) StopCoroutine(c);
        _pendingTriggers.Clear();

        foreach (var fx in _active)
            ReturnToPool(fx);
        _active.Clear();
    }


    private void Update()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var fx = _active[i];

            if (fx.infiniteDuration) continue;

            fx.remainingLifetime -= Time.deltaTime;
            if (fx.remainingLifetime <= 0f)
            {
                ReturnToPool(fx);
                _active.RemoveAt(i);
            }
        }
    }


    private void SpawnEffect(AttackEffectEntry entry, bool facingRight)
    {
        if (entry.effectData.prefab == null)
        {
            Debug.LogWarning($"AttackEffectPlayer: effect '{entry.effectData.effectName}' " +
                             $"has no prefab assigned.", this);
            return;
        }

        Transform anchor = ResolveAttachmentPoint(entry.attachmentPointName);

        GameObject instance = GetFromPool(entry.effectData.prefab);
        instance.SetActive(true);

        instance.transform.SetParent(anchor, worldPositionStays: false);

        Vector2 offset = entry.localOffset;
        if (entry.inheritFacing && !facingRight)
            offset.x = -offset.x;

        instance.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
        instance.transform.localRotation = Quaternion.identity;

        ApplySortingOrder(instance, entry.effectData.sortingOrderOffset);

        if (!entry.cancelWithAttack)
            instance.transform.SetParent(null, worldPositionStays: true);

        bool infinite = entry.effectData.loop && entry.cancelWithAttack;
        float lifetime = entry.effectData.duration > 0f
            ? entry.effectData.duration
            : GetAnimationLength(instance);

        _active.Add(new ActiveEffectInstance
        {
            instance = instance,
            prefab = entry.effectData.prefab,
            cancelWithAttack = entry.cancelWithAttack,
            remainingLifetime = lifetime,
            infiniteDuration = infinite
        });
    }

    private IEnumerator DelayedSpawn(AttackEffectEntry entry, bool facingRight, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnEffect(entry, facingRight);
    }


    private GameObject GetFromPool(GameObject prefab)
    {
        if (_pool.TryGetValue(prefab, out var queue) && queue.Count > 0)
        {
            var obj = queue.Dequeue();
            if (obj != null) return obj;
        }

        return Instantiate(prefab);
    }

    private void ReturnToPool(ActiveEffectInstance fx)
    {
        if (fx.instance == null) return;

        fx.instance.SetActive(false);
        fx.instance.transform.SetParent(transform, worldPositionStays: false);

        if (!_pool.ContainsKey(fx.prefab))
            _pool[fx.prefab] = new Queue<GameObject>();

        _pool[fx.prefab].Enqueue(fx.instance);
    }


    private Transform ResolveAttachmentPoint(string pointName)
    {
        if (!string.IsNullOrEmpty(pointName))
        {
            foreach (var ap in attachmentPoints)
                if (ap.pointName == pointName && ap.point != null)
                    return ap.point;

            Debug.LogWarning($"AttackEffectPlayer: attachment point '{pointName}' not found. " +
                             $"Falling back to character root.", this);
        }

        return transform;
    }

    private void ApplySortingOrder(GameObject instance, int offset)
    {
        var characterRenderer = GetComponentInChildren<SpriteRenderer>();
        int baseOrder = characterRenderer != null ? characterRenderer.sortingOrder : 0;

        foreach (var sr in instance.GetComponentsInChildren<SpriteRenderer>())
            sr.sortingOrder = baseOrder + offset;
    }

    private float GetAnimationLength(GameObject instance)
    {
        var anim = instance.GetComponent<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            var clips = anim.runtimeAnimatorController.animationClips;
            if (clips != null && clips.Length > 0)
                return clips[0].length;
        }
        return 1f;
    }

    private class ActiveEffectInstance
    {
        public GameObject instance;
        public GameObject prefab;
        public bool cancelWithAttack;
        public float remainingLifetime;
        public bool infiniteDuration;
    }
}
