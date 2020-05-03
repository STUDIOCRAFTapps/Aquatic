using UnityEngine;

[CreateAssetMenu(fileName = "NewCustomTileParticle", menuName = "Effects/Tiles/CustomTileParticle")]
public class CustomTileParticle : ScriptableObject {
    public BaseTileAsset[] tileAssets;
    public float zOffset = 0f;

    public CustomTileParticleUnit[] placingUnits;
    public CustomTileParticleUnit[] breakingUnits;
}

[System.Serializable]
public class CustomTileParticleUnit {
    public CustomParticleModelType model;

    [Header("Texture Parameters")]
    public Sprite texture;
    public Vector2Int textureCount = Vector2Int.one;
    public bool useFPS = false;
    public float fps = 20f;
    public bool randomTextureCyclePositon = false;

    [Header("General Parameters")]
    public Vector2 offset = Vector2.zero;
    public int particleCount = 5;
    public float lifetimeMin = 0.5f;
    public float lifetimeMax = 1f;

    [Header("Shape Parameters")]
    public Vector2 size = Vector2.one;
    public float rotationVariation = 0f;

    [Header("Motion Parameters (Break Particle Only)")]
    public float gravity = 1f;
    public float speedMin = 1f;
    public float speedMax = 7f;
    public Vector2 limitVelocityAxesFactor = Vector2.one;
    public float limitVelocityDampen = 0.1f;
}

public enum CustomParticleModelType {
    BreakingExplosion,
    ApparitionDust,
    BreakParticles,
    SuctionRingParticles,
    WaterSplash,
    Collect
}
