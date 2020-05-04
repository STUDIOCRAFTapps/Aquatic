using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackStrike : MonoBehaviour, IPixelAnimationCallbackReciever {

    public PixelAnimator pixelAnimator;

    void Start () {
        pixelAnimator.PlayClip("Strike");
    }

    public virtual void OnRecieveCallback (uint code) {
        if(code == 0) {
            Debug.Log("You got hit");
        } else {
            Debug.Log("Strike ended");
            Debug.Log("Pool me daddy ;3 (This message was written to torture you into implementing pooling for strikes)");
            Destroy(gameObject);
        }
    }
}
