using UnityEngine;

public class Player : MonoBehaviour
{
    private bool climbing = false;

    private Vector3 descendStart;
    private Vector3 descendTarget;
    private float descendDuration;
    private float descendElapsed;
    private bool descending = false;

    [SerializeField] Rigidbody2D rb;
    [SerializeField] Transform spider;
    [SerializeField] Transform webAnchor;

    public LineRenderer line;

    [Header("Settings")]
    private float minX, maxX;
    private bool isDragging = false;
    private float dragStartX = 0f;
    private float currentX;
    private Vector2 targetPosition;

    [Header("Tilt Settings")]
    [SerializeField] float tiltAmount;
    [SerializeField] float tiltSpeed;

    // Interpolation cache for camera follow
    private Vector2 prevRbPosition;
    private Vector2 currRbPosition;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        // RB config
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // default for climb

        descendDuration = 3f;
        descendElapsed = 1f;

        targetPosition = rb.position;
        currentX = rb.position.x;

        // Calculate screen bounds in world space
        Vector3 leftEdge = Camera.main.ViewportToWorldPoint(new Vector3(0, 0.5f, Mathf.Abs(Camera.main.transform.position.z)));
        Vector3 rightEdge = Camera.main.ViewportToWorldPoint(new Vector3(1, 0.5f, Mathf.Abs(Camera.main.transform.position.z)));

        minX = leftEdge.x + 0.2f;
        maxX = rightEdge.x - 0.2f;

        // Line
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;

