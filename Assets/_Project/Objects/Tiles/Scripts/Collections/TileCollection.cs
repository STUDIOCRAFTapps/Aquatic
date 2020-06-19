using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TileCollection", menuName = "Terrain/Collections/TileCollection")]
public class TileCollection : ScriptableObject {
    new public string name;

    [HideInInspector] public Dictionary<string, BaseTileAsset> tilesByString = new Dictionary<string, BaseTileAsset>();
    public BaseTileAsset[] items = null;

    [HideInInspector] public NamespaceAssetGroup parent;

    public void BuildDictionary () {
        tilesByString.Clear();
        foreach(BaseTileAsset it in items) {
            tilesByString.Add(it.id, it);
        }
    }
}
