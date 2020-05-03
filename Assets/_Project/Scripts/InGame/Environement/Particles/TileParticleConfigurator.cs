using UnityEngine;

public class TileParticleConfigurator : MonoBehaviour {
    [HideInInspector] public int modelType;
    public ParticleSystem target;
    new public ParticleSystemRenderer renderer;

    private CustomTileParticle reference;
    CustomTileParticleUnit unit;
    private int unitID;
    private MaterialPropertyBlock mpb;

    private void Awake () {
        mpb = new MaterialPropertyBlock();
    }

    public void PlayPlace (CustomTileParticle reference, int unitID, Vector2Int pos, MobileDataChunk mdc = null) {
        this.reference = reference;
        this.unitID = unitID;

        transform.position = new Vector3(pos.x + 0.5f, pos.y + 0.5f, reference.zOffset) + ((mdc != null) ? mdc.mobileChunk.position : Vector3.zero);
        unit = reference.placingUnits[unitID];
        Configure();

        Emit(true);
    }

    public void PlayBreak (CustomTileParticle reference, int unitID, Vector2Int pos, MobileDataChunk mdc = null) {
        this.reference = reference;
        this.unitID = unitID;

        unit = reference.breakingUnits[unitID];
        transform.position = new Vector3(pos.x + 0.5f + unit.offset.x, pos.y + 0.5f + unit.offset.y, reference.zOffset) + ((mdc != null) ? mdc.mobileChunk.position : Vector3.zero);
        Configure();

        Emit(false);
    }

    public void OnParticleSystemStopped () {
        ParticleManager.inst.SetTileParticleAsUnused(modelType, this);
    }

    private void Configure () {
        CustomParticleModelType model = unit.model;

        bool supportCustomization = model == CustomParticleModelType.BreakParticles || model == CustomParticleModelType.SuctionRingParticles;
        bool supportMotion = model == CustomParticleModelType.BreakParticles;

        ParticleSystem.TextureSheetAnimationModule tsam = target.textureSheetAnimation;
        ParticleSystem.MainModule main = target.main;
        ParticleSystem.LimitVelocityOverLifetimeModule lvolm = target.limitVelocityOverLifetime;
        main.stopAction = ParticleSystemStopAction.Callback;

        if(supportCustomization) {
            tsam.numTilesX = unit.textureCount.x;
            tsam.numTilesY = unit.textureCount.y;
            tsam.timeMode = unit.useFPS ? ParticleSystemAnimationTimeMode.FPS : ParticleSystemAnimationTimeMode.Lifetime;
            tsam.fps = unit.fps;
            tsam.startFrame = unit.randomTextureCyclePositon ? new ParticleSystem.MinMaxCurve(0f, tsam.numTilesX - 1f) : new ParticleSystem.MinMaxCurve(0f);
            
            main.startLifetime = new ParticleSystem.MinMaxCurve(unit.lifetimeMin, unit.lifetimeMax);
            main.startSizeX = unit.size.x;
            main.startSizeY = unit.size.y;
            main.startRotation = new ParticleSystem.MinMaxCurve(-unit.rotationVariation, unit.rotationVariation);

            mpb.Clear();
            mpb.SetTexture("_MainTex", unit.texture.texture);
            renderer.SetPropertyBlock(mpb);
        }

        if(supportMotion) {
            lvolm.limitX = unit.limitVelocityAxesFactor.x;
            lvolm.limitY = unit.limitVelocityAxesFactor.y;
            lvolm.dampen = unit.limitVelocityDampen;

            main.startSpeed = new ParticleSystem.MinMaxCurve(unit.speedMin, unit.speedMax);
            main.gravityModifier = unit.gravity;
        }
    }

    private void Emit (bool isPlace = true) {
        CustomTileParticleUnit unit;
        if(isPlace) {
            unit = reference.placingUnits[unitID];
        } else {
            unit = reference.breakingUnits[unitID];
        }

        switch(unit.model) {
            case CustomParticleModelType.ApparitionDust:
            target.Emit(1);
            break;
            case CustomParticleModelType.BreakingExplosion:
            target.Emit(1);
            break;
            case CustomParticleModelType.BreakParticles:
            target.Emit(unit.particleCount);
            break;
            case CustomParticleModelType.SuctionRingParticles:
            target.Emit(unit.particleCount);
            break;
            case CustomParticleModelType.WaterSplash:
            target.Emit(1);
            break;
            case CustomParticleModelType.Collect:
            target.Emit(1);
            break;
        }
    }
}
