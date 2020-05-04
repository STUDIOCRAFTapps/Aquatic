using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PenTool", menuName = "Terrain/Tool/PenTool")]
public class PenTool : BaseToolAsset {

    [Header("Tool Parameters")]
    public bool ignoreMaterial = false;

    public override void OnPressed (ToolUseInfo info) {

    }

    public override void OnHold (ToolUseInfo info) {
        info.brushes.PreviewBrush(info.currentPos.x, info.currentPos.y, ignoreMaterial ? 0 : info.materialID, info.brushIndex, info.mdc);
        info.brushes.UseBrush(info.currentPos.x, info.currentPos.y, info.layer, ignoreMaterial ? 0 : info.materialID, info.brushIndex, info.mdc);
        info.mdc?.RefreshTiles();
    }

    public override void OnReleased (ToolUseInfo info) {

    }

    public override void OnSelection (ToolUseInfo info) {
        info.brushes.PreviewBrush(info.currentPos.x, info.currentPos.y, ignoreMaterial ? 0 : info.materialID, info.brushIndex, info.mdc);
    }
}
