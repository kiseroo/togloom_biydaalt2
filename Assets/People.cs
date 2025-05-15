using UnityEngine;

public class People : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;        // Walking speed
    [SerializeField] private float moveRadius = 10f;      // How far they can walk from their starting point
    [SerializeField] private float minWaitTime = 2f;      // Minimum time to wait before moving again
    [SerializeField] private float maxWaitTime = 5f;      // Maximum time to wait before moving again

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float waitTimer;
    private bool isWaiting = true;
    private Rigidbody rb;

    void Start()
    {
        startPosition = transform.position;
        
        // Make sure we have a collider
        if (GetComponent<CapsuleCollider>() == null)
        {
            CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.3f;
            col.center = new Vector3(0, 1f, 0);
        }

        // Add Rigidbody to handle collisions
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure rigidbody
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.isKinematic = true; // Prevents physics from moving the character

        SetNewTarget();
    }

    void Update()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                SetNewTarget();
            }
            return;
        }

        // Check if there's something in the way
        RaycastHit hit;
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        if (Physics.Raycast(transform.position + Vector3.up, directionToTarget, out hit, 1f))
        {
            // If we hit a building, get new target
            if (!hit.collider.isTrigger)
            {
                SetNewTarget();
                return;
            }
        }

        // Move towards target
        Vector3 currentPos = transform.position;
        Vector3 movement = Vector3.MoveTowards(currentPos, targetPosition, moveSpeed * Time.deltaTime);
        movement.y = currentPos.y; // Keep the same height
        transform.position = movement;

        // Rotate towards movement direction
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }

        // Check if reached target
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            isWaiting = true;
            waitTimer = Random.Range(minWaitTime, maxWaitTime);
        }
    }

    void SetNewTarget()
    {
        for (int i = 0; i < 5; i++) // Try 5 times to find a valid position
        {
            Vector2 randomCircle = Random.insideUnitCircle * moveRadius;
            Vector3 potentialTarget = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Check if path to target is clear
            RaycastHit hit;
            Vector3 directionToTarget = (potentialTarget - transform.position).normalized;
            if (!Physics.Raycast(transform.position + Vector3.up, directionToTarget, out hit, Vector3.Distance(transform.position, potentialTarget)))
            {
                targetPosition = potentialTarget;
                return;
            }
        }
        
        // If we couldn't find a clear path, just try a closer position
        Vector2 closeCircle = Random.insideUnitCircle * (moveRadius * 0.5f);
        targetPosition = startPosition + new Vector3(closeCircle.x, 0, closeCircle.y);
    }
}
