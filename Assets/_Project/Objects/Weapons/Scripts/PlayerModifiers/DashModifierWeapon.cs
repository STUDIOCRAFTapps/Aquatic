using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashModifierData : PlayerModifierData {
    public float pressLength;
    public float cooldownStartTime;
    public Vector2 summonPos;
    public bool dashStarted;
    public bool clickStarted;
    public Vector2 dir;
}

[CreateAssetMenu(fileName = "New Dash Modifier Weapon", menuName = "Combat/Weapon/DashModifier")]
public class DashModifierWeapon : PlayerModifierWeapon {
    
    public bool doUpdateDirection;

    public override void OnStartAttack (ref WeaponPlayerData data, Vector2 dir, Vector2 pos, float angle) {
        if(Time.time - ((DashModifierData)data).cooldownStartTime >= ((DashModule)module).maxCooldownTime) {
            ((DashModifierData)data).clickStarted = true;
            ((DashModifierData)data).dashStarted = true;
            ((DashModifierData)data).dir = dir;
            ((DashModifierData)data).summonPos = pos;
        }
    }

    public override void OnHoldAttack (ref WeaponPlayerData data, Vector2 dir, Vector2 pos, float angle) {
        // Call player to apply module once.
        if(doUpdateDirection) {
            ((DashModifierData)data).dir = dir;
        }
        if(((DashModifierData)data).clickStarted) {
            data.owner.RunWearableModule((WearableModule)module, (PlayerModifierData)data);
        }
    }

    public override void OnReleaseAttack (ref WeaponPlayerData data, Vector2 dir, Vector2 pos, float angle) {
        base.OnReleaseAttack(ref data, dir, pos, angle);
    }

    public override WeaponPlayerData CreateWeaponPlayerData () {
        return new DashModifierData();
    }
}