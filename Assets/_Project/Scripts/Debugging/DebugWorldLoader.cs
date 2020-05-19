using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugWorldLoader : MonoBehaviour {
    public string debug_folder = "new_save";

    private void Awake () {
        WorldSaving.inst.PrepareNewSave(debug_folder);
    }
}
