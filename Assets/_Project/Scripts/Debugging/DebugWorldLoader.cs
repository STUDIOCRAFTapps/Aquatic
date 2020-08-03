using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugWorldLoader : MonoBehaviour {
    public bool doDebug = true;
    public string debug_folder = "new_save";

    private void Awake () {
        if(doDebug) {
            WorldSaving.inst.PrepareNewSave(debug_folder);
        }
    }
}
