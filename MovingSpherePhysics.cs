using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Range(0.1f, 100f)]
    float maxSpeed = 10f;
    [SerializeField, Range(0.1f, 100f)]
    float maxAcc = 10f, maxAirAcc = 1f;
    [SerializeField, Range(0f, 90f)]
    float maxGroundedAngle = 25f;
    float minGroundDotProduct;
    Vector3 contactNormal;
    [Header("Jump Settings")]
    [SerializeField, Range(0f, 10f)]
    float jumpHeight = 2f;
    [SerializeField, Range(0, 5)]
    int maxAirJumps = 0;
    int currentJumps = 0;
    Vector3 velocity, desiredVelocity;
    Rigidbody rb;
    bool desiredJump;
    int groundContactCount;
    bool OnGround => groundContactCount > 0;
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
        contactNormal = Vector3.zero;
    }

    void UpdateState()
    {
        velocity = rb.linearVelocity;
        if (OnGround)
        {
            currentJumps = 0;
            if (groundContactCount > 1) {
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
        if (OnGround || currentJumps < maxAirJumps)
        {
            currentJumps++;
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
            float alignSpeed = Vector3.Dot(velocity, contactNormal);
            if (alignSpeed > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - alignSpeed, 0f);
            }
            velocity += contactNormal * jumpSpeed;
        }
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
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normalised = collision.GetContact(i).normal;
            if (normalised.y >= minGroundDotProduct)
            {
                groundContactCount++;
                contactNormal += normalised;
            }
        }

    }

    void OnValidate()
    {
        minGroundDotProduct = Mathf.Sin(maxGroundedAngle * Mathf.Deg2Rad);
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
}
