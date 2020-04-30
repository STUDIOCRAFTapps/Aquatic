using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Hash  {
    public static long hVec2Int (Vector2Int p) {
        return ((long)((uint)p.x) << 32) | ((uint)p.y);
    }

    public static long hVec2Int (int x, int y) {
        return ((long)((uint)x) << 32) | ((uint)y);
    }
}
