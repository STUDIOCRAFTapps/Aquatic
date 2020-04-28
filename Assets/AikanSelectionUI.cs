using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AikanSelectionUI : MonoBehaviour {
    public int uiID;
    public int aikanID;

    public void SelectAikan () {
        SavesMenuManager.inst.SelectAikan(uiID, aikanID);
    }
}
