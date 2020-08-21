using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class BackgroundManager : MonoBehaviour {
    public float scaleFactor;
    public float backgroundHeight = 0.5f;

    public int layerCount = 5;
    public Sprite[] backgroundSprites;
    public int defaultOrderInLayer = -100;
    public int orderInLayerSteps = -10;
    public SpriteRenderer layerTemplate;

    public PixelPerfectCamera ppc;
    public Camera cam;
    public Vector3 cameraOriginOffset;
    public float[] scrollFactor;
    public float[] autoScrollFactor;

    MaterialPropertyBlock[] mpb;
    SpriteRenderer[] layers;
    Vector2 previousCamSize;

    void Start () {
        layers = new SpriteRenderer[layerCount];
        mpb = new MaterialPropertyBlock[layerCount];
        for(int i = 0; i < layerCount; i++) {
            mpb[i] = new MaterialPropertyBlock();
            layers[i] = Instantiate(layerTemplate, layerTemplate.transform.parent);
            layers[i].sprite = backgroundSprites[i];
            layers[i].sortingOrder = defaultOrderInLayer + i * orderInLayerSteps;
            layers[i].gameObject.SetActive(true);
        }

        CreateLayers();
        RearangeLayers();
        previousCamSize = GetCamSize();

        CameraNavigator.inst.OnPostApplyLerpEvent += RearangeLayers;
    }

    private void OnDestroy () {
        if(CameraNavigator.inst != null) {
            CameraNavigator.inst.OnPostApplyLerpEvent -= RearangeLayers;
        }
    }

    void Update () {
        Vector2 currentCamSize = GetCamSize();
        if(!Mathf.Approximately(currentCamSize.x, previousCamSize.x) || !Mathf.Approximately(currentCamSize.y, previousCamSize.y)) {
            CreateLayers();
        }
    }

    public void CreateLayers () {
        Vector2 camSize = GetCamSize();
        previousCamSize = camSize;
        cameraOriginOffset.x = camSize.x * (scaleFactor * 0.5f) - 0.5f;

        RearangeLayers();
    }

    void RearangeLayers () {
        if(scaleFactor == 0f) {
            scaleFactor = 1f;
        }

        Vector3 camAnchor = -cameraOriginOffset;
        camAnchor.z = 0f;
        transform.position = ppc.RoundToPixel(new Vector3(cam.transform.position.x, cam.transform.position.y, transform.position.z));
        transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
        float subSizeX = 1f / layerCount;

        for(int l = 0; l < layerCount; l++) {
            mpb[l].SetVector("_scroll", new Vector4(
                (cam.transform.position.x * scrollFactor[l] + (autoScrollFactor[l] * Time.time)) / backgroundSprites[l].bounds.size.x,
                (cam.transform.position.y * scrollFactor[l]) / backgroundSprites[l].bounds.size.y
            ));
            mpb[l].SetVector("_bounds", new Vector4(
                backgroundSprites[l].uv[0].x,
                backgroundSprites[l].uv[3].x,
                backgroundSprites[l].uv[0].y,
                backgroundSprites[l].uv[3].y
            ));
            
            mpb[l].SetVector("_scale", new Vector4(scaleFactor, scaleFactor));
            mpb[l].SetTexture("_MainTex", backgroundSprites[l].texture);
            layers[l].SetPropertyBlock(mpb[l]);

            /*if(layers[ind].sprite != null) {
                Vector2 layerSize = layers[ind].sprite.bounds.size;
                Vector2 scroll = -cam.transform.position * scrollFactor[l] + (autoScrollFactor[l] * Time.time * Vector3.right);
                Vector2 offset = layers[ind].transform.localPosition;

                for(int c = 0; c < copyLayers[ind].Count; c++) {
                    copyLayers[ind][c].localPosition = new Vector3(
                        (camAnchor.x - layerSize.x) + (c * layerSize.x) + Mathf.Repeat(scroll.x, layerSize.x),
                        camAnchor.y + scroll.y + backgroundHeight + (1 - h) * (layerSize.y * (1f - scaleFactor))
                    ) + (Vector3)offset;
                }
            }*/
        }
    }

    Vector2 GetCamSize () {
        return cam.ViewportToWorldPoint(Vector2.one) - cam.ViewportToWorldPoint(Vector2.zero);
    }
}
