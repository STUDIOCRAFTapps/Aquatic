using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Entity Particle Asset", menuName = "Effects/Entities/EntityParticleAsset")]
public class EntityParticleAsset : ScriptableObject {
    public Sprite textures;
    public Vector2 size = Vector2.one;
    public Vector2Int textureCountPerAxis = Vector2Int.one;
    public float lifetime = 0.5f;
    public Vector2 pivot;
    public Vector2 flip = Vector2.zero;
}
