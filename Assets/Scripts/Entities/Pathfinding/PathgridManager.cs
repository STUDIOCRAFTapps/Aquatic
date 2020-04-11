using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathgridManager : MonoBehaviour {

    public Dictionary<long, ReadOnlyNodeChunk> nodeChunks;
    public Queue<ReadOnlyNodeChunk> unusedNodeChunks;

    public static PathgridManager inst;

    private void Awake () {
        inst = this;

        nodeChunks = new Dictionary<long, ReadOnlyNodeChunk>();
        unusedNodeChunks = new Queue<ReadOnlyNodeChunk>();
    }

    #region NodeChunk
    public ReadOnlyNodeChunk GetNodeChunkAt (Vector2Int position) {
        if(nodeChunks.TryGetValue(Hash.hVec2Int(position), out ReadOnlyNodeChunk chunk)) {
            return chunk;
        } else {
            ReadOnlyNodeChunk newNodeChunk = null;
            if(unusedNodeChunks.Count > 0) {
                newNodeChunk = unusedNodeChunks.Dequeue();
                newNodeChunk.chunkPosition = position;
            } else {
                newNodeChunk = new ReadOnlyNodeChunk(TerrainManager.inst.chunkSize);
            }
            nodeChunks.Add(Hash.hVec2Int(position), newNodeChunk);
            return newNodeChunk;
        }
    }

    public ReadOnlyNodeChunk GetNodeChunkIfExists (Vector2Int position) {
        if(nodeChunks.TryGetValue(Hash.hVec2Int(position), out ReadOnlyNodeChunk chunk)) {
            return chunk;
        } else {
            return null;
        }
    }

    public void SetNodeChunkAsUnused (Vector2Int position) {
        if(nodeChunks.TryGetValue(Hash.hVec2Int(position), out ReadOnlyNodeChunk chunk)) {
            unusedNodeChunks.Enqueue(chunk);
            nodeChunks.Remove(Hash.hVec2Int(position));
        } else {
            Debug.Log("NodeChunk at " + position + " should've been removed but it doesn't even exist");
        }
    }
    #endregion

    #region Utils
    public ReadOnlyNode GetReadOnlyNodeFromTilePoint (int x, int y) {
        ReadOnlyNodeChunk nc = GetNodeChunkIfExists(TerrainManager.GetChunkPositionAtTile(x, y));
        if(nc != null) {
            return nc.nodeGrid[mod(x, TerrainManager.inst.chunkSize)][mod(y, TerrainManager.inst.chunkSize)];
        } else {
            return null;
        }
    }

    /*public void GetNeighbours (ReadOnlyNode node, ref List<ReadOnlyNode> neighbours) {
        for(int x = -1; x <= 1; x++) {
            for(int y = -1; y <= 1; y++) {
                if(x == 0 && y == 0)
                    continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;
                
                neighbours.Add(GetReadOnlyNodeFromTilePoint(checkX, checkY));
            }
        }
    }*/

    int mod (int a, int b) {
        return (a % b + b) % b;
    }
    #endregion
}

public class ReadOnlyNodeChunk {
    public ReadOnlyNode[][] nodeGrid;

    public Vector2Int chunkPosition;
    int chunkSize;

    public ReadOnlyNodeChunk (int chunkSize) {
        this.chunkSize = chunkSize;

        nodeGrid = new ReadOnlyNode[chunkSize][];
        for(int x = 0; x < chunkSize; x++) {
            nodeGrid[x] = new ReadOnlyNode[chunkSize];

            for(int y = 0; y < chunkSize; y++) {
                nodeGrid[x][y] = new ReadOnlyNode();
            }
        }
    }
}
