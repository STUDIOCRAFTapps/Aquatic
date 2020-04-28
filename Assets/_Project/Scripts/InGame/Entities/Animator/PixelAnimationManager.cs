using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelAnimationManager : MonoBehaviour {
    public PixelAnimationGroup[] groups;

    void Start () {
        foreach(PixelAnimationGroup group in groups) {
            group.Initialize();
        }
    }
}
