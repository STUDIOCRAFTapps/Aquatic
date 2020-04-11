using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityAsset", menuName = "Entities/Assets/Base")]
public class EntityAsset : ScriptableObject {

    [Header("Presentation")]
    public Sprite uiSprite;
    public string fullName;

    [Header("Entity Data")]
    public string id;
    public Entity prefab;
    [HideInInspector] [NonSerialized] public int globalID = -1;
    [HideInInspector] [NonSerialized] public EntityCollection collection;

    [Header("Loading Box")]
    public Vector2 loadBoxOffset;
    public Vector2 loadBoxSize;
}
