using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopePart : MonoBehaviour
{
    public Vector3 velocity;
    static Vector3 gravity = Vector3.down * 10;
    public bool isKinematic = false;
    public void SimulateVelocity(float deltaTime)
    {
        if (isKinematic == false)
        {
            velocity += gravity * deltaTime;
            transform.position += velocity * deltaTime;
        }
        else
        {
            velocity = Vector3.zero;
        }
    }
}
