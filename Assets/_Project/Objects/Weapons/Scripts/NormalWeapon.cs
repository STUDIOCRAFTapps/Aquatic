using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Normal Weapon", menuName = "Combat/Weapon/Normal")]
public class NormalWeapon : BaseWeapon {

    public int strikeID = 0;
    public bool doStrikeUseRotation = true;
    public float cooldown = 0.1f;

    public override void OnStartAttack (WeaponPlayerData data, Vector2 dir, Vector2 pos, float angle) {
        float timeDiff = Time.time - data.lastAttackTime;
        data.timeOfPress = Time.time;

        if(timeDiff > cooldown) {
            data.lastAttackTime = Time.time;

            CombatManager.inst.SpawnStrike(
                data.owner, strikeID,
                pos, angle, dir,
                Vector2.zero
            );
        }
    }
}
