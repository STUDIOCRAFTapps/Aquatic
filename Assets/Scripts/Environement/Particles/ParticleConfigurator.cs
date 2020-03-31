using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleConfigurator : MonoBehaviour {

    public bool isAdaptive = true;
    [HideInInspector] public int id;
    public ParticleSystem target;

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
        // Call 9Particle Manager to tell him it's all done now
        ParticleManager.inst.SetStaticEntityParticleAsUnused(id, this);
    }
}