        // Initialize interpolation cache
        prevRbPosition = rb.position;
        currRbPosition = rb.position;
    }

    void Update()
    {
        // Visual-only line and tilt
        if (spider != null)
        {
            float newZ = Mathf.LerpAngle(spider.localEulerAngles.z, 0, Time.deltaTime * tiltSpeed);
            spider.localEulerAngles = new Vector3(0, 0, newZ);
        }
        if (spider == null || webAnchor == null) return;
        line.SetPosition(0, webAnchor.position);
        line.SetPosition(1, spider.position);
    }

    void FixedUpdate()
    {
        // Store previous pose for interpolation
        prevRbPosition = currRbPosition;

        if (descending)
        {
            // Descent progresses in fixed time
            descendElapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(descendElapsed / descendDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            float newY = Mathf.Lerp(descendStart.y, descendTarget.y, easedT);

            // Lock X during descent, move Y
            Vector2 nextPos = new Vector2(descendStart.x, newY);
            rb.MovePosition(nextPos);
            currRbPosition = nextPos;

            if (t >= 1f)
            {
                descending = false;
                StartCoroutine(WaitAndStartClimb());
            }
            return;
        }

        if (climbing)
        {
            // Handle horizontal input in physics step
            HandleInputPhysics();

            // Move vertically by climbSpeed each fixed step
            targetPosition.y = rb.position.y + Data.Instance.climbSpeed * Time.fixedDeltaTime;

            // Move both axes together (interpolated by RB)
            rb.MovePosition(targetPosition);
            currRbPosition = targetPosition;
        }
        else
        {
            currRbPosition = rb.position;
        }
    }

    // Expose an interpolated render pose for smooth camera follow
    public Vector3 GetRenderPosition()
    {
        float alpha = 0f;
        if (Time.fixedDeltaTime > 0f)
            alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);

        return Vector2.Lerp(prevRbPosition, currRbPosition, alpha);
    }

    private void HandleInputPhysics()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartX = Input.mousePosition.x;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            float currentInputX = Input.mousePosition.x;
            float deltaX = currentInputX - dragStartX;

            if (Mathf.Abs(deltaX) > 1f)
            {
                float direction = Mathf.Sign(deltaX);
                float moveAmount = direction * Data.Instance.sideMoveSpeed * Time.fixedDeltaTime;
                currentX = Mathf.Clamp(currentX + moveAmount, minX, maxX);

                targetPosition = new Vector2(currentX, rb.position.y);

                if (spider != null)
                {
                    float targetTilt = -direction * tiltAmount;
                    float newZ = Mathf.LerpAngle(spider.localEulerAngles.z, targetTilt, Time.fixedDeltaTime * tiltSpeed);
                    spider.localEulerAngles = new Vector3(0, 0, newZ);
                }

                dragStartX = currentInputX;
            }
        }
        else
        {
            targetPosition = new Vector2(currentX, rb.position.y);
        }
    }

    public void TeleportPlayer(Vector3 descentTeleportDistance)
    {
        rb.position = descentTeleportDistance;
        currentX = descentTeleportDistance.x;
        targetPosition = descentTeleportDistance;
        climbing = false;
        descending = false;

        // Reset interpolation cache to avoid a big lerp jump
        prevRbPosition = descentTeleportDistance;
        currRbPosition = descentTeleportDistance;
    }

    public void BeginDescend(Vector3 startPos, float distance, float speed)
    {
        rb.gravityScale = 0f;

        descendStart = startPos;
        descendTarget = startPos - new Vector3(0, distance, 0);
        descendDuration = distance / speed;
        descendElapsed = 0f;

        // keep interpolation ON for consistent render pose
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Reset interpolation cache at the start of descent
        prevRbPosition = startPos;
        currRbPosition = startPos;

        descending = true;
        climbing = false;
    }

    private System.Collections.IEnumerator WaitAndStartClimb()
    {
        yield return new WaitForSeconds(0.2f);
        currentX = rb.position.x;
        targetPosition = rb.position;
        GameManager.GameManagerInstance.StartGameplay();
    }

    public void BeginClimb(float speed)
    {
        Data.Instance.climbSpeed = speed;
        rb.gravityScale = 0f;

        // Ensure interpolation for physics-driven climb
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Align interpolation cache
        prevRbPosition = rb.position;
        currRbPosition = rb.position;

        climbing = true;
    }

    private System.Collections.IEnumerator FallAndRestartCoroutine(float fallDuration)
    {
        GameManager.GameManagerInstance.currentState = GameState.Fail;

        if (line != null) line.enabled = false;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(Vector2.up * 3f, ForceMode2D.Impulse);

        StopMovement();

        yield return new WaitForSeconds(fallDuration);

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        GameManager.GameManagerInstance.EndRound(false);
    }

    public void StopMovement()
    {
        climbing = false;
        descending = false;
    }

    public void ResetPosition(Vector3 startPos)
    {
        rb.position = startPos;
        currentX = startPos.x;
        targetPosition = startPos;
        climbing = false;
        descending = false;

        // Reset interpolation cache
        prevRbPosition = startPos;
        currRbPosition = startPos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.GameManagerInstance.currentState == GameState.Playing && other.CompareTag("Wasp"))
        {
            StartCoroutine(FallAndRestartCoroutine(2f));

            // addind a Oh No floating text when hitting wasp 
            // need to adjust font size and color 

            //GameManager.GameManagerInstance.ShowInGameFloatingText(
            //    new Vector3(other.transform.position.x, other.transform.position.y + 0.5f, other.transform.position.z),
            //    "Oh No!"
            //);

            Debug.Log("Hit wasp nest! Round failed.");
        }

        if (GameManager.GameManagerInstance.currentState == GameState.Playing && other.CompareTag("RegularInsect"))
        {
            GameManager.GameManagerInstance.CollectInsect(Data.Instance.regularInsectValue, other.transform.position);
            Debug.Log("Insect collected!");
            Destroy(other.gameObject);
        }

        if (GameManager.GameManagerInstance.currentState == GameState.Playing && other.CompareTag("GoldenInsect"))
        {
            GameManager.GameManagerInstance.CollectInsect(Data.Instance.goldenInsectValue, other.transform.position);
            Debug.Log("Fly collected!");
            Destroy(other.gameObject);
        }
    }
}
