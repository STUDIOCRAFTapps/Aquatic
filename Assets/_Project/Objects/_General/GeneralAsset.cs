using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralAsset : ScriptableObject {
    public NamespaceAssetGroup[] assetGroups;

    // Build texture ressources here
    // Build global id collection here
}

[System.Serializable]
public class NamespaceAssetGroup {
    public string name;
    public string namespaceId;
    public string description;

    public TileCollection[] tileCollections;
    public EntityCollection[] entityCollections;
    public WeaponCollection[] weaponCollections;

    // TODO;
    // - Fixed Particle Assets
    // - Attack Strike Assets
}
