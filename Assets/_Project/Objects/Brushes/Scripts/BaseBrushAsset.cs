using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MLAPI;
using MLAPI.Messaging;

[CreateAssetMenu(fileName = "BaseBrush", menuName = "Terrain/Brushes/BaseBrush")]
public class BaseBrushAsset : ScriptableObject {

    public int gid = -1;

    [Header("Presentation")]
    public Sprite[] uiSprites;
    
    [Header("Data")]
    public Sprite[] brushesData;

    protected int[][,] bitmasks;

    public virtual void BuildBrushes () {
        bitmasks = new int[brushesData.Length][,];
        for(int i = 0; i < brushesData.Length; i++) {
            int w = Mathf.CeilToInt(brushesData[i].textureRect.width);
            int h = Mathf.CeilToInt(brushesData[i].textureRect.height);
            Color[] brushColors = brushesData[i].texture.GetPixels(
                Mathf.FloorToInt(brushesData[i].textureRect.x),
                Mathf.FloorToInt(brushesData[i].textureRect.y),
                w, h
            );
            bitmasks[i] = new int[w, h];
            for(int x = 0; x < w; x++) {
                for(int y = 0; y < h; y++) {
                    bitmasks[i][x, y] = brushColors[x + y * w].a > 0.5f ? 1 : 0;
                }
            }
        }
    }

    public virtual void UseBrush (int x, int y, TerrainLayers layer, int globalID, int brushIndex, MobileDataChunk mdc = null, bool replicateOnServer = true) {
        int w = bitmasks[brushIndex].GetLength(0);
        int h = bitmasks[brushIndex].GetLength(1);

        for(int px = 0; px < w; px++) {
            for(int py = 0; py < h; py++) {
                if(bitmasks[brushIndex][px, py] == 1) {
                    if(TerrainManager.inst.GetGlobalIDAt(x + px - Mathf.FloorToInt(w * 0.5f), y + py - Mathf.FloorToInt(h * 0.5f), layer, out int checkID, mdc)) {
                        if(checkID != globalID) {
                            TerrainManager.inst.SetGlobalIDAt(x + px - Mathf.FloorToInt(w * 0.5f), y + py - Mathf.FloorToInt(h * 0.5f), layer, globalID, mdc, replicateOnServer);
                        }
                    }
                }
            }
        }
    }

    public virtual void PreviewBrush (int x, int y, int globalID, int brushIndex, MobileDataChunk mdc = null) {
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

    public int GetMaxRadius (int brushIndex) {
        int w = bitmasks[brushIndex].GetLength(0);
        int h = bitmasks[brushIndex].GetLength(1);

        return Mathf.Max(w, h);
    }
}
