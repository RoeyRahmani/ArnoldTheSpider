using UnityEngine;

//Optional Improvement:

// Use refactor to make separate methods for each movement type.
// This improves readability and makes it easier to add new behaviors.
// Example:
//private void Update()
//{
//    switch (entityType)
//    {
//        case EntityType.Ant:
//            UpdateAnt();
//            break;
//        case EntityType.Beetle:
//            UpdateBeetle();
//            break;
//        case EntityType.Flying:
//        case EntityType.Wasp:
//            UpdateFlyingOrWasp();
//            break;
//    }
//}

public class InteractableEntity : MonoBehaviour
{
    public enum EntityType { Flying, Wasp, Beetle, Ant, Snail }
    public EntityType entityType; // Set this in the inspector or when spawning

    private float speed;
    private float minX, maxX;
    private int direction = 1;

    private float horizontalSpeed;
    private float verticalSpeed;



    //--------------------- Mover ---------------------//
     void Start()
    {
        // For Ant and Beetle, use separate speeds
        if (   entityType == EntityType.Beetle 
            || entityType == EntityType.Ant 
           ) 
        {
            horizontalSpeed = Random.Range(0.2f, 0.4f); // slower side-to-side
            verticalSpeed = Random.Range(0.8f, 1.2f);   // faster up/down
        }
        else if (entityType == EntityType.Snail)
        {
            speed = Random.Range(0.1f, 0.3f); // Very slow speed for Snail
        }
        else // Flying or Wasp speed
        {
            speed = Random.Range(0.7f, 1.3f);
        }

        // Randomize starting direction: 50% chance left (-1), 50% chance right (1)
        direction = Random.value < 0.5f ? -1 : 1;

        // Screen bounds in world space
        Vector3 leftEdge = Camera.main.ViewportToWorldPoint(new Vector3(0, 0.5f, Mathf.Abs(Camera.main.transform.position.z)));
        Vector3 rightEdge = Camera.main.ViewportToWorldPoint(new Vector3(1, 0.5f, Mathf.Abs(Camera.main.transform.position.z)));

        float margin = (entityType == EntityType.Beetle || entityType == EntityType.Ant) ? 1.5f : 0.3f;
        minX = leftEdge.x + margin;
        maxX = rightEdge.x - margin;

        FlipSprite(); // Ensure correct facing at spawn
    }

    private void Update()
    {
        if (entityType == EntityType.Beetle || entityType == EntityType.Ant)
        {
            float verticalDir = entityType == EntityType.Ant ? 1f : -1f;
            Vector3 moveDir = new Vector3(direction * horizontalSpeed, verticalDir * verticalSpeed, 0);

            transform.Translate(moveDir * Time.deltaTime);

            if (transform.position.x <= minX || transform.position.x >= maxX)
            {
                direction *= -1; // Reverse direction
                transform.position = new Vector3( // Clamp position to stay within bounds
                    Mathf.Clamp(transform.position.x, minX, maxX),
                    transform.position.y,
                    transform.position.z
                );
                FlipSprite();
            }

            float tiltSign = (entityType == EntityType.Beetle) ? 1f : -1f;
            float tiltAngle = tiltSign * 15f * direction;
            transform.rotation = Quaternion.Euler(0, 0, tiltAngle);
        }
        else if (entityType == EntityType.Snail)
        {
            // Move side to side
            transform.Translate(Vector3.up * speed * Time.deltaTime);
            
        }
        else
        {
            // Move side to side 
            transform.Translate(Vector3.right * direction * speed * Time.deltaTime);

            if (transform.position.x <= minX || transform.position.x >= maxX)
            {
                direction *= -1; // Reverse direction
                transform.position = new Vector3( // Clamp position to stay within bounds
                    Mathf.Clamp(transform.position.x, minX, maxX),
                    transform.position.y,
                    transform.position.z
                );
                FlipSprite();
            }
            // Reset rotation for flying/wasp
            transform.rotation = Quaternion.identity;
        }
    }

    // --------------------- Helper Methods -------------------//
    private void FlipSprite()
    {
        Vector3 scale = transform.localScale;

        if (entityType == EntityType.Ant)
        {
            // For Ant: right = positive X, left = negative X
            scale.x = Mathf.Abs(scale.x) * (direction > 0 ? 1 : -1);
        }
        else
        {
            // For others: right = negative X, left = positive X (original logic)
            scale.x = Mathf.Abs(scale.x) * -direction;
        }

        transform.localScale = scale;
    }   

    private void OnDestroy()
    {
        if (GameManager.GameManagerInstance != null)
            GameManager.GameManagerInstance.UnregisterEntity(this);
    }
}
