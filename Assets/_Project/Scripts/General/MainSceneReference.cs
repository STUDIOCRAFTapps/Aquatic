using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class MainSceneReference : MonoBehaviour {
    public static MainSceneReference inst;

    public Camera mainCamera;

    public GameObject editorUI;
    public TerrainEditorUI terrainEditorUI;
    public RectTransform[] editorDraggingKnobs;
    public RectTransform editorZOrderingUI;
    public DNAEditorManager dnaEditorUI;

    private void Awake () {
        inst = this;
    }
}
