using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct WeaponPlayerData {
    public PlayerController owner;
    public float timeOfPress;
    public float lastAttackTime;
}

public abstract class BaseWeapon : ScriptableObject {

    new public string name;
    public string id;
    public Sprite icon;
    public WeaponTypes type;

    /// <summary>
    /// This is called the frame the player presses the attack button
    /// </summary>
    public virtual void OnStartAttack (WeaponPlayerData data, Vector2 dir, Vector2 pos, float angle) {
    }

    /// <summary>
    /// This is called for every normal (not fixed) frame the player holds the attack button
    /// </summary>
    public virtual void OnHoldAttack (WeaponPlayerData data, Vector2 dir, Vector2 pos, float angle) {
    }

    /// <summary>
    /// This is called the frame the player releases the attack button
    /// </summary>
    public virtual void OnReleaseAttack (WeaponPlayerData data, Vector2 dir, Vector2 pos, float angle) {
    }

    public virtual WeaponPlayerData CreateWeaponPlayerData () {
        return new WeaponPlayerData();
    }
}

public enum WeaponTypes {
    ClickOnce, //Instant attack, must release to attack again
    ClickHold, //Instant attack, hold to attack again at fixed interval
    ChargeOnce, //Attack on release only, after holding for a certain time
    ChargeHold  //Attack after a fixed time, then repeatly attack after a different fixed time
}
