using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour {

    public static CombatManager inst;

    public AttackStrike[] allAttackStrikePrefabs;

    public Queue<AttackStrike>[] attackStrikesPool;

    void Awake () {
        inst = this;

        attackStrikesPool = new Queue<AttackStrike>[allAttackStrikePrefabs.Length];
        for(int i = 0; i < allAttackStrikePrefabs.Length; i++) {
            attackStrikesPool[i] = new Queue<AttackStrike>();
        }
    }

    public void SetStrikeAsUnused (int assetID, AttackStrike attackStrike) {
        attackStrike.gameObject.SetActive(false);
        attackStrikesPool[assetID].Enqueue(attackStrike);
    }

    public void SpawnStrike (PlayerController owner, int prefabID, BaseAttackStrikeData data, Vector2 pos, float rotation, Vector2 direction, Vector2 additionnalVel) {
        AttackStrike newStrike;
        if(attackStrikesPool[prefabID].Count > 0) {
            newStrike = attackStrikesPool[prefabID].Dequeue();
            newStrike.gameObject.SetActive(true);
        } else {
            newStrike = Instantiate(allAttackStrikePrefabs[prefabID], transform);
            newStrike.assetID = prefabID;
        }

        newStrike.Init(owner, data, additionnalVel, direction);
        newStrike.transform.position = (Vector3)pos + Vector3.back * 0.25f;
        newStrike.transform.eulerAngles = Vector3.forward * rotation;
    }

    public void AttackPlayers (Bounds2D bounds, float damage) {
        foreach(PlayerController pc in GameManager.inst.allPlayers) {
            if(bounds.Overlaps(pc.rbody.aabb)) {
                pc.DamagePlayer(bounds.center, damage);
            }
        }
    }
}
