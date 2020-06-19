using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Boomerang Attack Strike", menuName = "Combat/AttackStrikes/Boomerang")]
public class BoomerangAttackStrikeAsset : BaseAttackStrikeAsset {
    public int prefabID;
    public BoomerangAttackStrikeData data;

    override public BaseAttackStrikeData GetData () {
        return data;
    }

    override public int GetPrefabID () {
        return prefabID;
    }
}

[System.Serializable]
public class BoomerangAttackStrikeData : BaseAttackStrikeData {
    public float speed;
    public float acceleration;
}
