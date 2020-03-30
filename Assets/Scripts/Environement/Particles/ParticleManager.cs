using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour {
    public static ParticleManager inst;
    public CustomTileParticle[] customTileParticles;
    public CustomTileParticle defaultTileParticles;

    public TileParticleConfigurator[] prefabs;

    private Queue<TileParticleConfigurator>[] unusedTileParticles;
    private Dictionary<int, CustomTileParticle> gidToCustomTileParticle;

    private void Awake () {
        inst = this;

        unusedTileParticles = new Queue<TileParticleConfigurator>[prefabs.Length];
        for(int i = 0; i < prefabs.Length; i++) {
            unusedTileParticles[i] = new Queue<TileParticleConfigurator>();
        }

        gidToCustomTileParticle = new Dictionary<int, CustomTileParticle>();
        foreach(CustomTileParticle ctp in customTileParticles) {
            foreach(BaseTileAsset bta in ctp.tileAssets) {
                gidToCustomTileParticle.Add(bta.globalID, ctp);
            }
        }
    }


    #region Public Functions
    public void PlayTilePlace (Vector2Int pos, BaseTileAsset tileAsset) {
        if(gidToCustomTileParticle.ContainsKey(tileAsset.globalID)) {
            CustomTileParticle ctp = gidToCustomTileParticle[tileAsset.globalID];

            for(int i = 0; i < ctp.placingUnits.Length; i++) {
                GetUnusedTileParticle((int)ctp.placingUnits[i].model).PlayPlace(ctp, i, pos);
            }
        } else {
            CustomTileParticle ctp = defaultTileParticles;

            for(int i = 0; i < ctp.placingUnits.Length; i++) {
                GetUnusedTileParticle((int)ctp.placingUnits[i].model).PlayPlace(ctp, i, pos);
            }
        }
    }

    public void PlayTileBreak (Vector2Int pos, BaseTileAsset tileAsset) {
        if(gidToCustomTileParticle.ContainsKey(tileAsset.globalID)) {
            CustomTileParticle ctp = gidToCustomTileParticle[tileAsset.globalID];

            for(int i = 0; i < ctp.placingUnits.Length; i++) {
                GetUnusedTileParticle((int)ctp.placingUnits[i].model).PlayBreak(ctp, i, pos);
            }
        } else {
            CustomTileParticle ctp = defaultTileParticles;

            for(int i = 0; i < ctp.placingUnits.Length; i++) {
                GetUnusedTileParticle((int)ctp.placingUnits[i].model).PlayBreak(ctp, i, pos);
            }
        }
    }
    #endregion

    #region TileParticlePool
    private TileParticleConfigurator GetUnusedTileParticle (int modelType) {
        if(unusedTileParticles[modelType].Count > 0) {
            TileParticleConfigurator tileParticle = unusedTileParticles[modelType].Dequeue();
            tileParticle.gameObject.SetActive(true);
            return tileParticle;
        } else {
            TileParticleConfigurator tileParticle = Instantiate(prefabs[modelType], transform);
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
