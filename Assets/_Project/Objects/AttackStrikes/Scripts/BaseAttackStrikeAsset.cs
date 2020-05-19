using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Attack Strike", menuName = "Combat/AttackStrikes/Base")]
public class BaseAttackStrikeAsset : ScriptableObject {
    public int maxHitCount = 1;
    public bool knockbackUponLast = true;
    public float damage;
    public float knockbackMultiplier = 1f;
    public AttackStrike prefab;
    public int playParticleOnDeath = -1;
}
