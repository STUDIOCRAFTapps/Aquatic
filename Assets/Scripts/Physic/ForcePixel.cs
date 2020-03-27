using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RigidbodyPixel))]
public class ForcePixel : MonoBehaviour {
    public Vector2 force;
    public float constantFriction = 0f;
}
