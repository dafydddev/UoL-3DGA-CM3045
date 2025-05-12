using System;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [SerializeField, Range(0.1f, 100f)]
    float maxSpeed = 10f;
    [SerializeField, Range(0.1f, 100f)]
    float maxAcc = 10f;
    [SerializeField]
    Rect allowedZone = new Rect(-5f, -5f, 10f, 10f);
    [SerializeField, Range(0f, 1f)]
    float bounceFactor = 0.5f;
    private Vector3 velocity;
    void Update()
    {
        Vector2 playerInput;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);
        Vector3 desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
        float maxSpeedChange = maxAcc * Time.deltaTime;
        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
        velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);
        Vector3 displacement = velocity * Time.deltaTime;
        Vector3 newPosition = transform.localPosition + displacement;
        if (newPosition.x < allowedZone.xMin)
        {
            newPosition.x = allowedZone.xMin;
            velocity.x = -velocity.x * bounceFactor;
        }
        else if (newPosition.x > allowedZone.xMax)
        {
            newPosition.x = allowedZone.xMax;
            velocity.x = -velocity.x * bounceFactor;
        }
        if (newPosition.z < allowedZone.yMin)
        {
            newPosition.z = allowedZone.yMin;
            velocity.z = -velocity.z * bounceFactor;
        }
        else if (newPosition.z > allowedZone.yMax)
        {
            newPosition.z = allowedZone.yMax;
            velocity.z = -velocity.z * bounceFactor;
        }
        transform.localPosition = newPosition;
    }
}
