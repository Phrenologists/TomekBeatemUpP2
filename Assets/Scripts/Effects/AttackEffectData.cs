using System;
using UnityEngine;

[CreateAssetMenu(menuName = "BeatEmUp/AttackEffectData")]
public class AttackEffectData : ScriptableObject
{
    [Tooltip("Human-readable name for inspector and debug overlays.")]
    public string effectName = "Effect";

    [Header("Prefab")]
    [Tooltip("GameObject with SpriteRenderer + Animator. " +
             "The Animator should have a single default state that plays automatically.")]
    public GameObject prefab;

    [Header("Lifetime")]
    [Tooltip("How long the effect lives in seconds. " +
             "Set to 0 to let the animation play to natural completion " +
             "(requires an AnimationClip with no loop and known length).")]
    [Min(0f)] public float duration = 0.5f;

    [Tooltip("If true, the effect loops until cancelled. " +
             "Useful for charging effects that should persist for the whole startup phase.")]
    public bool loop = false;

    [Header("Rendering")]
    [Tooltip("Sorting order offset added on top of the character sprite's current order. " +
             "Positive = renders in front, negative = behind.")]
    public int sortingOrderOffset = 1;
}


[Serializable]
public class AttackEffectEntry
{
    [Tooltip("The effect asset to play.")]
    public AttackEffectData effectData;

    [Tooltip("Which attack phase triggers this effect.")]
    public AttackPhase triggerPhase = AttackPhase.Active;

    [Tooltip("Seconds after the phase starts before the effect fires. " +
             "0 = fires on the first frame of the phase.")]
    [Min(0f)] public float delayWithinPhase = 0f;

    [Tooltip("Name of the AttachmentPoint slot on the character's AttackEffectPlayer. " +
             "Must match exactly. Leave empty to attach to the character root.")]
    public string attachmentPointName = "";

    [Tooltip("Additional offset in local space of the attachment point.")]
    public Vector2 localOffset = Vector2.zero;

    [Tooltip("If true, localOffset.x is flipped when the character faces left, " +
             "so the effect always appears in front of the character.")]
    public bool inheritFacing = true;

    [Tooltip("If true, this effect is deactivated when the attack is cancelled or ends. " +
             "If false, the effect plays to its full duration regardless of what the " +
             "player does — use this for projectiles or environmental effects that " +
             "should persist after the player gets hit.")]
    public bool cancelWithAttack = true;
}

[Serializable]
public class AttachmentPoint
{
    [Tooltip("Name referenced by AttackEffectEntry.attachmentPointName.")]
    public string pointName;

    [Tooltip("The child Transform to attach effects to. Drag from the hierarchy.")]
    public Transform point;
}
