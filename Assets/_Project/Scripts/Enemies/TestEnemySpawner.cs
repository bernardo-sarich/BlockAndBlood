using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Minimal test spawner for TASK-02 validation.
/// Spawns enemy instances at the grid's spawn row pointing to goal row.
/// Press E to spawn one enemy. Auto-spawns every _autoSpawnRate seconds if > 0.
/// This component is for playtesting only — WaveManager will replace it in TASK-06.
/// </summary>
public class TestEnemySpawner : MonoBehaviour
{
    [Header("Enemy Config")]
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private EnemyData  _enemyData;

    [Header("Spawn Settings")]
    [SerializeField] private float _autoSpawnRate = 0f; // 0 = manual only
    [SerializeField] private int   _spawnColumn   = 7;  // which grid column (0..Columns-1)

    private float _autoSpawnTimer;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            SpawnEnemy();

        if (_autoSpawnRate > 0f)
        {
            _autoSpawnTimer -= Time.deltaTime;
            if (_autoSpawnTimer <= 0f)
            {
                SpawnEnemy();
                _autoSpawnTimer = _autoSpawnRate;
            }
        }
    }

    private void SpawnEnemy()
    {
        if (_enemyPrefab == null || _enemyData == null || GridManager.Instance == null) return;

        // Spawn at the spawn row cell (must be ON the A* graph for pathfinding)
        // FIX: was hardcoded to 0–6 from old 7-col grid
        int col = Mathf.Clamp(_spawnColumn, 0, GridManager.Columns - 1);
        Vector3 spawnPos = GridManager.Instance.CellToWorld(new Vector2Int(col, GridManager.SpawnRow));
        Vector3 goalPos  = GridManager.Instance.CellToWorld(new Vector2Int(col, GridManager.GoalRow));
        spawnPos.z = 0f;
        goalPos.z  = 0f;

        GameObject go = Instantiate(_enemyPrefab, spawnPos, Quaternion.identity);
        if (go.TryGetComponent<EnemyBehaviour>(out var enemy))
            enemy.Initialize(_enemyData, spawnPos, goalPos);
        else
            Destroy(go);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (GridManager.Instance == null) return;
        Gizmos.color = Color.red;
        // FIX: was hardcoded to rows 0 and 8 from old 9-row grid
        Vector3 spawn = GridManager.Instance.CellToWorld(new Vector2Int(_spawnColumn, GridManager.SpawnRow));
        Vector3 goal  = GridManager.Instance.CellToWorld(new Vector2Int(_spawnColumn, GridManager.GoalRow));
        Gizmos.DrawSphere(spawn, 0.2f);
        Gizmos.DrawLine(spawn, goal);
    }
#endif
}
