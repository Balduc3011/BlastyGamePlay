using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FloorTile : MonoBehaviour
{
    public MapConstructor mapConstructor;
    public Coordinate coordinate;
    public List<TouchDirection> touchDirections;
    public List<FloorTileWall> walls;
    public FloorTileWall wallPrefab;
    public MeshRenderer grainMesh;

    public void SetTiledMaterial(float tillX, float tillY, float offsetX, float offsetY)
    {
        if (grainMesh == null) return;

        Material tiledMaterial = new Material(grainMesh.material);
        // Thay đổi Tiling (x, y)
        tiledMaterial.SetTextureScale("_MainTex", new Vector2(tillX, tillY));

        // Thay đổi Offset (x, y)
        tiledMaterial.SetTextureOffset("_MainTex", new Vector2(offsetX, offsetY));
        grainMesh.material = tiledMaterial;
        grainMesh.gameObject.SetActive(true);
    }

    public void SetTileIndex(int x, int y)
    {
        coordinate.xIndex = x;
        coordinate.yIndex = y;
    }
    
    public void SetTouchDirections(List<TouchDirection> directions)
    {
        touchDirections = directions;
        GenerateWalls();
    }
    
    void GenerateWalls()
    {
        foreach (TouchDirection direction in System.Enum.GetValues(typeof(TouchDirection)))
        {
            if (!touchDirections.Contains(direction))
            {
                SpawnWall(direction);
            }
        }
    }
    
    [Button]
    public void SpawnWall(TouchDirection direction)
    {
        #if UNITY_EDITOR
        FloorTileWall wall =  (FloorTileWall)PrefabUtility.InstantiatePrefab(wallPrefab);
        wall.transform.position = transform.position;
        wall.transform.SetParent(transform);
        #else
        FloorTileWall wall = Instantiate(wallPrefab, transform);
        #endif
        wall.SetTouchDirection(direction);
        walls.Add(wall);
        wall.floorTile = this;
    }
}
