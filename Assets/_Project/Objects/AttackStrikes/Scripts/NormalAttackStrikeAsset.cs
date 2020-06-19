using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAttackStrikeAsset {
    BaseAttackStrikeData GetData();
    int GetPrefabID ();
}

[CreateAssetMenu(fileName = "New Attack Strike", menuName = "Combat/AttackStrikes/Base")]
public class NormalAttackStrikeAsset : BaseAttackStrikeAsset {
    public int prefabID;
    public BaseAttackStrikeData data;

    override public BaseAttackStrikeData GetData () {
        return data;
    }

    override public int GetPrefabID () {
        return prefabID;
    }
}

[System.Serializable]
public class BaseAttackStrikeData {
    public int maxHitCount = 1;
    public bool knockbackUponLast = true;
    public float damage;
    public float knockbackMultiplier = 1f;
    public int playParticleOnDeath = -1;
}
