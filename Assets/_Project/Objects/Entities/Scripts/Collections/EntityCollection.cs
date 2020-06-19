using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityCollection", menuName = "Entities/Collections/EntityCollection")]
public class EntityCollection : ScriptableObject {
    new public string name;

    [HideInInspector] public Dictionary<string, EntityAsset> entitiesByString = new Dictionary<string, EntityAsset>();
    public EntityAsset[] items = null;

    [HideInInspector] public NamespaceAssetGroup parent;

    public void BuildDictionary () {
        entitiesByString.Clear();
        foreach(EntityAsset ea in items) {
            entitiesByString.Add(ea.id, ea);
        }
    }
}
