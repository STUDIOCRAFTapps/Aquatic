using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStateGroup", menuName = "Player/StateGroup", order = -1)]
public class PlayerStateGroup : ScriptableObject {
    public BasePlayerModule[] actionModules;

    public void UpdatePlayerStateGroup (PlayerInfo info, List<WearableModuleDataPair> oneFrameModules) {
        foreach(BasePlayerModule action in actionModules) {
            action.UpdateStatus(info);
        }
        for(int i = 0; i < oneFrameModules.Count; i++) {
            oneFrameModules[i].module.UpdateStatusPMW(info, oneFrameModules[i].data);
        }

        foreach(BasePlayerModule action in actionModules) {
            action.UpdateAction(info);
        }
        for(int i = 0; i < oneFrameModules.Count; i++) {
            oneFrameModules[i].module.UpdateActionPMW(info, oneFrameModules[i].data);
        }
    }
}
