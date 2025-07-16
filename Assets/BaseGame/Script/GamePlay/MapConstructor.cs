using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapConstructor : MonoBehaviour
{
    public Camera camera; // Base Size = 6.5f for 6 grid
    public Transform groundTrs; // Base Scale = 1 for 6 grid
    public FloorTile floorTilePrefab;
    public Transform tileParent;
    public List<FloorTile> floorTiles;
    public List<BaseGate> gates;
    public List<Coordinate> tileCoordinates;
    
    public int xSize;
    public int ySize;
    public float xDelta = 0;
    public float yDelta = 0;

    public Transform blocksParent;
    public List<BaseBlock> blocks;
    public List<EliminateLine> eliminateLines;
    
    public Coordinate lastCheckCoordinate;

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        CollectBlock();
    }
    
    #region InitMap

    public void SetCoordinates()
    {
        tileCoordinates = new List<Coordinate>();
        floorTiles = new List<FloorTile>();
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                tileCoordinates.Add(new Coordinate(x, y));
            }
        }
    }

    [Button]
    public void GenerateBaseSize()
    {
        SetCoordinates();
        GenerateMap();
    }

    [Button]
    public void GenerateMap()
    {
        ClearTiles();
        var size = GetMapSize();
        xDelta = (float)-size.xIndex / 2;
        yDelta = (float)-size.yIndex / 2;
        floorTiles = new List<FloorTile>();
        for (var i = 0; i < tileCoordinates.Count; i++)
        {
            var coordinate = tileCoordinates[i];
            Vector3 position = new Vector3(coordinate.xIndex + xDelta, 0, coordinate.yIndex + yDelta);
#if UNITY_EDITOR
            FloorTile tile = (FloorTile)PrefabUtility.InstantiatePrefab(floorTilePrefab);
#else
            FloorTile tile = Instantiate(floorTilePrefab);
#endif
            tile.transform.position = position;
            tile.transform.SetParent(tileParent);
            floorTiles.Add(tile);
            tile.mapConstructor = this;
            SetTileIndex(tile, coordinate.xIndex, coordinate.yIndex);
            tile.SetTouchDirections(GetTouchDirections(new Coordinate(coordinate.xIndex, coordinate.yIndex)));
        }
        SetScale();
    }

    void SetScale()
    {
        var size = GetMapSize();
        float maxSize = (size.xIndex + size.yIndex) / 2f + 1;
        if(camera != null)
        {
            camera.orthographicSize = 6.5f * (maxSize / 6f);
        }
        if (groundTrs != null)
        {
            groundTrs.localScale = new Vector3(maxSize / 6f, 1, maxSize / 6f);
        }
    }

    [Button]
    public void CollectTile()
    {
        tileCoordinates.Clear();
        for (var i = 0; i < floorTiles.Count; i++)
        {
            var tile = floorTiles[i];
            if(tile != null)
                tileCoordinates.Add(new Coordinate(tile.coordinate.xIndex, tile.coordinate.yIndex));
        }
    }

    void SetTileIndex(FloorTile tile, int x, int y)
    {
        tile.SetTileIndex(x, y);
    }

    Coordinate GetMapSize()
    {
        Coordinate size = new Coordinate(0, 0);
        for (var i = 0; i < tileCoordinates.Count; i++)
        {
            if(tileCoordinates[i].xIndex > size.xIndex)
            {
                size.xIndex = tileCoordinates[i].xIndex;
            }
            if(tileCoordinates[i].yIndex > size.yIndex)
            {
                size.yIndex = tileCoordinates[i].yIndex;
            }
        }
        return size;
    }

    public void SetFloorTiled()
    {
        Coordinate mapSize = GetMapSize();
        float tillX = 1f / (mapSize.xIndex + 1);
        float tillY = 1f / (mapSize.yIndex + 1);
        
        for (var i = 0; i < floorTiles.Count; i++)
        {
            floorTiles[i].SetTiledMaterial(tillX, tillY, tillX * floorTiles[i].coordinate.xIndex, tillY * floorTiles[i].coordinate.yIndex);
        }
    }
    
    public void RegisterGate(BaseGate gate)
    {
        if(!gates.Contains(gate))
            gates.Add(gate);
    }

    public void RemoveGate(BaseGate gate)
    {
        gates.Remove(gate);
    }
    
    [Button]
    public void ClearTiles()
    {
        if(floorTiles == null)
        {
            return;
        }
        for (var i = 0; i < floorTiles.Count; i++)
        {
            if(floorTiles[i] != null)
                DestroyImmediate(floorTiles[i].gameObject);
        }
        floorTiles.Clear();
        gates.Clear();
    }
    
    public List<TouchDirection> GetTouchDirections(Coordinate coordinate)
    {
        List<TouchDirection> directions = new List<TouchDirection>();
        for (var i = 0; i < tileCoordinates.Count; i++)
        {
            var tile = tileCoordinates[i];
            if(tile.xIndex == coordinate.xIndex && tile.yIndex == coordinate.yIndex)
            {
                continue;
            }
            if (tile.xIndex == coordinate.xIndex && tile.yIndex == coordinate.yIndex + 1)
            {
                directions.Add(TouchDirection.Up);
            }
            else if (tile.xIndex == coordinate.xIndex && tile.yIndex == coordinate.yIndex - 1)
            {
                directions.Add(TouchDirection.Down);
            }
            else if (tile.xIndex == coordinate.xIndex + 1 && tile.yIndex == coordinate.yIndex)
            {
                directions.Add(TouchDirection.Right);
            }
            else if (tile.xIndex == coordinate.xIndex - 1 && tile.yIndex == coordinate.yIndex)
            {
                directions.Add(TouchDirection.Left);
            }
        }
        return directions;
    }

    bool HasTiles(Coordinate coordinate)
    {
        for (var i = 0; i < tileCoordinates.Count; i++)
        {
            if(tileCoordinates[i].xIndex == coordinate.xIndex && tileCoordinates[i].yIndex == coordinate.yIndex)
            {
                return true;
            }
        }
        return false;
    }
    
    bool HasBlockOnTile(Coordinate coordinate, ColorCode colorCode)
    {
        for (var i = 0; i < blocks.Count; i++)
        {
            if(blocks[i].HasBlockOnTile(coordinate, colorCode))
            {
                return true;
            }
        }
        return false;
    }
    public void AddBlock(BaseBlock block)
    {
        if (blocks == null)
        {
            blocks = new List<BaseBlock>();
        }
        if (!blocks.Contains(block))
        {
            blocks.Add(block);
        }
    }
    
    public void RemoveBlock(BaseBlock block)
    {
        if (blocks.Contains(block))
        {
            blocks.Remove(block);
        }
    }

    public void CollectBlock()
    {
        // Get all children of blocksParent check if it is BaseBlock and add to blocks list if not already present
        blocks.Clear();
        for (var i = 0; i < blocksParent.childCount; i++)
        {
            var child = blocksParent.GetChild(i);
            BaseBlock block = child.GetComponent<BaseBlock>();
            if (block != null && !blocks.Contains(block))
            {
                blocks.Add(block);
            }
        }
    }
    #endregion

    #region GamePlay

    public bool CheckLastCheckCoordinate(Vector3 position, out Coordinate coordinate)
    {
        Coordinate checkCoordinate = new Coordinate(0, 0);
        float minDistance = float.MaxValue;
        for (var i = 0; i < tileCoordinates.Count; i++)
        {
            var tile = tileCoordinates[i];
            Vector3 tilePosition = new Vector3(tile.xIndex + xDelta, 0, tile.yIndex + yDelta);
            float distance = Vector3.Distance(position, tilePosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                checkCoordinate = tile;
            }
        }
        if (lastCheckCoordinate != null )
        {
            if ((lastCheckCoordinate.xIndex != checkCoordinate.xIndex || lastCheckCoordinate.yIndex != checkCoordinate.yIndex))
            {
                lastCheckCoordinate.xIndex = checkCoordinate.xIndex;
                lastCheckCoordinate.yIndex = checkCoordinate.yIndex;
                coordinate = new Coordinate(checkCoordinate);
                return true;
            }
        }
        else
        {
            lastCheckCoordinate = new Coordinate(checkCoordinate);
            lastCheckCoordinate.xIndex = checkCoordinate.xIndex;
            lastCheckCoordinate.yIndex = checkCoordinate.yIndex;
            coordinate = new Coordinate(checkCoordinate);
            return true;
        }
        coordinate = new Coordinate(checkCoordinate);
        return false;
    }
    
    public Vector3 GetNearestCordinate(Vector3 position, out Coordinate coordinate)
    {
        coordinate = new Coordinate(0, 0);
        float minDistance = float.MaxValue;
        for (var i = 0; i < tileCoordinates.Count; i++)
        {
            var tile = tileCoordinates[i];
            Vector3 tilePosition = new Vector3(tile.xIndex + xDelta, 0, tile.yIndex + yDelta);
            float distance = Vector3.Distance(position, tilePosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                coordinate = tile;
            }
        }
        return new Vector3(coordinate.xIndex + xDelta, 0, coordinate.yIndex + yDelta);;
    }
    
    public void SetSelectedBlock(BaseBlock block)
    {
        
    }

    public void DeSelectBlock()
    {
        CheckBlockEliminate();
        EliminateBlockCoordinate();
        EliminateBlock();
    }

    void CheckBlockEliminate()
    {
        if(eliminateLines == null)
            eliminateLines = new List<EliminateLine>();
        eliminateLines.Clear();
        List<BaseGate> checkedGates = new List<BaseGate>();
        for (int i = 0; i < gates.Count; i++)
        {
            BaseGate gate = gates[i];
            BaseGate matchedGate = null;
            checkedGates.Add(gate);
            for (int j = 0; j < gates.Count; j++)
            {
                // if(gate == gates[j])
                //     continue;
                if(checkedGates.Contains(gates[j]))
                    continue;
                if (CheckMatchingGate(gate, gates[j]))
                {
                    matchedGate = gates[j];
                    List<Coordinate> toEliminate = CheckLine(gate.floorTileWall.floorTile.coordinate, matchedGate.floorTileWall.floorTile.coordinate, gate.colorCode);
                    if (toEliminate != null && toEliminate.Count > 0)
                    {
                        EliminateLine toEliminateLine = new EliminateLine(toEliminate);
                        toEliminateLine.toEliminateGates.Add(gate);
                        toEliminateLine.toEliminateGates.Add(matchedGate);
                        eliminateLines.Add(toEliminateLine);
                    }
                    break;
                }
            }
        }
    }

    bool CheckMatchingGate(BaseGate gate1, BaseGate gate2)
    {
        if (gate1.colorCode != gate2.colorCode)
            return false;
        if (gate1.floorTileWall.floorTile.coordinate.xIndex != gate2.floorTileWall.floorTile.coordinate.xIndex
            && gate1.floorTileWall.floorTile.coordinate.yIndex != gate2.floorTileWall.floorTile.coordinate.yIndex)
            return false;
        if (gate1.floorTileWall.floorTile.coordinate.yIndex == gate2.floorTileWall.floorTile.coordinate.yIndex)
        {
            if ((gate1.floorTileWall.touchDirection == TouchDirection.Left && gate2.floorTileWall.touchDirection == TouchDirection.Right)
                || (gate1.floorTileWall.touchDirection == TouchDirection.Right && gate2.floorTileWall.touchDirection == TouchDirection.Left))
                return CheckInLine(gate1.floorTileWall.floorTile.coordinate, gate2.floorTileWall.floorTile.coordinate);
        }
        else if (gate1.floorTileWall.floorTile.coordinate.xIndex == gate2.floorTileWall.floorTile.coordinate.xIndex)
        {
            if ((gate1.floorTileWall.touchDirection == TouchDirection.Up && gate2.floorTileWall.touchDirection == TouchDirection.Down)
                || (gate1.floorTileWall.touchDirection == TouchDirection.Down && gate2.floorTileWall.touchDirection == TouchDirection.Up))
                return CheckInLine(gate1.floorTileWall.floorTile.coordinate, gate2.floorTileWall.floorTile.coordinate);
        }
        return false;
    }
    
    bool CheckInLine(Coordinate pos1, Coordinate pos2)
    {
        bool isHorizontal = pos1.yIndex == pos2.yIndex;
        if (isHorizontal)
        {
            int mixX = Mathf.Min(pos1.xIndex, pos2.xIndex);
            int maxX = Mathf.Max(pos1.xIndex, pos2.xIndex);
            int yIndex = pos1.yIndex; // Assuming both have the same Y index for horizontal check
            for (int i = mixX; i < maxX + 1; i++)
            {
                if(!HasTiles(new Coordinate(i, yIndex)))
                    return false;
            }
            return true;
        }
        else
        {
            int mixY = Mathf.Min(pos1.yIndex, pos2.yIndex);
            int maxY = Mathf.Max(pos1.yIndex, pos2.yIndex);
            int xIndex = pos1.xIndex; // Assuming both have the same X index for vertical check
            for (int i = mixY; i < maxY + 1; i++)
            {
                if(!HasTiles(new Coordinate(xIndex, i)))
                    return false;
            }
            return true;
        }
    }

    List<Coordinate> CheckLine(Coordinate pos1, Coordinate pos2, ColorCode colorCode)
    {
        List<Coordinate> toEliminateCoordinates = new List<Coordinate>();
        bool isHorizontal = pos1.yIndex == pos2.yIndex;
        if (isHorizontal)
        {
            int mixX = Mathf.Min(pos1.xIndex, pos2.xIndex);
            int maxX = Mathf.Max(pos1.xIndex, pos2.xIndex);
            int yIndex = pos1.yIndex; // Assuming both have the same Y index for horizontal check
            for (int i = mixX; i < maxX + 1; i++)
            {
                if(HasBlockOnTile(new Coordinate(i, yIndex), colorCode))
                {
                    toEliminateCoordinates.Add(new Coordinate(i, yIndex));
                }
                else
                {
                    toEliminateCoordinates.Clear();
                    break;
                }
            }
        }
        else
        {
            int mixY = Mathf.Min(pos1.yIndex, pos2.yIndex);
            int maxY = Mathf.Max(pos1.yIndex, pos2.yIndex);
            int xIndex = pos1.xIndex; // Assuming both have the same X index for vertical check
            for (int i = mixY; i < maxY + 1; i++)
            {
                if(HasBlockOnTile(new Coordinate(xIndex, i), colorCode))
                {
                    toEliminateCoordinates.Add(new Coordinate(xIndex, i));
                }
                else
                {
                    toEliminateCoordinates.Clear();
                    break;
                }
            }
        }
        return toEliminateCoordinates;
    }

    void EliminateBlockCoordinate()
    {
        for (var i = 0; i < eliminateLines.Count; i++)
        {
            for (var i1 = 0; i1 < eliminateLines[i].toEliminateCoordinates.Count; i1++)
            {
                EliminateBlockCoordinate(eliminateLines[i].toEliminateCoordinates[i1]);
            }
        }
    }
    void EliminateBlockCoordinate(Coordinate coordinate)
    {
        for (var i = 0; i < blocks.Count; i++)
        {
            blocks[i].EliminateBlockCoordinate(coordinate);
        }
    }
    
    void EliminateBlock()
    {
        for (var i = blocks.Count - 1; i >= 0; i--)
        {
            blocks[i].EliminateBlock();
        }
        for (var i = 0; i < eliminateLines.Count; i++)
        {
            for (var i1 = 0; i1 < eliminateLines[i].toEliminateGates.Count; i1++)
            {
                eliminateLines[i].toEliminateGates[i1].EliminateGate();
            }

            for (var i1 = 0; i1 < blocks.Count; i1++)
            {
                blocks[i1].OnOneLineResolved();
            }
        }
    }

    public void CheckHighLightBlock()
    {
        CheckBlockEliminate();
        CheckEliminableBlock();
        HighLightLine();
    }

    public void CheckEliminableBlock()
    {
        for (var i = 0; i < eliminateLines.Count; i++)
        {
            for (var i1 = 0; i1 < eliminateLines[i].toEliminateCoordinates.Count; i1++)
            {
                var block = CheckEliminableCoordinate(eliminateLines[i].toEliminateCoordinates[i1]);
                if(block != null && !eliminateLines[i].toEliminateBlocks.Contains(block))
                {
                    eliminateLines[i].toEliminateBlocks.Add(block);
                }
            }
        }
    }
    BaseBlock CheckEliminableCoordinate(Coordinate coordinate)
    {
        for (var i = 0; i < blocks.Count; i++)
        {
            if (blocks[i].CheckHasAbsoluteCoordinate(coordinate))
                return blocks[i];
        }
        return null;
    }
    
    void HighLightLine()
    {
        List<BaseGate> highlightedGates = new List<BaseGate>();
        List<BaseBlock> highlightedBlocks = new List<BaseBlock>();
        for (var i = 0; i < eliminateLines.Count; i++)
        {
            for (var i1 = 0; i1 < eliminateLines[i].toEliminateGates.Count; i1++)
            {
                highlightedGates.Add(eliminateLines[i].toEliminateGates[i1]);
            }
            for (var i1 = 0; i1 < eliminateLines[i].toEliminateBlocks.Count; i1++)
            {
                highlightedBlocks.Add(eliminateLines[i].toEliminateBlocks[i1]);
            }
        }

        for (int i = 0; i < gates.Count; i++)
        {
            gates[i].SetHighlighted(highlightedGates.Contains(gates[i]));
        }
        for (var i = 0; i < blocks.Count; i++)
        {
            blocks[i].SetHighlighted(highlightedBlocks.Contains(blocks[i]));
        }
    }
    #endregion
    
}
[System.Serializable]
public class Coordinate
{
    public int xIndex;
    public int yIndex;
    
    public Coordinate(int x, int y)
    {
        xIndex = x;
        yIndex = y;
    }
    
    public Coordinate(Coordinate coordinate)
    {
        xIndex = coordinate.xIndex;
        yIndex = coordinate.yIndex;
    }

    public void LogInfo()
    {
        Debug.Log($"Coordinate: ({xIndex}, {yIndex})");
    }
}

[System.Serializable]
public class EliminateLine
{
    public List<BaseGate> toEliminateGates = new List<BaseGate>();
    public List<Coordinate> toEliminateCoordinates = new List<Coordinate>();
    public List<BaseBlock> toEliminateBlocks = new List<BaseBlock>();
    public EliminateLine(List<Coordinate> coordinates)
    {
        toEliminateCoordinates = coordinates;
    }
}

public enum TouchDirection
{
    Up,
    Down,
    Left,
    Right,
}
