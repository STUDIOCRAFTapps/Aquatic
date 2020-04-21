using UnityEngine;

[CreateAssetMenu(fileName = "FishAsset", menuName = "Entities/Assets/Fish")]
public class FishAsset : LivingEntityAsset {
    public float maxTargetingDistance;
    public float minAttakInterval;
    public float speed = 40f;
}
