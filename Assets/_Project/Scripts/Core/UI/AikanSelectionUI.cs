using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AikanSelectionUI : MonoBehaviour {
    public int uiID;
    public int aikanID;
    public bool inOptions;

    public void SelectAikan () {
        if(inOptions) {
            SavesMenuManager.inst.saveOptionMenuManager.SelectAikan(uiID, aikanID);
        } else {
            SavesMenuManager.inst.SelectAikan(uiID, aikanID);
        }
    }
}
