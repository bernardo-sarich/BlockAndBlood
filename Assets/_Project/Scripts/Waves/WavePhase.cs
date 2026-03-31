using System;
using UnityEngine;

/// <summary>
/// Defines the enemy mix and spawn rate for one time window of a run.
/// StartTime and EndTime are in seconds elapsed since the Playing state began.
/// Enemy type is selected probabilistically from Composition weights.
/// </summary>
[CreateAssetMenu(menuName = "Block&Blood/WavePhase", fileName = "NewWavePhase")]
public class WavePhase : ScriptableObject
{
    [Header("Active Window (seconds since Playing began)")]
    public float StartTime;
    public float EndTime;

    [Header("Spawn Rate")]
    [Tooltip("Seconds between each enemy spawn while this phase is active.")]
    public float SpawnInterval;

    [Header("Enemy Composition")]
    [Tooltip("Weights do NOT need to sum to 1 — selection is normalized automatically.")]
    public EnemySpawnWeight[] Composition;
}

/// <summary>
/// One entry in a WavePhase composition table.
/// Weight is relative (e.g. 3 + 1 = 75 % / 25 %).
/// </summary>
[Serializable]
public struct EnemySpawnWeight
{
    public EnemyData Data;
    [Min(0f)] public float Weight;
}
