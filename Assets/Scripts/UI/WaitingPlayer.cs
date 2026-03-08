using UnityEngine;

public class WaitingPlayer : MonoBehaviour
{
    public float rotationSpeed = 180f; // degrees per second

    void Update()
    {
        transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
    }
}
