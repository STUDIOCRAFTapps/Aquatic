using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashModifierData : PlayerModifierData {
    public float timeOfInitialPress;
    public bool dashStarted;
    public Vector2 dir;
}

[CreateAssetMenu(fileName = "New Dash Modifier Weapon", menuName = "Combat/Weapon/DashModifier")]
public class DashModifierWeapon : PlayerModifierWeapon {

    public bool doUpdateDirection;

    public override void OnStartAttack (ref WeaponPlayerData data, Vector2 dir, Vector2 pos, float angle) {
        ((DashModifierData)data).timeOfInitialPress = Time.time;
        ((DashModifierData)data).dashStarted = true;
        ((DashModifierData)data).dir = dir;
    }

    public override void OnHoldAttack (ref WeaponPlayerData data, Vector2 dir, Vector2 pos, float angle) {
        // Call player to apply module once.
        if(doUpdateDirection) {
            ((DashModifierData)data).dir = dir;
        }
        data.owner.RunWearableModule((WearableModule)module, (PlayerModifierData)data);
    }

    public override WeaponPlayerData CreateWeaponPlayerData () {
        return new DashModifierData();
    }
}