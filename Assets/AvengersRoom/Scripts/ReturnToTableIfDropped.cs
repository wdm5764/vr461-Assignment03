using UnityEngine;

// The player is stationary, so a puzzle part that falls off the table would be lost.
// Add this to any grabbable part: if it drops below the table, it snaps back to where it started.
public class ReturnToTableIfDropped : MonoBehaviour
{
    [Tooltip("World height below which the part is considered dropped (table top is 0.9 m).")]
    public float dropHeight = 0.6f;

    [Tooltip("Seconds the part must stay below dropHeight before it resets.")]
    public float delay = 1f;

    Vector3 startPosition;
    Quaternion startRotation;
    Rigidbody body;
    float belowTimer;

    void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        body = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (transform.position.y >= dropHeight)
        {
            belowTimer = 0f;
            return;
        }

        belowTimer += Time.deltaTime;
        if (belowTimer >= delay) ResetToStart();
    }

    public void ResetToStart()
    {
        belowTimer = 0f;
        if (body != null && !body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        transform.SetPositionAndRotation(startPosition, startRotation);
    }
}
