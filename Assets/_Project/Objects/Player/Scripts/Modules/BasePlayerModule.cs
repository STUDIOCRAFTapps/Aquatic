using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasePlayerModule : ScriptableObject {

    public virtual void UpdateStatus (PlayerInfo info) {

    }

    public virtual void UpdateAction (PlayerInfo info) {

    }
}

public interface WearableModule {
    void UpdateStatusPMW (PlayerInfo info, PlayerModifierData data);
    void UpdateActionPMW (PlayerInfo info, PlayerModifierData data);
}
