using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//--------------------- TODO ---------------------//
// Add larve, red Ant, cocons and lightning bug

// make insects to be unlockable with money to earn more points, descend deeper and 
//  optional
//  keep the "oh no" text when colliding with wasp
//  Add a top tree that the player can tell that we reached home.
//  Add spider web when you collect an insect and try to attach it to the branches when passing by them

public enum GameState { Loading, Waiting, Descending, Playing, Win, Fail }

public class GameManager : MonoBehaviour
{
    public static GameManager GameManagerInstance;

    // Lists
    private List<InteractableEntity> activeEntities = new List<InteractableEntity>(); // track spawned entities
    private List<GameObject> activeHeightIndicators = new List<GameObject>();

    public GameState currentState;

    [Header("Script References")]
    public Player player;
    public Rigidbody2D playerRb;

    private int insectsCollected = 0;
    private int roundStartPoints; // Points at the start of the round

    private float homeScreenAnimationDelay;

    private bool watchedRV;
    private bool isAnimatingPoints = false;

    private Camera mainCam;

    private Vector3 spiderStartPos;
    private Vector3 camOffset;
    private Vector3 backgroundOffset;

    [Header("Hight Indicator")]
    [SerializeField] GameObject heightIndicatorPrefab;

    private float climbTopY;
    private float climbBottomY;

    private float indicatorStep; //50f

    [Header("UI References")]
    // Texts
    [SerializeField] TextMeshProUGUI pointsText;
    [SerializeField] TextMeshProUGUI inGamePointsText;
    [SerializeField] TextMeshProUGUI inGameDescendDistanceText;
    [SerializeField] TextMeshProUGUI inGameMaxCollectableText;
    [Space(5)]

    [SerializeField] TextMeshProUGUI homeDescendDistanceText;
    [SerializeField] TextMeshProUGUI homeMaxCollectableText;
    [Space(5)]

    [SerializeField] TextMeshProUGUI descentBaseCostText;
    [SerializeField] TextMeshProUGUI collectableBaseCostText;
    [SerializeField] TextMeshProUGUI winPointsText;
    [SerializeField] TextMeshProUGUI failPointsText;
    [Space(10)]

    // text prefabs
    [SerializeField] GameObject floatingTextPrefab;
    [Space(10)]

    //Screens
    [SerializeField] GameObject winScreen;
    [SerializeField] GameObject failScreen;
    [SerializeField] GameObject settingsScreen;
    [SerializeField] GameObject homeScreen;
    [SerializeField] GameObject gameScreen;
    [SerializeField] GameObject manualScreen;
    [SerializeField] GameObject loadingScreen;
    [Space(10)]

    [SerializeField] RectTransform bankTarget;
    [Space(10)]

    [SerializeField] GameObject spiderUI;       // the UI spider image object
    [SerializeField] RectTransform webTarget;   // WebTarget in home Screen spider
    [SerializeField] LineRenderer spiderLine;
    [SerializeField] Animator spiderAnimator;   // assign the Spider UI Animator

    // Game Screen Elements
    [SerializeField] Transform background;      // Background image object
    [Space(10)]

    // Progress Bar
    [SerializeField] GameObject progressBar;
    [SerializeField] Image fill;
    [SerializeField] RectTransform knob;
    private RectTransform fillRect;

    private void Awake()
    {
        if (GameManagerInstance == null) 
            GameManagerInstance = this;
    }

