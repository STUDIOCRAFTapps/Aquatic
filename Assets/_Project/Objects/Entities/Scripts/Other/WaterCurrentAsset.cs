using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaterCurrentAsset", menuName = "Entities/Assets/WaterCurrent")]
public class WaterCurrentAsset : EntityAsset {
    public float force = 2f;
    public float maxMaxLastTime = 4f;
}
