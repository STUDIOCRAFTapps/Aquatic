using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelAnimationManager : MonoBehaviour {
    public PixelAnimationGroup[] groups;
    public Color maxFlash;
    public Color minFlash;

    public static PixelAnimationManager inst;

    private void Awake () {
        inst = this;

        foreach(PixelAnimationGroup group in groups) {
            group.Initialize();
        }
    }
}
