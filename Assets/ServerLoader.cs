using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class ServerLoader : MonoBehaviour {
    void Start () {
        if(NetworkingManager.Singleton.IsServer || NetworkingManager.Singleton.IsHost) {
            GetComponent<NetworkedObject>().Spawn(null, true);
        }
    }
}
