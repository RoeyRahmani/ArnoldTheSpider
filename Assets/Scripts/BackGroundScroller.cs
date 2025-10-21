using UnityEngine;

public class BackGroundScroller : MonoBehaviour
{
    public float backgroundHeight; // height of your background sprite
    public BackGroundScroller otherBackground; // assign in inspector or via script

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        float camY = cam.transform.position.y;

        // If this background is too far below the camera, move it above the other
        if (transform.position.y + backgroundHeight < camY)
        {
            Vector3 newPos = otherBackground.transform.position;
            newPos.y += backgroundHeight;
            transform.position = newPos;
        }
        // If this background is too far above the camera, move it below the other
        else if (transform.position.y - backgroundHeight > camY)
        {
            Vector3 newPos = otherBackground.transform.position;
            newPos.y -= backgroundHeight;
            transform.position = newPos;
        }
    }
}

