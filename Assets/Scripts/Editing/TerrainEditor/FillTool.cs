using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FillTool", menuName = "Terrain/Tool/FillTool")]
public class FillTool : BaseToolAsset {

    [Header("Tool Parameters")]
    public bool completeChunkFill = false;
    
    public override void OnPressed (ToolUseInfo info) {
        if(completeChunkFill) {
            Vector2Int chunkPos = TerrainManager.inst.GetChunkPositionAtTile(info.currentPos);

            for(int x = 0; x < TerrainManager.inst.chunkSize; x++) {
                for(int y = 0; y < TerrainManager.inst.chunkSize; y++) {
                    TerrainManager.inst.SetGlobalIDAt(
                        x + chunkPos.x * TerrainManager.inst.chunkSize, 
                        y + chunkPos.y * TerrainManager.inst.chunkSize,
                        info.layer,
                        info.materialID,
                        info.mdc
                    );
                }
            }
        } else {
            Vector2Int cs = TerrainManager.inst.chunkSize * Vector2Int.one;
            if(!TerrainManager.inst.GetGlobalIDAt(info.currentPos.x, info.currentPos.y, info.layer, out int targetID, info.mdc)) {
                return;
            }
            Vector2Int cpos = TerrainManager.inst.GetChunkPositionAtTile(info.currentPos);
            if(info.mdc != null) {
                cs = info.mdc.restrictedSize;
                cpos = Vector2Int.zero;
            }

            if(targetID == info.materialID) {
                return;
            }

            int limit = 0;

            Queue<Vector2Int> q = new Queue<Vector2Int>();
            q.Enqueue(info.currentPos);
            while(q.Count > 0) {
                Vector2Int n = q.Dequeue();
                if(getGID(n.x, n.y, info.layer, info.mdc) != targetID) {
                    continue;
                }
                Vector2Int w = n;
                Vector2Int e = new Vector2Int(n.x + 1, n.y);
                while((local(w, cpos).x >= 0) && (getGID(w.x, w.y, info.layer, info.mdc) == targetID)) {
                    TerrainManager.inst.SetGlobalIDAt(w.x, w.y, info.layer, info.materialID, info.mdc);
                    if((local(w, cpos).y - 1 >= 0) && (getGID(w.x, w.y - 1, info.layer, info.mdc) == targetID))
                        q.Enqueue(new Vector2Int(w.x, w.y - 1));
                    if((local(w, cpos).y + 1 < cs.y) && (getGID(w.x, w.y + 1, info.layer, info.mdc) == targetID))
                        q.Enqueue(new Vector2Int(w.x, w.y + 1));
                    w.x--;
                    limit++;
                    if(limit >= 256) {
                        Debug.LogError("Error!");
                        break;
                    }
                }
                while((local(e, cpos).x < cs.x) && (getGID(e.x, e.y, info.layer, info.mdc) == targetID)) {
                    TerrainManager.inst.SetGlobalIDAt(e.x, e.y, info.layer, info.materialID, info.mdc);
                    if((local(e, cpos).y - 1 >= 0) && (getGID(e.x, e.y - 1, info.layer, info.mdc) == targetID))
                        q.Enqueue(new Vector2Int(e.x, e.y - 1));
                    if((local(e, cpos).y + 1 < cs.y) && (getGID(e.x, e.y + 1, info.layer, info.mdc) == targetID))
                        q.Enqueue(new Vector2Int(e.x, e.y + 1));
                    e.x++;
                    limit++;
                    if(limit >= 256) {
                        Debug.LogError("Error!");
                        break;
                    }
                }
                limit++;
                if(limit >= 256) {
                    Debug.LogError("Error!");
                    break;
                }
            }
        }
        info.mdc?.RefreshTiles();
    }

    public override void OnHold (ToolUseInfo info) {

    }

    public override void OnReleased (ToolUseInfo info) {

    }

    public override void OnSelection (ToolUseInfo info) {
        TerrainEditorManager.inst.PreviewTileAt(info.currentPos.x, info.currentPos.y, info.materialID, info.mdc);
    }

    Vector2Int local (Vector2Int v, Vector2Int cpos) {
        return TerrainManager.inst.GetLocalPositionAtTile(v.x, v.y, cpos);
    }

    int getGID (int x, int y, TerrainLayers layers, MobileDataChunk mdc = null) {
        return TerrainManager.inst.GetGlobalIDAt(x, y, layers, mdc);
    }
}
