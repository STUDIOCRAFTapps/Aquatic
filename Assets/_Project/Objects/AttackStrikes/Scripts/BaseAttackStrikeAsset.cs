using UnityEngine;

public class BaseAttackStrikeAsset : ScriptableObject, IAttackStrikeAsset {
    virtual public int GetPrefabID () {
        return 0;
    }
    virtual public BaseAttackStrikeData GetData () {
        return null;
    }
}