    private void Start()
    {
        // Set target frame rate and physics timestep
        Application.targetFrameRate = 60; // or 30 for battery
        QualitySettings.vSyncCount = 0;   // let targetFrameRate control
        Time.fixedDeltaTime = 1f / 60f;   // match physics to frame rate

        winScreen.gameObject.SetActive(false);
        gameScreen.gameObject.SetActive(false);
        failScreen.gameObject.SetActive(false);
        settingsScreen.gameObject.SetActive(false);
        manualScreen.gameObject.SetActive(false);
        homeScreen.gameObject.SetActive(false);

        mainCam = Camera.main;
        watchedRV = false;
        spiderStartPos = player.transform.position;
        camOffset = mainCam.transform.position - player.transform.position;

        indicatorStep = 50;
        homeScreenAnimationDelay = 0.3f;

        if (player == null)
        {
            Debug.LogWarning(" Spider reference not set in GameManager!");
            return;
        }

        if (background != null)
        {
            backgroundOffset = background.position - mainCam.transform.position;
        }

        if (fill != null)
        {
            fillRect = fill.rectTransform;
        }

        Debug.Log("Finish start with all getters, starting loading");

        UpdateUI();
        StartCoroutine(ShowLoadingThenHome());

    }

    private void Update()
    {   
        // Check if spider reached the top while playing
        if (currentState == GameState.Playing && player.transform.position.y >= climbTopY)
        {
            EndRound(true); // reached top, round ends
        }

        // Update remaining descent distance while climbing
        if (currentState == GameState.Playing && inGameDescendDistanceText != null)
        {
            float remaining = Mathf.Max(0, spiderStartPos.y - player.transform.position.y);
            inGameDescendDistanceText.text = remaining.ToString("F0");
        }

        // Line between spider UI and web target
        if (spiderLine != null && spiderUI != null && webTarget != null)
        {
            // Convert UI positions to world space if needed
            Vector3 spiderPos = spiderUI.transform.position;
            Vector3 targetPos = webTarget.position;

            spiderLine.positionCount = 2;
            spiderLine.SetPosition(0, spiderPos);
            spiderLine.SetPosition(1, targetPos);
        }
    }

    // ---------------- Optimization Logic ---------------- 

    private void LateUpdate()
    {
        if (currentState == GameState.Descending || currentState == GameState.Playing)
        {
            // Use the player's interpolated render pose for smooth camera motion
            Vector3 follow = (player != null) ? player.GetRenderPosition() : Vector3.zero;

            // Keep X fixed by design; preserve Z; apply the same vertical offset
            mainCam.transform.position = new Vector3(
                camOffset.x,
                follow.y + camOffset.y,
                mainCam.transform.position.z
            );
        }

        // Background follows the camera (static illusion)
        if (background != null)
        {
            background.position = new Vector3(
                mainCam.transform.position.x + backgroundOffset.x,
                mainCam.transform.position.y + backgroundOffset.y,
                background.position.z
            );
        }

        // Cull entities outside of camera view for performance
        if (currentState == GameState.Descending || currentState == GameState.Playing)
        {
            const float cullMargin = 0.15f;
            foreach (var entity in activeEntities)
            {
                if (entity == null) continue;
                Camera cam = Camera.main;
                Vector3 viewportPos = cam.WorldToViewportPoint(entity.transform.position);

                bool shouldBeActive = viewportPos.x > -cullMargin && viewportPos.x < 1 + cullMargin &&
                                      viewportPos.y > -cullMargin && viewportPos.y < 1 + cullMargin &&
                                      viewportPos.z > 0;

                if (entity.gameObject.activeSelf != shouldBeActive)
                    entity.gameObject.SetActive(shouldBeActive);
            }

            // Cull height indicators outside of camera view
            foreach (var indicator in activeHeightIndicators)
            {
                if (indicator == null) continue;
                Camera cam = Camera.main;
                Vector3 viewportPos = cam.WorldToViewportPoint(indicator.transform.position);

                bool shouldBeActive = viewportPos.x > -cullMargin && viewportPos.x < 1 + cullMargin &&
                                      viewportPos.y > -cullMargin && viewportPos.y < 1 + cullMargin &&
                                      viewportPos.z > 0;

                if (indicator.activeSelf != shouldBeActive)
                    indicator.SetActive(shouldBeActive);
            }
        }
    }

    // ---------------- Save | Load Logic ---------------- 
    
