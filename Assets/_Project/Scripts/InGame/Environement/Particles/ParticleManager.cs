using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour {
    public static ParticleManager inst;
    public CustomTileParticle[] customTileParticles;
    public CustomTileParticle defaultTileParticles;

    public TileParticleConfigurator[] tileParticlePrefabs;
    public ParticleConfigurator fixedParticlePrefab;
    public EntityParticleAsset[] fixedParticleAssets;
    public ParticleConfigurator[] adaptiveParticlePrefabs;

    private List<ParticleConfigurator> adaptiveParticles;
    private List<ParticleConfigurator> fixedParticles;
    private Queue<TileParticleConfigurator>[] unusedTileParticles;
    private Dictionary<int, CustomTileParticle> gidToCustomTileParticle;

    private void Awake () {
        inst = this;

        unusedTileParticles = new Queue<TileParticleConfigurator>[tileParticlePrefabs.Length];
        for(int i = 0; i < tileParticlePrefabs.Length; i++) {
            unusedTileParticles[i] = new Queue<TileParticleConfigurator>();
        }

        adaptiveParticles = new List<ParticleConfigurator>();
        for(int i = 0; i < adaptiveParticlePrefabs.Length; i++) {
            adaptiveParticles.Add(Instantiate(adaptiveParticlePrefabs[i], transform));
        }

        fixedParticles = new List<ParticleConfigurator>();
        for(int i = 0; i < fixedParticleAssets.Length; i++) {
            fixedParticles.Add(Instantiate(fixedParticlePrefab, transform));
            fixedParticles[i].Configure(fixedParticleAssets[i]);
        }

        gidToCustomTileParticle = new Dictionary<int, CustomTileParticle>();
        foreach(CustomTileParticle ctp in customTileParticles) {
            foreach(BaseTileAsset bta in ctp.tileAssets) {
                gidToCustomTileParticle.Add(bta.globalID, ctp);
            }
        }
    }


    #region Public Functions
    public void PlayTilePlace (Vector2Int pos, BaseTileAsset tileAsset, MobileDataChunk mdc = null) {
        if(gidToCustomTileParticle.ContainsKey(tileAsset.globalID)) {
            CustomTileParticle ctp = gidToCustomTileParticle[tileAsset.globalID];

            for(int i = 0; i < ctp.placingUnits.Length; i++) {
                GetUnusedTileParticle((int)ctp.placingUnits[i].model).PlayPlace(ctp, i, pos, mdc);
            }
        } else {
            CustomTileParticle ctp = defaultTileParticles;

            for(int i = 0; i < ctp.placingUnits.Length; i++) {
                GetUnusedTileParticle((int)ctp.placingUnits[i].model).PlayPlace(ctp, i, pos, mdc);
            }
        }
    }

    public void PlayTileBreak (Vector2Int pos, BaseTileAsset tileAsset, MobileDataChunk mdc = null) {
        if(gidToCustomTileParticle.ContainsKey(tileAsset.globalID)) {
            CustomTileParticle ctp = gidToCustomTileParticle[tileAsset.globalID];

            for(int i = 0; i < ctp.breakingUnits.Length; i++) {
                GetUnusedTileParticle((int)ctp.breakingUnits[i].model).PlayBreak(ctp, i, pos, mdc);
            }
        } else {
            CustomTileParticle ctp = defaultTileParticles;

            for(int i = 0; i < ctp.breakingUnits.Length; i++) {
                GetUnusedTileParticle((int)ctp.breakingUnits[i].model).PlayBreak(ctp, i, pos, mdc);
            }
        }
    }

    public void PlayFixedParticle (Vector3 position, int id) {
        fixedParticles[id].Play(position);
    }

    public void PlayAdaptiveParticle (Vector3 position, int id) {
        adaptiveParticles[id].Play(position);
    }

    #endregion

    #region TileParticlePool
    private TileParticleConfigurator GetUnusedTileParticle (int modelType) {
        if(unusedTileParticles[modelType].Count > 0) {
            TileParticleConfigurator tileParticle = unusedTileParticles[modelType].Dequeue();
            tileParticle.gameObject.SetActive(true);
            return tileParticle;
        } else {
            TileParticleConfigurator tileParticle = Instantiate(tileParticlePrefabs[modelType], transform);
            tileParticle.modelType = modelType;
            return tileParticle;
        }
    }

    public void SetTileParticleAsUnused (int modelType, TileParticleConfigurator tileParticle) {
        tileParticle.gameObject.SetActive(false);
        unusedTileParticles[modelType].Enqueue(tileParticle);
    }
    #endregion
}
