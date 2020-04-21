#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CustomTileParticle))]
public class CustomTileParticleEditor : Editor {

    public override void OnInspectorGUI () {
        CustomTileParticle ctp = (CustomTileParticle)target;

        DrawDefaultInspector();

        if(GUILayout.Button("Set Simple Default")) {
            ctp.breakingUnits = new CustomTileParticleUnit[] {
                new CustomTileParticleUnit() {
                    model = CustomParticleModelType.BreakingExplosion
                }
            };
            ctp.placingUnits = new CustomTileParticleUnit[] {
                new CustomTileParticleUnit() {
                    model = CustomParticleModelType.ApparitionDust
                }
            };
        }
        if(GUILayout.Button("Set Particle Default")) {
            ctp.breakingUnits = new CustomTileParticleUnit[] {
                new CustomTileParticleUnit() {
                    model = CustomParticleModelType.BreakingExplosion
                },
                new CustomTileParticleUnit() {
                    model = CustomParticleModelType.BreakParticles
                }
            };
            ctp.placingUnits = new CustomTileParticleUnit[] {
                new CustomTileParticleUnit() {
                    model = CustomParticleModelType.ApparitionDust
                },
                new CustomTileParticleUnit() {
                    model = CustomParticleModelType.SuctionRingParticles
                }
            };
        }
    }
}
#endif