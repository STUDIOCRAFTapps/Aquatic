using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DNAPropertyBoxSingle : DNAPropertyBox {

    public TMP_InputField singleField;

    public override void Setup (string name, float indentSize, ref List<string> fieldPathReference) {
        base.Setup(name, indentSize, ref fieldPathReference);
    }

    public override void LoadData () {
        singleField.text = ((float)GetDataUsingPath(DNAEditorManager.inst.dataSource, DNAEditorManager.inst.dataSourceType)).ToString();
    }

    public override void ApplyData () {
        if(float.TryParse(singleField.text, out float value)) {
            SetDataUsingPath(DNAEditorManager.inst.dataSource, value, DNAEditorManager.inst.dataSourceType);
        }
    }
}
