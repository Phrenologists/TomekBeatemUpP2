// ArenaEncounterData.cs
// ScriptableObject that defines one enemy encounter — a sequence of waves,
// each containing a list of enemies to spawn.
// Create via Assets > Create > FightNRage > ArenaEncounterData
//
// WAVE FLOW:
//   Waves run in order. Each wave can complete either when all its enemies
//   are defeated (AllDefeated) or after a fixed delay (Delay — enemies from
//   the previous wave may still be alive when the next wave begins).
//
// SPAWN POSITIONS:
//   Each entry defines which side of the screen the enemy enters from
//   (Left or Right) and what depth position (targetGroundY) they move to.
//   Enemies are instantiated just off-screen and walk in naturally via their
//   EnemyAIBrain — no special entrance scripting needed.
//
// ENTRY DELAY:
//   entryDelay staggers enemies within a wave so they don't all appear at
//   the same frame. A stagger of 0.3–0.5s between entries feels natural.

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "FightNRage/ArenaEncounterData")]
public class ArenaEncounterData : ScriptableObject
{
    [Tooltip("Waves in the order they should run. Each wave must complete " +
             "before the next begins (see WaveCompletionMode per wave).")]
    public List<EncounterWave> waves = new List<EncounterWave>();

    [Tooltip("If true, the camera unlocks as soon as the final wave is cleared. " +
             "If false, camera stays locked until manually unlocked (e.g. scripted event).")]
    public bool unlockCameraOnComplete = true;
}

// ── Wave ──────────────────────────────────────────────────────────────────────

[Serializable]
public class EncounterWave
{
    [Tooltip("Human-readable label for this wave in the inspector.")]
    public string waveName = "Wave";

    [Tooltip("Enemies to spawn in this wave.")]
    public List<EnemySpawnEntry> enemies = new List<EnemySpawnEntry>();

    [Tooltip("When should the next wave begin?\n" +
             "AllDefeated — wait until every enemy in this wave is dead.\n" +
             "Delay        — wait a fixed number of seconds then start the next " +
             "wave regardless (enemies from this wave may still be alive).")]
    public WaveCompletionMode completionMode = WaveCompletionMode.AllDefeated;

    [Tooltip("Seconds to wait before starting the next wave. " +
             "Only used when completionMode = Delay.")]
    [Min(0f)] public float delayBeforeNextWave = 2f;
}

// ── Spawn entry ───────────────────────────────────────────────────────────────

[Serializable]
public class EnemySpawnEntry
{
    [Tooltip("Enemy prefab to instantiate. Must have EnemyController and EnemyAIBrain.")]
    public GameObject prefab;

    [Tooltip("Which side of the screen the enemy enters from.")]
    public SpawnSide spawnSide = SpawnSide.Right;

    [Tooltip("How far outside the camera edge to spawn the enemy (world units). " +
             "Larger values give the player more warning; smaller values feel more sudden.")]
    [Min(0.5f)] public float offscreenBuffer = 2f;

    [Tooltip("The depth (Y) position the enemy targets. Should be within the " +
             "camera's visible depth lane. 0 = centre lane.")]
    public float targetGroundY = 0f;

    [Tooltip("Seconds after the wave starts before this enemy is spawned. " +
             "Use this to stagger entries — e.g. 0, 0.4, 0.8 for three enemies " +
             "entering at different times.")]
    [Min(0f)] public float entryDelay = 0f;
}

// ── Enums ─────────────────────────────────────────────────────────────────────

public enum WaveCompletionMode
{
    AllDefeated,   // next wave starts only when all enemies in this wave are dead
    Delay          // next wave starts after delayBeforeNextWave seconds
}

public enum SpawnSide
{
    Left,
    Right
}
