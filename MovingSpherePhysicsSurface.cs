using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Range(0.1f, 100f)]
    float maxSpeed = 10f;
    [SerializeField, Range(0.1f, 100f)]
    float maxAcc = 10f, maxAirAcc = 1f;
    [Header("Grounded Settings")]
    [SerializeField]
    LayerMask probeMask = -1, stairsMask = -1;
    [SerializeField, Min(0f)]
    float rayToGroundDistance = 1f;
    [SerializeField, Range(0f, 90f)]
    float maxGroundedAngle = 25f, maxStairsAngle = 50f;
    [SerializeField, Range(0f, 100f)]
    float maxSnapSpeed = 100f;
    float minGroundDotProduct, minStairsDotProduct;
    Vector3 contactNormal, steepNormal;

    [Header("Jump Settings")]
    [SerializeField, Range(0f, 10f)]
    float jumpHeight = 2f;
    [SerializeField, Range(0, 5)]
    int maxAirJumps = 0;
    int currentJumps = 0;
    int stepsSinceGrounded, stepsSinceLastJump;
    Vector3 velocity, desiredVelocity;
    Rigidbody rb;
    bool desiredJump;
    int groundContactCount, steepContactCount;
    bool OnGround => groundContactCount > 0;
    bool OnSteep => steepContactCount > 0;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        OnValidate();
    }

    void Update()
    {
        Vector2 playerInput;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);
        desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
        desiredJump |= Input.GetButtonDown("Jump");
    }
    void FixedUpdate()
    {
        UpdateState();
        AdjustVelocity();
        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }
        rb.linearVelocity = velocity;
        ClearState();
    }

    void ClearState()
    {
        groundContactCount = 0;
        steepContactCount = 0;
        contactNormal = Vector3.zero;
        steepNormal = Vector3.zero;
    }

    void UpdateState()
    {
        stepsSinceGrounded++;
        stepsSinceLastJump++;
        velocity = rb.linearVelocity;
        if (OnGround || SnapOntoGround() || CheckSteepContacts())
        {
            stepsSinceGrounded = 0;
            if (stepsSinceLastJump > 0)
            {
                currentJumps = 0;
            }
            if (groundContactCount > 1)
            {
                contactNormal.Normalize();
            }
        }
        else
        {
            contactNormal = Vector3.up;
        }
    }

    void Jump()
    {
        Vector3 jumpDirection;
        if (OnGround)
        {
            jumpDirection = contactNormal;
        }
        else if (OnSteep)
        {
            jumpDirection = steepNormal;
            currentJumps = 0;

        }
        else if (maxAirJumps > 0 && currentJumps <= maxAirJumps)
        {
            if (currentJumps == 0)
            {
                currentJumps = 1;
            }
            jumpDirection = contactNormal;
        }
        else
        {
            return;
        }
        stepsSinceLastJump = 0;
        currentJumps++;
        float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
        jumpDirection = (jumpDirection + Vector3.up).normalized;
        float alignSpeed = Vector3.Dot(velocity, jumpDirection);
        if (alignSpeed > 0f)
        {
            jumpSpeed = Mathf.Max(jumpSpeed - alignSpeed, 0f);
        }
        velocity += jumpDirection * jumpSpeed;

    }

    void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    void OnCollisionExit(Collision collision)
    {
        EvaluateCollision(collision);
    }

    void EvaluateCollision(Collision collision)
    {
        float minDot = GetMinDot(collision.gameObject.layer);
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normalised = collision.GetContact(i).normal;
            if (normalised.y >= minDot)
            {
                groundContactCount++;
                contactNormal += normalised;
            }
            else if (normalised.y > -0.01f)
            {
                steepContactCount += 1;
                steepNormal += normalised;
            }

        }
    }

    void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundedAngle * Mathf.Deg2Rad);
        minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
    }

    Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }

    void AdjustVelocity()
    {
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = OnGround ? maxAcc : maxAirAcc;
        float maxSpeedChange = acceleration * Time.deltaTime;

        float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    bool SnapOntoGround()
    {
        if (stepsSinceGrounded > 1 || stepsSinceLastJump <= 2)
        {
            return false;
        }
        float speed = velocity.magnitude;
        if (speed > maxSnapSpeed)
        {
            return false;
        }
        if (!Physics.Raycast(rb.position, Vector3.down, out RaycastHit hit, rayToGroundDistance, probeMask))
        {
            return false;
        }
        if (hit.normal.y < GetMinDot(hit.collider.gameObject.layer))
        {
            return false;
        }
        groundContactCount = 1;
        contactNormal = hit.normal;
        float dot = Vector3.Dot(velocity, hit.normal);
        if (dot > 0f)
        {
            velocity = (velocity - hit.normal * dot).normalized * speed;
        }
        return true;
    }

    float GetMinDot(int layer)
    {
        return (stairsMask & (1 << layer)) == 0 ?
            minGroundDotProduct : minStairsDotProduct;
    }

    bool CheckSteepContacts()
    {
        if (steepContactCount > 1)
        {
            steepNormal.Normalize();
            if (steepNormal.y >= minGroundDotProduct)
            {
                groundContactCount = 1;
                contactNormal = steepNormal;
                return true;
            }
        }
        return false;
    }
}
