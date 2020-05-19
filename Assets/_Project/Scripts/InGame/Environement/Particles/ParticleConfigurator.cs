using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleConfigurator : MonoBehaviour {

    public bool isAdaptive = true;
    [HideInInspector] public int id;
    public ParticleSystem target;
    new public ParticleSystemRenderer renderer;

    public void Configure (EntityParticleAsset asset) {
        ParticleSystem.TextureSheetAnimationModule tsam = target.textureSheetAnimation;
        ParticleSystem.MainModule main = target.main;

        main.startSizeX = asset.size.x;
        main.startSizeY = asset.size.y;

        tsam.numTilesX = asset.textureCountPerAxis.x;
        tsam.numTilesY = asset.textureCountPerAxis.y;

        main.startLifetime = asset.lifetime;
        renderer.pivot = asset.pivot;
        renderer.flip = asset.flip;

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetTexture("_MainTex", asset.textures.texture);
        renderer.SetPropertyBlock(mpb);
    }

    void Awake () {
        ParticleSystem.MainModule main = target.main;
        main.stopAction = ParticleSystemStopAction.Callback;
    }

    public void Play (Vector3 pos) {
        if(isAdaptive) {
            transform.position = new Vector3(pos.x, Mathf.Floor(pos.y), pos.z);
        } else {
            transform.position = pos;
        }
        target.Emit(1);
    }

    public void OnParticleSystemStopped () {
        /*// Call particle Manager to tell him it's all done now
        ParticleManager.inst.SetStaticEntityParticleAsUnused(id, this);*/
    }
}