    // Save data when application quits
    private void OnApplicationQuit()
    {
        Data.Instance.SaveGameData();
    }


    // ---------------- Loading Screen Logic ---------------- 
    private IEnumerator ShowLoadingThenHome()
    {
        currentState = GameState.Loading;
        loadingScreen.gameObject.SetActive(true);

        // Simulate loading time and load data
        float loadTime = 2f;
        float elapsed = 0f;

        // Load game data from JSON
        bool loaded = Data.Instance.LoadGameData();

        while (elapsed < loadTime)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / loadTime);
            UpdateProgressBar(progress);
            yield return null;
        }

        UpdateProgressBar(1f);

        // Hide loading, show home
        Debug.Log("Loading complete. Data loaded: " + loaded);
        Debug.Log("Tap to Start!");
        UpdateUI();
        loadingScreen.gameObject.SetActive(false);
        homeScreen.gameObject.SetActive(true);
        currentState = GameState.Waiting;
    }


    // ---------------- ProgressBar Logic ----------------
    private void UpdateProgressBar(float progress)
    {
        if (fill == null || knob == null || fillRect == null) return;

        // Update fill amount
        fill.fillAmount = progress;

        // Move knob inside parent (local space)
        float width = fillRect.rect.width;
        Vector3 newLocalPos = knob.localPosition;

        // Progress moves from left (0) to right (1)
        newLocalPos.x = -width * 0.5f + (width * progress);
        knob.localPosition = newLocalPos;
    }


    // ---------------------- Round Logic ----------------------

    private IEnumerator StartRoundTransition()
    {
        spiderStartPos = new Vector3(0, -3, 0);
        float descendCap = Data.Instance.descendCap;
        float totalDescend = Data.Instance.descendDistance;
        float descendSegment, descendStartY;

        if (totalDescend <= descendCap)
        {
            descendSegment = totalDescend;
            descendStartY = -3f;
        }
        else
        {
            descendSegment = descendCap;
            descendStartY = -3f - (totalDescend - descendCap);
        }
        // descendStartPos
        Vector3 descentTeleportDistance = new Vector3(0, descendStartY, 0);

        // 1. Teleport the spider (no descent yet)
        player.TeleportPlayer(descentTeleportDistance);

        // 2. Wait for camera/background to follow
        yield return new WaitForSeconds(1.5f); // adjust as needed for smoothness

        // 3. Switch screens and start the visible descent
        StartRound(totalDescend, descendSegment, descentTeleportDistance);
    }
    public void StartRound(float totalDescend, float descendSegment, Vector3 descendStartPos)
    {
        insectsCollected = 0;
        roundStartPoints = Data.Instance.points;

        UpdateUI();

        // Entities always spawn from the bottom of the climb (lowest Y) + 5, up to -3
         climbTopY = -3f;
         climbBottomY = spiderStartPos.y - totalDescend;

        SpawnEntities(climbBottomY, climbTopY);
        BuildHeightIndicators(climbBottomY, climbTopY);

        // Switch screens
        homeScreen.gameObject.SetActive(false);
        gameScreen.gameObject.SetActive(true);

        // Calculate descend speed to always take 3 seconds for the capped segment
        float descendDuration = 3f;
        float calculatedDescendSpeed = descendSegment / descendDuration;

        // Begin descend for the segment
        player.BeginDescend(descendStartPos, descendSegment, calculatedDescendSpeed);

        // Reset UI for new round
        if (inGameMaxCollectableText != null)
            inGameMaxCollectableText.text = Data.Instance.maxCollectablePerRound.ToString();
        if (inGameDescendDistanceText != null)
            inGameDescendDistanceText.text = Data.Instance.descendDistance.ToString("F0");
        if (inGamePointsText != null)
            inGamePointsText.text = "0";
    }

    public void StartGameplay()
    {
        currentState = GameState.Playing;
        player.BeginClimb(Data.Instance.climbSpeed);
    }

    public void EndRound(bool didWin)
    {
        int pointsEarned = Data.Instance.points - roundStartPoints; ;

        if (didWin)
        {
            player.StopMovement();
            currentState = GameState.Win;
            winScreen.gameObject.SetActive(true);
            if (winPointsText != null)
                winPointsText.text = "" + pointsEarned;
        }
        else
        {
            //currentState = GameState.Fail; //GameState logic handled in Player script during fall
            failScreen.gameObject.SetActive(true);
            if (failPointsText != null)
                failPointsText.text = "" + pointsEarned;
        }

        Debug.Log("Round End: " + (didWin ? "WIN" : "FAIL"));
    }

    public void RestartGame()
    {
        // Hide all screens except home
        winScreen.gameObject.SetActive(false);
        failScreen.gameObject.SetActive(false);
        gameScreen.gameObject.SetActive(false);
        homeScreen.gameObject.SetActive(true);

        // Play the outro animation for the spider
        if (spiderAnimator != null)
            spiderAnimator.SetTrigger("PlayOutro");

        // Start coroutine to wait for animation to end then change state to waiting 
        StartCoroutine(WaitForOutroAndSetState());

        // Reset spider positions (game logic)
        player.ResetPosition(spiderStartPos);

        player.transform.rotation = Quaternion.identity;
        if (player.line != null)
        { player.line.enabled = true; }

        ClearEntities();
        ClearHeightIndicators();

        // Reset camera to starting position
        if (mainCam != null)
        {
            Vector3 camStartPos = spiderStartPos + camOffset;
            mainCam.transform.position = new Vector3(
                camStartPos.x,
                camStartPos.y,
                mainCam.transform.position.z
            );
        }

        // --- Animate coins and points ---
        int pointsEarned = Data.Instance.points - roundStartPoints;

        if (pointsEarned > 0 && bankTarget != null)
        {
            ShowHomeScreenFloatingText(pointsEarned);
            Debug.Log("Showing floating text at bank target");
        }


        if (pointsEarned > 0 && watchedRV)
        {
            isAnimatingPoints = true;
            //StartCoroutine(PlayCoinFlyAnimation(true));
            StartCoroutine(AnimatePoints(Data.Instance.points - pointsEarned, Data.Instance.points, 1.5f));
        }
        else if (pointsEarned > 0)
        {
            isAnimatingPoints = true;
            // StartCoroutine(PlayCoinFlyAnimation(false));
            StartCoroutine(AnimatePoints(Data.Instance.points - pointsEarned, Data.Instance.points, 1.5f));
        }

        // Reset roundStartPoints for the next round
        // roundStartPoints = Data.Instance.points;

        Debug.Log($"RestartGame: pointsEarned={pointsEarned}, bankTarget={bankTarget}");
        UpdateUI();
    }

    private IEnumerator WaitForOutroAndSetState()
    {
         float outroDuration = 1.7f; //(this need to be the exact same time the outro animation is)
         yield return new WaitForSeconds(outroDuration);
        
         currentState = GameState.Waiting;
    }



    // ---------------- Hight Indicators logic ---------------- 
    private void BuildHeightIndicators(float climbBottomY, float climbTopY )
    {
        ClearHeightIndicators();
        if (heightIndicatorPrefab == null) return;

        float total = climbTopY - climbBottomY;
        int steps = Mathf.CeilToInt(total / indicatorStep);

        for (int i = 0; i <= steps; i++)
        {
            float y = climbTopY - i * indicatorStep;

            // Stop if the next indicator would be below the bottom
            if (y < climbBottomY)
                break;

            float remaining = climbTopY - y;
            if (remaining < 0) remaining = 0;

            GameObject indicator = Instantiate(heightIndicatorPrefab, new Vector3(0f, y, 0f), Quaternion.identity);

            var textUGUI = indicator.GetComponentInChildren<TextMeshProUGUI>();
            if (textUGUI != null)
            textUGUI.text = $"{Mathf.RoundToInt(remaining)}m";
            else
            {
                var text3D = indicator.GetComponentInChildren<TextMeshPro>();
                if (text3D != null)
                 text3D.text = $"{Mathf.RoundToInt(remaining)}m";
            }
          activeHeightIndicators.Add(indicator);
        }
    }

    private void ClearHeightIndicators()
    {
        foreach (var go in activeHeightIndicators)
            if (go != null) Destroy(go);
        activeHeightIndicators.Clear();
    }


    // ---------------------- Points Logic ----------------------
    public void CollectInsect(int pointsToAdd, Vector3 worldPosition)
    {
        insectsCollected++;
        Data.Instance.points += pointsToAdd;

        ShowInGameFloatingText(worldPosition, "+" + pointsToAdd);

        if (inGamePointsText != null)
        {
            inGamePointsText.text = (Data.Instance.points - roundStartPoints).ToString();
        }

        UpdateUI();

        if (insectsCollected >= Data.Instance.maxCollectablePerRound)
        {
            EndRound(true);
        }
    }

    // In-game floating text (collecting insects)
    public void ShowInGameFloatingText(Vector3 worldPosition, string text)
    {
        if (floatingTextPrefab == null) return;

    // Get the canvas and its RectTransform
    Canvas canvas = gameScreen.GetComponentInParent<Canvas>();
    RectTransform canvasRect = canvas.GetComponent<RectTransform>();

    // Convert world position to screen point
    Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);

    // Convert screen point to local point in canvas
    Vector2 localPoint;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, canvas.worldCamera, out localPoint);

    // Instantiate and set position
    var instance = Instantiate(floatingTextPrefab, gameScreen.transform);
    RectTransform rect = instance.GetComponent<RectTransform>();
    rect.anchoredPosition = localPoint;

    var tmp = instance.GetComponent<TMPro.TextMeshProUGUI>();
    if (tmp != null)
    {
        tmp.text = text;
        tmp.fontSize = 150;
    }

    StartCoroutine(AnimateFloatingText(instance));
    }

    // Home screen floating text (bank/points after round)
    public void ShowHomeScreenFloatingText(int amount)
    {
        if (floatingTextPrefab == null || bankTarget == null) return;
        StartCoroutine(ShowHomeScreenFloatingTextDelayed(amount));
    }

    private IEnumerator ShowHomeScreenFloatingTextDelayed(int amount)
    {
        // Wait for all transforms/camera/canvas to update
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(homeScreenAnimationDelay);

        var canvas = homeScreen.GetComponentInParent<Canvas>();
        var canvasRect = canvas.GetComponent<RectTransform>();

        // Get the bank target's position now, after all resets
        Vector3 bankWorldPos = bankTarget.position;

        // Convert world position to screen point
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, bankWorldPos);

        // Convert screen point to local point in canvas
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, canvas.worldCamera, out localPoint);

        var instance = Instantiate(floatingTextPrefab, homeScreen.transform);
        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.anchoredPosition = localPoint;

        var tmp = instance.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = $"+{amount}";
            tmp.fontSize = 150;
            tmp.color = Color.green;
        }

        StartCoroutine(AnimateFloatingText(instance));
    }

    private void ShowBankSpendFloatingText(float amount)
    {
        if (floatingTextPrefab == null || bankTarget == null) return;

        var instance = Instantiate(floatingTextPrefab, homeScreen.transform);

        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.position = bankTarget.position;

        var tmp = instance.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = $"-{Mathf.RoundToInt(amount)}";
            tmp.fontSize = 150;
            tmp.color = Color.red; 
        }

        StartCoroutine(AnimateFloatingText(instance));
    }

    // ---------------- Animation Logic ----------------

    // floating text animation (move up and disappear gradually)
    private IEnumerator AnimateFloatingText(GameObject textObj)
    {
        float moveUpDistance = 0.5f;
        float duration = 1.2f;
        float fadeDuration = 0.8f;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        Vector3 startPos = rect.position;
        Vector3 endPos = startPos + Vector3.up * moveUpDistance;
        float elapsed = 0f;

        var tmp = textObj.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmp == null) yield break; // Prevent NullReferenceException

        Color originalColor = tmp.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rect.position = Vector3.Lerp(startPos, endPos, t);

            // Fade out
            if (elapsed > duration - fadeDuration)
            {
                float fadeT = 1 - ((elapsed - (duration - fadeDuration)) / fadeDuration);
                tmp.color = new Color(originalColor.r, originalColor.g, originalColor.b, fadeT);
            }

            yield return null;
        }

        Destroy(textObj);
    }

    // animate the points to go up grdually to the new amount
    private IEnumerator AnimatePoints(int startValue, int endValue, float duration)
    {
        yield return new WaitForSeconds(homeScreenAnimationDelay); // Add delay before showing points go up 

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            int current = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, elapsed / duration));
            pointsText.text = current.ToString();
            yield return null;
        }
        pointsText.text = endValue.ToString();
        isAnimatingPoints = false;
    }


    /// /------------------- Spawn | Despawn Entities Logic ----------------------
    private void SpawnEntities(float climbBottomY, float climbTopY)
    {
        float ySpacing = Data.Instance.ySpacing;
        GameObject[] regularPrefabs = Data.Instance.collectablePrefabs;
        GameObject[] waspPrefabs = Data.Instance.waspPrefabs;
        GameObject[] goldenPrefabs = Data.Instance.goldenPrefabs;

        if ((regularPrefabs == null || regularPrefabs.Length == 0) &&
            (waspPrefabs == null || waspPrefabs.Length == 0) &&
            (goldenPrefabs == null || goldenPrefabs.Length == 0))
        {
            Debug.LogWarning("No prefabs assigned for spawning!");
            return;
        }

        // Entities spawn from 5 units above the bottom, up to the top
        float spawnStartY = climbBottomY + 5f;
        float spawnEndY = climbTopY;

        // Safe guard between entities
        List<Vector3> usedPositions = new List<Vector3>();
        float minDistance = 1.0f; // minimum distance between spawned entities

        float currentY = spawnStartY;
        while (currentY <= spawnEndY)
        {
            float roll = Random.value;
            GameObject prefab = null;

            float waspThreshold = Data.Instance.waspSpawnRate;
            float goldenThreshold = waspThreshold + Data.Instance.goldenSpawnRate;
            float regularThreshold = goldenThreshold + Data.Instance.regularSpawnRate;

            if (roll < waspThreshold && waspPrefabs.Length > 0)
                prefab = waspPrefabs[Random.Range(0, waspPrefabs.Length)];
            else if (roll < goldenThreshold && goldenPrefabs.Length > 0)
                prefab = goldenPrefabs[Random.Range(0, goldenPrefabs.Length)];
            else if (roll < regularThreshold && regularPrefabs.Length > 0)
                prefab = regularPrefabs[Random.Range(0, regularPrefabs.Length)];

            Vector3 leftEdge = Camera.main.ViewportToWorldPoint(new Vector3(0, 0.5f, Mathf.Abs(Camera.main.transform.position.z)));
            Vector3 rightEdge = Camera.main.ViewportToWorldPoint(new Vector3(1, 0.5f, Mathf.Abs(Camera.main.transform.position.z)));

            float randX;
            Vector3 spawnPos;
            int attempts = 0;
            do
            {
                randX = Random.Range(leftEdge.x + 1f, rightEdge.x - 1f);
                spawnPos = new Vector3(randX, currentY, 0);
                attempts++;
            }
            while (usedPositions.Any(pos => Vector3.Distance(pos, spawnPos) < minDistance) && attempts < 5);

            usedPositions.Add(spawnPos);

            if (prefab != null)
            {
                GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
                InteractableEntity interactableEntity = obj.GetComponent<InteractableEntity>();
                if (interactableEntity == null) interactableEntity = obj.AddComponent<InteractableEntity>();
                //interactableEntity.Init();
                RegisterEntity(interactableEntity);
            }

            currentY += ySpacing; // move up
        }
    }

    public void RegisterEntity(InteractableEntity entity)
    {
        if (!activeEntities.Contains(entity))
            activeEntities.Add(entity);
    }

    public void UnregisterEntity(InteractableEntity entity)
    {
        activeEntities.Remove(entity);
    }

    private void ClearEntities()
    {
        foreach (var entity in activeEntities.ToList())
        {
            Destroy(entity.gameObject);
        }
        activeEntities.Clear();
    }


    // --------------------- Upgrade Logic ----------------------
    public void UpgradeMaxCollectable()
    {
        float upgradeCost = Data.Instance.collectableBaseCost * Mathf.Pow(Data.Instance.collectableCostMultiplier, Data.Instance.maxCollectableUpgradeCount);
        upgradeCost = Mathf.Round(upgradeCost);

        if (currentState != GameState.Waiting) return;

        if (Data.Instance.points < upgradeCost)
        {
            ShowBankWarningFloatingText("Missing Money!");
            Debug.Log($"Not enough points to upgrade max collectable! Need {upgradeCost}, have {Data.Instance.points}");
            return;
        }

        Data.Instance.maxCollectablePerRound += 1;
        Data.Instance.points -= Mathf.RoundToInt(upgradeCost);
        Data.Instance.maxCollectableUpgradeCount++;

        // Show spend text
        ShowBankSpendFloatingText(upgradeCost);

        // After reaching the threshold, change the multiplier for future upgrades
        if (Data.Instance.maxCollectableUpgradeCount == Data.Instance.collectableSpecialCostThreshold)
        {
            Data.Instance.collectableCostMultiplier = 1.05f; // Set to your new multiplier value
            // You can also fetch this value from Firebase here if needed
        }

        UpdateUI();
        Debug.Log($"Upgraded max collectable to {Data.Instance.maxCollectablePerRound}. Cost: {upgradeCost}");
    }

    public void UpgradeDescendDistance()
    {
        if (currentState != GameState.Waiting) return;

        // Calculate cost
        float upgradeCost = Data.Instance.descendBaseCost;
        if (Data.Instance.descendDistance > Data.Instance.descendStepSize)
        {
            // For every full 100 meters above 100, add step cost
            int extraSteps = Mathf.FloorToInt((Data.Instance.descendDistance - Data.Instance.descendStepSize) / Data.Instance.descendStepSize) + 1;
            upgradeCost += extraSteps * Data.Instance.descendAddedCost;
        }

        if (Data.Instance.points < upgradeCost)
        {
            ShowBankWarningFloatingText("Missing Money!");
            Debug.Log($"Not enough points to upgrade descend distance! Need {upgradeCost}, have {Data.Instance.points}");
            return;
        }

        // Upgrade descend distance by multiplier and round to nearest whole number
        Data.Instance.descendDistance = Mathf.Round(Data.Instance.descendDistance * Data.Instance.descendMultiplier);

        Data.Instance.points -= Mathf.RoundToInt(upgradeCost);

        ShowBankSpendFloatingText(upgradeCost);

        UpdateUI();
        Debug.Log($"Upgraded descend distance to {Data.Instance.descendDistance}. Cost: {upgradeCost}");
    }

    public void ShowBankWarningFloatingText(string message)
    {
        if (floatingTextPrefab == null || bankTarget == null) return;

        // Instantiate under homeScreen for consistent scaling
        var instance = Instantiate(floatingTextPrefab, homeScreen.transform);

        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.position = bankTarget.position;

        var tmp = instance.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = message;
           // tmp.fontSize = 150;
        }

        StartCoroutine(AnimateFloatingText(instance));
    }


    // ---------------------- UI Logic ----------------------
    private void UpdateUI()
    {
        // Update only the active screen's UI

        if (homeScreen.activeSelf || loadingScreen.activeSelf)
        {
            if (pointsText != null && !isAnimatingPoints)
                pointsText.text = Data.Instance.points.ToString();

            if (homeDescendDistanceText != null)
                homeDescendDistanceText.text = Data.Instance.descendDistance.ToString("F0");

            if (homeMaxCollectableText != null)
                homeMaxCollectableText.text = Data.Instance.maxCollectablePerRound.ToString();

            if (descentBaseCostText != null)
            {
                float descendCost = Data.Instance.descendBaseCost;
                if (Data.Instance.descendDistance > Data.Instance.descendStepSize)
                {
                    int extraSteps = Mathf.FloorToInt((Data.Instance.descendDistance - Data.Instance.descendStepSize) / Data.Instance.descendStepSize) + 1;
                    descendCost += extraSteps * Data.Instance.descendAddedCost;
                }
                descentBaseCostText.text = Mathf.RoundToInt(descendCost) + " Upgrade";
            }

            if (collectableBaseCostText != null)
            {
                float collectableCost = Data.Instance.collectableBaseCost * Mathf.Pow(Data.Instance.collectableCostMultiplier, Data.Instance.maxCollectableUpgradeCount);
                collectableBaseCostText.text = Mathf.RoundToInt(collectableCost) + " Upgrade";
            }
        }

        if (gameScreen.activeSelf)
        {
            if (inGameDescendDistanceText != null)
                inGameDescendDistanceText.text = Data.Instance.descendDistance.ToString();

            if (inGameMaxCollectableText != null)
            {
                if (currentState == GameState.Playing)
                    inGameMaxCollectableText.text = (Data.Instance.maxCollectablePerRound - insectsCollected).ToString();
                else
                    inGameMaxCollectableText.text = Data.Instance.maxCollectablePerRound.ToString();
            }

            if (inGamePointsText != null)
                inGamePointsText.text = (Data.Instance.points - roundStartPoints).ToString();
        }
    }


    // ---------------------- Buttons Logic ----------------------
    public void OnStartGameButtonPressed()
    {
        if (currentState != GameState.Waiting)
            return; // Prevent multiple starts

        spiderAnimator.SetTrigger("PlayIntro");
        currentState = GameState.Descending;
        StartCoroutine(StartRoundTransition());
    }

    public void RVMultiplier()
    {
        int pointsEarned = Data.Instance.points - roundStartPoints;
        int bonus = pointsEarned * 2; // 2x bonus to make total 3x (original + 2x more)
        Data.Instance.points += bonus;
        watchedRV = true;
        UpdateUI();
        RestartGame();
    }

    public void OpenSettings()
    {
        if (currentState == GameState.Waiting)
        {
            settingsScreen.gameObject.SetActive(true);
        }
        else
        {
            settingsScreen.gameObject.SetActive(true);
            Time.timeScale = 0f; // Pause the game
        }
    }

    public void CloseSettings()
    {
        if (currentState == GameState.Waiting)
        {
            settingsScreen.gameObject.SetActive(false);
        }
        else
        {
            settingsScreen.gameObject.SetActive(false);
            Time.timeScale = 1f; // Unpause the game
        }
    }

    public void OpenManual()
    {
        manualScreen.gameObject.SetActive(true);
        settingsScreen.gameObject.SetActive(false);
    }

    public void CloseManual()
    {
        manualScreen.gameObject.SetActive(false);
        settingsScreen.gameObject.SetActive(true);
    }


    // ---------------------- Developer Testing Buttons ----------------------
    public void Dev_ResetSaveFileToDefaults()
    {
        Data.Instance.ResetToDefaults();
        Data.Instance.SaveGameData();
        UpdateUI();
        Debug.Log("Save file reset to defaults.");
    }
    
}

