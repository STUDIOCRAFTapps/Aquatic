using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour {

    public static CombatManager inst;

    public BaseAttackStrikeAsset[] allAttackStrikeAssets;


    public Queue<AttackStrike>[] attackStrikesPool;

    void Awake () {
        inst = this;

        attackStrikesPool = new Queue<AttackStrike>[allAttackStrikeAssets.Length];
        for(int i = 0; i < allAttackStrikeAssets.Length; i++) {
            attackStrikesPool[i] = new Queue<AttackStrike>();
        }
    }

    public void SetStrikeAsUnused (int assetID, AttackStrike attackStrike) {
        attackStrike.gameObject.SetActive(false);
        attackStrikesPool[assetID].Enqueue(attackStrike);
    }

    public void SpawnStrike (PlayerController owner, int assetID, Vector2 pos, float rotation, Vector2 direction, Vector2 additionnalVel) {
        AttackStrike newStrike;
        if(attackStrikesPool[assetID].Count > 0) {
            newStrike = attackStrikesPool[assetID].Dequeue();
            newStrike.gameObject.SetActive(true);
        } else {
            newStrike = Instantiate(allAttackStrikeAssets[assetID].prefab, transform);
            newStrike.assetID = assetID;
        }

        newStrike.Init(owner, allAttackStrikeAssets[assetID], additionnalVel, direction);
        newStrike.transform.position = (Vector3)pos + Vector3.back * 0.25f;
        newStrike.transform.eulerAngles = Vector3.forward * rotation;
    }
}
