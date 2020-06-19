using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flytext : MonoBehaviour {
    public TMPro.TextMeshPro mainText;
    public TMPro.TextMeshPro shadowText;

    float velY;
    float endTime;

    public void Configure (Vector2 pos, string value, float length, float velY) {
        mainText.SetText(value);
        shadowText.SetText(value);
        transform.position = pos;

        endTime = Time.time + length;

        this.velY = velY;
    }

    public void Update () {
        transform.Translate(Vector3.up * velY * Time.deltaTime);

        if(Time.time > endTime) {
            ParticleManager.inst.ReturnFlytext(this);
        }
    }
}
