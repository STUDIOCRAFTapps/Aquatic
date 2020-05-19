using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum TerrainLayers {
    Ground,
    WaterSurface,
    Background,
    WaterBackground,
    Overlay
}

public struct MeshData {
    public List<Vector3> verts;
    public List<int> tris;
    public List<Vector3> uvs;
    public List<Vector2> animUVs;
    public int faceOffset;

    public void Initiate () {
        verts = new List<Vector3>(1024);
        tris = new List<int>(6144);
        uvs = new List<Vector3>(1024);
        animUVs = new List<Vector2>(1024);
        faceOffset = 0;
    }

    public void Clear () {
        verts.Clear();
        tris.Clear();
        uvs.Clear();
        animUVs.Clear();
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
    public Vector2Int position;
    public int loadCount;
    public CancellableTimer timer;

    public ChunkLoadCounter (Vector2Int position) {
        this.position = position;
    }
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

public struct Bounds2D {
    public Vector2 min;
    public Vector2 max;

    public Vector2 center {
        get {
            return new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
        }
    }

    public Vector2 size {
        get {
            return new Vector2(max.x - min.x, max.y - min.y);
        }
    }

    public Bounds2D (Vector2 min, Vector2 max) {
        this.min = min;
        this.max = max;
    }

    public Bounds2D CreateBoundsFromPoints (Vector2 p1, Vector2 p2) {
        return new Bounds2D(
            new Vector2(
                Mathf.Min(p1.x, p2.x),
                Mathf.Min(p1.y, p2.y)),
            new Vector2(
                Mathf.Max(p1.x, p2.x),
                Mathf.Max(p1.y, p2.y))
        );
    }

    public bool Overlaps (Bounds2D b) {
        return (this.min.x < b.max.x && this.max.x > b.min.x &&
                this.min.y > b.max.y && this.max.y < b.max.y);
    }

    public bool Overlaps (Vector2 p) {
        return (p.x > min.x && p.x < max.x && p.y > min.y && p.y < max.y);
    }

    public bool IntersectRay (Vector2 origin, Vector2 invDir, out float distance) {
        bool signDirX = invDir.x < 0;
        bool signDirY = invDir.y < 0;

        Vector2 bbox = signDirX ? max : min;
        float tmin = (bbox.x - origin.x) * invDir.x;
        bbox = signDirX ? min : max;
        float tmax = (bbox.x - origin.x) * invDir.x;
        bbox = signDirY ? max : min;
        float tymin = (bbox.y - origin.y) * invDir.y;
        bbox = signDirY ? min : max;
        float tymax = (bbox.y - origin.y) * invDir.y;

        if((tmin > tymax) || (tymin > tmax)) {
            distance = 0f;
            return false;
        }
        if(tymin > tmin) {
            tmin = tymin;
        }
        if(tymax < tmax) {
            tmax = tymax;
        }

        distance = tmin;
        return true;
    }

    public void ExtendByDelta (Vector2 delta) {
        if(delta.x < 0f) {
            min.x += delta.x;
        } else {
            max.x += delta.x;
        }
        if(delta.y < 0f) {
            min.y += delta.y;
        } else {
            max.y += delta.y;
        }
    }

    public void ExtendByDelta (float deltaX, float deltaY) {
        if(deltaX < 0f) {
            min.x += deltaX;
        } else {
            max.x += deltaX;
        }
        if(deltaY < 0f) {
            min.y += deltaY;
        } else {
            max.y += deltaY;
        }
    }

    public void PositionToTile (int tileX, int tileY) {
        min.x += tileX;
        min.y += tileY;
        max.x += tileX;
        max.y += tileY;
    }

    public void Move (Vector2 delta) {
        min.x += delta.x;
        min.y += delta.y;
        max.x += delta.x;
        max.y += delta.y;
    }

    public Vector2 GetIntersectingArea (Bounds2D b) {
        return new Vector2(
            Mathf.Max(Mathf.Min(max.x, b.max.x) - Mathf.Max(min.x, b.min.x), 0f),
            Mathf.Max(Mathf.Min(max.y, b.max.y) - Mathf.Max(min.y, b.min.y), 0f)
        );
    }
}
