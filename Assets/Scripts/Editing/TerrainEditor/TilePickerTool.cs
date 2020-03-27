using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TilePickerTool", menuName = "Terrain/Tool/TilePickerTool")]
public class TilePickerTool : BaseToolAsset {
    
    public override void OnPressed (ToolUseInfo info) {

    }

    public override void OnHold (ToolUseInfo info) {
        if(TerrainManager.inst.GetGlobalIDAt(info.currentPos.x, info.currentPos.y, info.layer, out int globalId, info.mdc)) {
            TerrainEditorManager.inst.selectedMaterialID = globalId;
        }
    }

    public override void OnReleased (ToolUseInfo info) {

    }

    public override void OnSelection (ToolUseInfo info) {
        TerrainEditorManager.inst.PreviewTileAt(info.currentPos.x, info.currentPos.y, info.materialID, info.mdc);
    }
}
