using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityCollection", menuName = "Entities/Collections/EntityCollection")]
public class EntityCollection : ScriptableObject {
    public string id;

    [HideInInspector] public Dictionary<string, EntityAsset> entitiesByString = new Dictionary<string, EntityAsset>();
    public EntityAsset[] items = null;

    public void BuildDictionary () {
        entitiesByString.Clear();
        foreach(EntityAsset ea in items) {
            entitiesByString.Add(ea.id, ea);
        }
    }
}
