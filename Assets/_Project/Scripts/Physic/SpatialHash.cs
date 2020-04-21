using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Body {
    public Bounds aabb;
    public List<int> gridIndex;
}

public class SpatialHash {
    /* the square cell gridLength of the grid. Must be larger than the largest shape in the space. */
    private float gridHeightRes;
    private float gridWidthRes;
    private float invCellSize;

    public int cellSize {
        get; set;
    }

    /* the world space width */
    public int gridWidth {
        get; set;
    }

    /* the world space height */
    public int gridHeight {
        get; set;
    }

    /* the number of buckets (i.e. cells) in the spatial grid */
    public int gridLength {
        get; set;
    }
    /* the array-list holding the spatial grid buckets */
    public List<List<Body>> grid {
        get; set;
    }

    public SpatialHash (int _width, int _height, int _cellSize) {
        gridWidthRes = _width;
        gridHeightRes = _height;

        cellSize = _cellSize;
        invCellSize = 1f / cellSize;

        gridWidth = Mathf.CeilToInt(_width * invCellSize);
        gridHeight = Mathf.CeilToInt(_height * invCellSize);

        gridLength = gridWidth * gridHeight;

        grid = new List<List<Body>>(gridLength);

        for(int i = 0; i < gridLength; i++)
            grid.Add(new List<Body>());

    }

    public void AddBody (Body b) {
        AddIndex(b, getIndex1DVec(ClampToGridVec(b.aabb.center.x, b.aabb.center.y)));
    }

    public void RemoveBody (Body b) {
        RemoveIndexes(b);
    }

    public void UpdateBody (Body b) {
        updateIndexes(b, AABBToGrid(b.aabb.min, b.aabb.max));
    }

    public List<Body> getAllBodiesSharingCellsWithBody (Body body) {
        var collidingBodies = new List<Body>();
        foreach(int i in body.gridIndex) {
            if(grid[i].Count == 0)
                continue;

            foreach(var cbd in grid[i].ToArray()) {
                if(cbd == body)
                    continue;
                collidingBodies.Add(cbd);
            }
        }
        return collidingBodies;
    }

    public bool isBodySharingAnyCell (Body body) {
        foreach(int i in body.gridIndex) {
            if(grid[i].Count == 0)
                continue;

            foreach(var cbd in grid[i].ToArray()) {
                if(cbd == body)
                    continue;
                return true;
            }
        }
        return false;
    }

    public int getIndex1DVec (Vector2 pos) {
        return (int)(Math.Floor(pos.x * invCellSize) + gridWidth * Math.Floor(pos.y * invCellSize));
    }

    private int getIndex (float pos) {
        return (int)(pos * invCellSize);
    }

    private int getIndex1D (int x, int y) {
        // i = x + w * y;  x = i % w; y = i / w;
        return (x + gridWidth * y);
    }

    private void updateIndexes (Body b, List<int> ar) {
        foreach(int i in b.gridIndex) {
            RemoveIndex(b, i);
        }
        //b.gridIndex.splice( 0, b.gridIndex.length );
        b.gridIndex.Clear();

        foreach(int i in ar) {
            AddIndex(b, i);
        }
    }

    private void AddIndex (Body b, int cellPos) {
        grid[cellPos].Add(b);
        b.gridIndex.Add(cellPos);
    }
    private void RemoveIndexes (Body b) // changed from CellObject
    {
        foreach(int i in b.gridIndex) {
            RemoveIndex(b, i);
        }
        //b.gridIndex.splice( 0, b.gridIndex.length );
        b.gridIndex.Clear();
    }
    private void RemoveIndex (Body b, int pos) // changed from CellObject
    {
        grid[pos].Remove(b);
    }

    private bool isValidGridPos (int num) {
        if(num < 0 || num >= gridLength)
            return false;
        else
            return true;
    }

    public Vector2 ClampToGridVec (float x, float y) {
        Vector2 vec = new Vector2(x, y);
        vec.x = Mathf.Clamp(vec.x, 0, gridWidthRes - 1);
        vec.y = Mathf.Clamp(vec.y, 0, gridHeightRes - 1);
        return vec;
    }

    private List<int> AABBToGrid (Vector2 min, Vector2 max) {
        var arr = new List<int>();
        int aabbMinX = Mathf.Clamp(getIndex(min.x), 0, gridWidth - 1);
        int aabbMinY = Mathf.Clamp(getIndex(min.y), 0, gridHeight - 1);
        int aabbMaxX = Mathf.Clamp(getIndex(max.x), 0, gridWidth - 1);
        int aabbMaxY = Mathf.Clamp(getIndex(max.y), 0, gridHeight - 1);

        int aabbMin = getIndex1D(aabbMinX, aabbMinY);
        int aabbMax = getIndex1D(aabbMaxX, aabbMaxY);

        arr.Add(aabbMin);
        if(aabbMin != aabbMax) {
            arr.Add(aabbMax);
            int lenX = aabbMaxX - aabbMinX + 1;
            int lenY = aabbMaxY - aabbMinY + 1;
            for(int x = 0; x < lenX; x++) {
                for(int y = 0; y < lenY; y++) {
                    if((x == 0 && y == 0) || (x == lenX - 1 && y == lenY - 1))
                        continue;
                    arr.Add(getIndex1D(x, y) + aabbMin);
                }
            }
        }
        return arr;
    }

    /* DDA line algorithm. @author playchilla.com */
    public List<int> LineToGrid (float x1, float y1, float x2, float y2) {
        var arr = new List<int>();

        int gridPosX = getIndex(x1);
        int gridPosY = getIndex(y1);

        if(!isValidGridPos(gridPosX) || !isValidGridPos(gridPosY))
            return arr;

        arr.Add(getIndex1D(gridPosX, gridPosY));

        float dirX = x2 - x1;
        float dirY = y2 - y1;
        float distSqr = dirX * dirX + dirY * dirY;
        if(distSqr < 0.00000001f) // todo: use const epsilon
            return arr;

        float nf = (1f / Mathf.Sqrt(distSqr));
        dirX *= nf;
        dirY *= nf;

        float deltaX = cellSize / Math.Abs(dirX);
        float deltaY = cellSize / Math.Abs(dirY);

        float maxX = gridPosX * cellSize - x1;
        float maxY = gridPosY * cellSize - y1;
        if(dirX >= 0)
            maxX += cellSize;
        if(dirY >= 0)
            maxY += cellSize;
        maxX /= dirX;
        maxY /= dirY;

        int stepX = Math.Sign(dirX);
        int stepY = Math.Sign(dirY);
        int gridGoalX = getIndex(x2);
        int gridGoalY = getIndex(y2);
        int currentDirX = gridGoalX - gridPosX;
        int currentDirY = gridGoalY - gridPosY;

        while(currentDirX * stepX > 0 || currentDirY * stepY > 0) {
            if(maxX < maxY) {
                maxX += deltaX;
                gridPosX += stepX;
                currentDirX = gridGoalX - gridPosX;
            } else {
                maxY += deltaY;
                gridPosY += stepY;
                currentDirY = gridGoalY - gridPosY;
            }

            if(!isValidGridPos(gridPosX) || !isValidGridPos(gridPosY))
                break;

            arr.Add(getIndex1D(gridPosX, gridPosY));
        }
        return arr;
    }

    public void Clear () {
        foreach(var cell in grid) {
            if(cell.Count > 0) {
                foreach(var co in cell) {
                    co.gridIndex.Clear();
                }
                cell.Clear();
            }
        }
    }
}