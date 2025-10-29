using UnityEngine;

public class Data : MonoBehaviour
{
    public static Data Instance;

    [Header("Player Settings")]
    public float descendCap;           // if descend reaches this, teletoprt the player the excess (prevent long descends)
    public float descendDistance;      // how far down each descend goes
    public int maxCollectablePerRound; // max collectables that can spawn per round
    public float sideMoveSpeed;        // horizontal movement speed
    public float climbSpeed;           // vertical climbing speed
    public int points;                 // Player points
    [Space(15)]

    [Header("Collecteble points amount")]
    public int regularInsectValue;
    public int goldenInsectValue;


    [Header("Upgrades Settings")]
    public float descendBaseCost;  
    public float descendAddedCost; 
    public float descendStepSize;  
    public float descendMultiplier;
    [Space(15)]
    
    public int maxCollectableUpgradeCount;        // how many times the max collectable upgraded
    public float collectableBaseCost;             // cost of upgrade 
    public float collectableCostMultiplier;       // each time the cost increases by this multiplier
    public int collectableSpecialCostThreshold;   // after this many upgrades, the cost increases more
    [Space(15)]

    //public int RVMultiplier;

    [Header("Entity Prefabs")]
    [SerializeField] public GameObject[] collectablePrefabs; 
    [SerializeField] public GameObject[] waspPrefabs;        
    [SerializeField] public GameObject[] goldenPrefabs;

    [Header("Spawn Rates")][Tooltip("Must add up to 1 in total")]
    
    [Range(0f, 1f)] public float regularSpawnRate; 
    [Range(0f, 1f)] public float waspSpawnRate;  
    [Range(0f, 1f)] public float goldenSpawnRate; 

    [Header("Spawn Settings")]
    public float ySpacing; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ResetToDefaults()
    {
        // Set all your default values here
        descendCap = 100f;
        descendDistance = 30f;
        maxCollectablePerRound = 5;
        sideMoveSpeed = 4f;
        climbSpeed = 5f;
        points = 0;

        descendBaseCost = 30f;
        descendAddedCost = 10f;
        descendStepSize = 100f;
        descendMultiplier = 1.17f;

        maxCollectableUpgradeCount = 0;
        collectableBaseCost = 30;
        collectableCostMultiplier = 1.17f;
        collectableSpecialCostThreshold = 10;

        regularInsectValue = 10;
        goldenInsectValue = 50;

        regularSpawnRate = 0.65f;
        waspSpawnRate = 0.3f;
        goldenSpawnRate = 0.05f;

       // RVMultiplier = 3;

        ySpacing = 6f;

    }

    [System.Serializable]
    private class SaveData
    {
        public float descendCap;
        public float descendDistance;
        public int   maxCollectablePerRound;
        public float sideMoveSpeed;
        public float climbSpeed;
        public int   points;
        public float descendBaseCost;
        public float descendAddedCost;
        public float descendStepSize;
        public float descendMultiplier;
        public int   maxCollectableUpgradeCount;
        public float collectableBaseCost;
        public float collectableCostMultiplier;
        public int   collectableSpecialCostThreshold;
        public int   regularInsectValue;
        public int   goldenInsectValue;
        public float regularSpawnRate;
        public float waspSpawnRate;
        public float goldenSpawnRate;
        public float ySpacing;
    }

    private string SaveFilePath => System.IO.Path.Combine(Application.persistentDataPath, "save.json");

    public void SaveGameData()
    {
        SaveData save = new SaveData
        {
            descendCap = descendCap,
            descendDistance = descendDistance,
            maxCollectablePerRound = maxCollectablePerRound,
            sideMoveSpeed = sideMoveSpeed,
            climbSpeed = climbSpeed,
            points = points,
            descendBaseCost = descendBaseCost,
            descendAddedCost = descendAddedCost,
            descendStepSize = descendStepSize,
            descendMultiplier = descendMultiplier,
            maxCollectableUpgradeCount = maxCollectableUpgradeCount,
            collectableBaseCost = collectableBaseCost,
            collectableCostMultiplier = collectableCostMultiplier,
            collectableSpecialCostThreshold = collectableSpecialCostThreshold,
            regularInsectValue = regularInsectValue,
            goldenInsectValue = goldenInsectValue,
            regularSpawnRate = regularSpawnRate,
            waspSpawnRate = waspSpawnRate,
            goldenSpawnRate = goldenSpawnRate,
            ySpacing = ySpacing
        };
        string json = JsonUtility.ToJson(save);
        System.IO.File.WriteAllText(SaveFilePath, json);
        Debug.Log("Game data saved: " + SaveFilePath);
    }

    public bool LoadGameData()
    {
        if (System.IO.File.Exists(SaveFilePath))
        {
            string json = System.IO.File.ReadAllText(SaveFilePath);
            SaveData save = JsonUtility.FromJson<SaveData>(json);

            descendCap = save.descendCap;
            descendDistance = save.descendDistance;
            maxCollectablePerRound = save.maxCollectablePerRound;
            sideMoveSpeed = save.sideMoveSpeed;
            climbSpeed = save.climbSpeed;
            points = save.points;
            descendBaseCost = save.descendBaseCost;
            descendAddedCost = save.descendAddedCost;
            descendStepSize = save.descendStepSize;
            descendMultiplier = save.descendMultiplier;
            maxCollectableUpgradeCount = save.maxCollectableUpgradeCount;
            collectableBaseCost = save.collectableBaseCost;
            collectableCostMultiplier = save.collectableCostMultiplier;
            collectableSpecialCostThreshold = save.collectableSpecialCostThreshold;
            regularInsectValue = save.regularInsectValue;
            goldenInsectValue = save.goldenInsectValue;
            regularSpawnRate = save.regularSpawnRate;
            waspSpawnRate = save.waspSpawnRate;
            goldenSpawnRate = save.goldenSpawnRate;
            ySpacing = save.ySpacing;

            Debug.Log("Game data loaded: " + SaveFilePath);
            return true;
        }
        Debug.Log("No save file found, using defaults.");
        return false;
    }
}
