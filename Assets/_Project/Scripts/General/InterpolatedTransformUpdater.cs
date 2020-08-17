using UnityEngine;
using System.Collections;

public class InterpolatedTransformUpdater : MonoBehaviour {
    private InterpolatedTransform interpolatedTransform;

    void Awake () {
        interpolatedTransform = GetComponent<InterpolatedTransform>();
    }

    void FixedUpdate () {
        interpolatedTransform.LateFixedUpdate();
    }
}