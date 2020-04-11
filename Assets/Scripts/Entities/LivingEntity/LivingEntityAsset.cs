using UnityEngine;

[CreateAssetMenu(fileName = "LivingEntityAsset", menuName = "Entities/Assets/LivingEntity")]
public class LivingEntityAsset : EntityAsset {

    [Header("Custom")]
    public Vector2 offset;
    public Vector2 size;
    public float mass;

    public float maxHealth;
}
