using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public GridManager           GridManager;
    public EconomyManager        EconomyManager;
    public TowerPlacementManager TowerPlacementManager;
    public LivesManager          LivesManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
