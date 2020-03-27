using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "DragTool", menuName = "Terrain/Tool/DragTool")]
public class DragTool : BaseToolAsset {

    [Header("Tool Parameters")]
    public DragToolAlgorithm algorithm = DragToolAlgorithm.Box;
    public bool reorderMinMax;

    public override void OnPressed (ToolUseInfo info) {

    }

    public override void OnHold (ToolUseInfo info) {
        ExecuteIn(GetMin(info.initPos, info.currentPos, reorderMinMax), GetMax(info.initPos, info.currentPos, reorderMinMax), info, (x, y, ninfo) => PreviewTile(x, y, ninfo));
    }

    public override void OnReleased (ToolUseInfo info) {
        ExecuteIn(GetMin(info.initPos, info.currentPos, reorderMinMax), GetMax(info.initPos, info.currentPos, reorderMinMax), info, (x, y, ninfo) => PlaceTile(x, y, ninfo));
    }

    public override void OnSelection (ToolUseInfo info) {
        TerrainEditorManager.inst.PreviewTileAt(info.currentPos.x, info.currentPos.y, info.materialID, info.mdc);
    }

    public void ExecuteIn (Vector2Int min, Vector2Int max, ToolUseInfo info, Action<int, int, ToolUseInfo> action) {
        if(algorithm == DragToolAlgorithm.Box) {
            for(int x = min.x; x <= max.x; x++) {
                for(int y = min.y; y <= max.y; y++) {
                    action(x, y, info);
                }
            }
        } else if(algorithm == DragToolAlgorithm.Line) {
            int x = min.x;
            int y = min.y;

            int dx = Math.Abs(max.x - min.x), sx = min.x < max.x ? 1 : -1;
            int dy = Math.Abs(max.y - min.y), sy = min.y < max.y ? 1 : -1;
            int err = (dx > dy ? dx : -dy) / 2, e2;

            int size = 0;

            for(;;) {
                action(x, y, info);
                if(x == max.x && y == max.y)
                    break;
                e2 = err;
                if(e2 > -dx) {
                    err -= dy;
                    x += sx;
                }
                if(e2 < dy) {
                    err += dx;
                    y += sy;
                }
                size++;
                if(size >= 64) {
                    Debug.LogError("Line size exceeded 64");
                    break;
                }
            }
        }
        info.mdc?.RefreshTiles();
    }

    public void PlaceTile (int x, int y, ToolUseInfo info) {
        if(TerrainManager.inst.GetGlobalIDAt(x, y, info.layer, out int globalId, info.mdc)) {
            if(globalId != info.materialID) {
                TerrainManager.inst.SetGlobalIDAt(x, y, info.layer, info.materialID, info.mdc);
            }
        }
    }

    public void PreviewTile (int x, int y, ToolUseInfo info) {
        TerrainEditorManager.inst.PreviewTileAt(x, y, info.materialID, info.mdc);
    }

    public Vector2Int GetMin (Vector2Int v0, Vector2Int v1, bool reorder) {
        return reorder?new Vector2Int(Mathf.Min(v0.x, v1.x), Mathf.Min(v0.y, v1.y)):v0;
    }

    public Vector2Int GetMax (Vector2Int v0, Vector2Int v1, bool reorder) {
        return reorder?new Vector2Int(Mathf.Max(v0.x, v1.x), Mathf.Max(v0.y, v1.y)):v1;
    }
}

public enum DragToolAlgorithm {
    Line,
    Box
}
