using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "SmoothBrush", menuName = "Terrain/Brushes/SmoothBrush")]
public class SmoothBrush : BaseBrushAsset {

    [Header("Brush Parameters")]
    public int birthPoint = 4;
    public int deathPoint = 4;

    protected int[][,] tempGID;

    public override void BuildBrushes () {
        base.BuildBrushes();

        tempGID = new int[bitmasks.Length][,];
        for(int i = 0; i < bitmasks.Length; i++) {
            tempGID[i] = new int[bitmasks[i].GetLength(0), bitmasks[i].GetLength(1)];
        }
    }

    public override void UseBrush (int x, int y, TerrainLayers layer, int globalID, int brushIndex, MobileDataChunk mdc) {
        int w = bitmasks[brushIndex].GetLength(0);
        int h = bitmasks[brushIndex].GetLength(1);

        CopyTemp(x, y, layer, brushIndex, mdc);
        for(int px = 0; px < w; px++) {
            for(int py = 0; py < h; py++) {
                if(bitmasks[brushIndex][px, py] == 1) {
                    int nc = GetNeighbourCount(x + px - Mathf.FloorToInt(w * 0.5f), y + py - Mathf.FloorToInt(h * 0.5f), layer, mdc);
                    if(nc > birthPoint) {
                        tempGID[brushIndex][px, py] = globalID;
                    } else if(nc < deathPoint) {
                        tempGID[brushIndex][px, py] = 0;
                    }
                }
            }
        }
        PasteTemp(x, y, layer, brushIndex, mdc);
    }

    public override void PreviewBrush (int x, int y, int globalID, int brushIndex, MobileDataChunk mdc) {
        int w = bitmasks[brushIndex].GetLength(0);
        int h = bitmasks[brushIndex].GetLength(1);

        for(int px = 0; px < w; px++) {
            for(int py = 0; py < h; py++) {
                if(bitmasks[brushIndex][px, py] == 1) {
                    TerrainEditorManager.inst.PreviewTileAt(x + px - Mathf.FloorToInt(w * 0.5f), y + py - Mathf.FloorToInt(h * 0.5f), globalID, mdc);
                }
            }
        }
    }

    int GetNeighbourCount (int x, int y, TerrainLayers layer, MobileDataChunk mdc) {
        int c = 0;
        for(int px = -1; px < 2; px++) {
            for(int py = -1; py < 2; py++) {
                if(px == 0 && py == 0) {
                    continue;
                }
                if(TerrainManager.inst.GetGlobalIDAt(x + px, y + py, layer, out int checkID, mdc)) {
                    if(checkID != 0) c++;
                }
            }
        }
        return c;
    }

    void CopyTemp (int x, int y, TerrainLayers layer, int brushIndex, MobileDataChunk mdc) {
        int w = bitmasks[brushIndex].GetLength(0);
        int h = bitmasks[brushIndex].GetLength(1);

        for(int px = 0; px < w; px++) {
            for(int py = 0; py < h; py++) {
                if(bitmasks[brushIndex][px, py] != 1) {
                    continue;
                }
                if(TerrainManager.inst.GetGlobalIDAt(x + px - Mathf.FloorToInt(w * 0.5f), y + py - Mathf.FloorToInt(h * 0.5f), layer, out int checkID, mdc)) {
                    tempGID[brushIndex][px, py] = checkID;
                }
            }
        }
    }

    void PasteTemp (int x, int y, TerrainLayers layer, int brushIndex, MobileDataChunk mdc) {
        int w = bitmasks[brushIndex].GetLength(0);
        int h = bitmasks[brushIndex].GetLength(1);

        for(int px = 0; px < w; px++) {
            for(int py = 0; py < h; py++) {
                if(bitmasks[brushIndex][px, py] != 1) {
                    continue;
                }
                if(TerrainManager.inst.GetGlobalIDAt(x + px - Mathf.FloorToInt(w * 0.5f), y + py - Mathf.FloorToInt(h * 0.5f), layer, out int checkID, mdc)) {
                    if(checkID != tempGID[brushIndex][px, py]) {
                        TerrainManager.inst.SetGlobalIDAt(x + px - Mathf.FloorToInt(w * 0.5f), y + py - Mathf.FloorToInt(h * 0.5f), layer, tempGID[brushIndex][px, py], mdc);
                    }
                }
            }
        }
    }
}
