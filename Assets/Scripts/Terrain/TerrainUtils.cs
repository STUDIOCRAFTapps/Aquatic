using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum TerrainLayers {
    Ground,
    WaterSurface,
    Background,
    WaterBackground
}

public struct MeshData {
    public List<Vector3> verts;
    public List<int> tris;
    public List<Vector3> uvs;
    public List<Vector2> animUVs;
    public int faceOffset;

    public void Initiate () {
        verts = new List<Vector3>(1024);
        tris = new List<int>(2048);
        uvs = new List<Vector3>(1024);
        animUVs = new List<Vector2>(1024);
        faceOffset = 0;
    }

    public void Clear () {
        verts.Clear();
        tris.Clear();
        uvs.Clear();
        animUVs = new List<Vector2>();
        faceOffset = 0;
    }

    public void Apply (Mesh mesh, int subMesh = 0) {
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, subMesh);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, animUVs);
    }
}

public class TileString {
    public string nspace;
    public string id;

    public TileString () { }

    public TileString (string nspace, string id) {
        this.nspace = nspace;
        this.id = id;
    }
}

[System.Serializable]
public struct ChunkLoadBounds {
    public Vector2Int minimum {
        set; get;
    }
    public Vector2Int maximum {
        set; get;
    }

    public ChunkLoadBounds (Vector2Int minimum, Vector2Int maximum) {
        this.minimum = minimum;
        this.maximum = maximum;
    }

    public override string ToString ()
        => $"{minimum}, {maximum}";

    public bool IsAreaNull ()
        => ((maximum.x - minimum.x) * (maximum.y - minimum.y)) <= 0f;

    public static bool operator == (ChunkLoadBounds a, ChunkLoadBounds b)
        => a.minimum == b.minimum && a.maximum == b.maximum;

    public static bool operator != (ChunkLoadBounds a, ChunkLoadBounds b)
        => !(a == b);

    public override bool Equals (object obj) {
        if(!(obj is ChunkLoadBounds)) {
            return false;
        }

        var bounds = (ChunkLoadBounds)obj;
        return minimum.Equals(bounds.minimum) &&
               maximum.Equals(bounds.maximum);
    }

    public override int GetHashCode () {
        var hashCode = -1421831408;
        hashCode = hashCode * -1521134295 + base.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<Vector2Int>.Default.GetHashCode(minimum);
        hashCode = hashCode * -1521134295 + EqualityComparer<Vector2Int>.Default.GetHashCode(maximum);
        return hashCode;
    }
}

public class ChunkLoadCounter {
    public int loadCount;
    public CancellableTimer timer;
}

public class CancellableTimer {
    public bool isRunning {
        private set; get;
    }

    bool cancelled = false;
    Action callback;
    float seconds;

    public void Start (float seconds, Action callback) {
        this.seconds = seconds;
        this.callback = callback;
        isRunning = true;
        TerrainManager.inst.StartCoroutine(WaitForEndOfTimer());
    }

    public void Cancel () {
        cancelled = true;
    }

    IEnumerator WaitForEndOfTimer () {
        yield return new WaitForSeconds(seconds);
        if(!cancelled) {
            callback?.Invoke();
        }
        isRunning = false;
    }
}
