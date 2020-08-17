using System.Collections;
using System.Collections.Generic;
using MLAPI.Serialization;
using UnityEngine;

[CreateAssetMenu(fileName = "PenTool", menuName = "Terrain/Tool/PenTool")]
public class PenTool : BaseToolAsset {

    [Header("Tool Parameters")]
    public bool ignoreMaterial = false;

    public override void OnPressed (ToolUseInfo info) {

    }

    public override void OnHold (ToolUseInfo info) {

        info.brushes.PreviewBrush(info.currentPos.x, info.currentPos.y, ignoreMaterial ? 0 : info.materialID, info.brushIndex, info.mdc);

        if(!info.hasMovedSinceLastFrame) {
            return;
        }

        if(info.brushes.GetMaxRadius(info.brushIndex) <= 0 || info.mdc != null) {
            info.brushes.UseBrush(info.currentPos.x, info.currentPos.y, info.layer, ignoreMaterial ? 0 : info.materialID, info.brushIndex, info.mdc);
            info.mdc?.RefreshTiles();
        } else {
            TerrainManager.inst.UseToolNetworkRequest(this, (writer) => {
                writer.WriteInt32(info.brushes.gid);
                writer.WriteByte((byte)info.brushIndex);

                writer.WriteInt32(info.currentPos.x);
                writer.WriteInt32(info.currentPos.y);
                writer.WriteByte((byte)info.layer);
                writer.WriteInt32(ignoreMaterial ? 0 : info.materialID);
            });
        }
    }

    public override void OnReleased (ToolUseInfo info) {

    }

    public override void OnSelection (ToolUseInfo info) {
        info.brushes.PreviewBrush(info.currentPos.x, info.currentPos.y, ignoreMaterial ? 0 : info.materialID, info.brushIndex, info.mdc);
    }

    public override void DecodeAction (BitReader reader) {
        int brushGid = reader.ReadInt32();
        int brushIndex = reader.ReadByte();
        int x = reader.ReadInt32();
        int y = reader.ReadInt32();
        TerrainLayers layer = (TerrainLayers)reader.ReadByte();
        int gid = reader.ReadInt32();

        BaseBrushAsset bba = GeneralAsset.inst.GetBrushFromGlobalID(brushGid);
        bba.UseBrush(x, y, layer, gid, brushIndex, null, false);
    }
}
