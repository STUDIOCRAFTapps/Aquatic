using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Repeat Weapon", menuName = "Combat/Weapon/Repeat")]
public class RepeatWeapon : BaseWeapon {

    public BaseAttackStrikeAsset baseAttackStrikeAsset;
    public bool doStrikeUseRotation = true;
    public float cooldown = 0.1f;

    public override void OnHoldAttack (ref WeaponPlayerData data, Vector2 dir, Vector2 pos, float angle) {
        float timeDiff = Time.time - data.lastAttackTime;
        data.timeOfPress = Time.time;

        if(timeDiff > cooldown) {
            data.lastAttackTime = Time.time;

            CombatManager.inst.SpawnStrike(
                data.owner, baseAttackStrikeAsset.GetPrefabID(), baseAttackStrikeAsset.GetData(),
                pos, angle, dir,
                Vector2.zero
            );
        }
    }
}
