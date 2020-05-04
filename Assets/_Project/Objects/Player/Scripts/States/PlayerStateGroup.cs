using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStateGroup", menuName = "Player/StateGroup", order = -1)]
public class PlayerStateGroup : ScriptableObject {
    public BasePlayerModule[] actionModules;

    public void UpdatePlayerStateGroup (PlayerInfo info) {
        UpdateAllModules(info);
    }

    void UpdateAllModules (PlayerInfo info) {
        foreach(BasePlayerModule action in actionModules) {
            action.UpdateStatus(info);
        }
        foreach(BasePlayerModule action in actionModules) {
            action.UpdateAction(info);
        }
    }
}
