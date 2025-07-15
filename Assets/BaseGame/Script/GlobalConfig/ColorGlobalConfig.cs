using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorGlobalConfig", menuName = "GlobalConfigs/ColorGlobalConfig")]
[GlobalConfig("Assets/Resources/GlobalConfig/")]
public class ColorGlobalConfig : GlobalConfig<ColorGlobalConfig>
{
    public List<BlockColorConfig> blockColorConfigs;
    public List<BlockShapeData> blockShapeDatas;

    [Button]
    public void FetchColor()
    {
        blockColorConfigs = new List<BlockColorConfig>();
        foreach (ColorCode colorCode in System.Enum.GetValues(typeof(ColorCode)))
        {
            BlockColorConfig config = new BlockColorConfig
            {
                colorCode = colorCode,
                blockMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseGame/3D/Material/BlockColor/" + $"Color {(int)colorCode}.mat"),
                grainMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseGame/3D/Material/BlockColor/" + $"M_Pattern {(int)colorCode}.mat")
            };
            blockColorConfigs.Add(config);
        }
    }
    
    public Material GetBlockMaterial(ColorCode colorCode)
    {
        foreach (BlockColorConfig config in blockColorConfigs)
        {
            if (config.colorCode == colorCode)
            {
                return config.blockMaterial;
            }
        }
        return null;
    }
    public Mesh GetGateMesh(ColorCode colorCode)
    {
        foreach (BlockColorConfig config in blockColorConfigs)
        {
            if (config.colorCode == colorCode)
            {
                return config.gateMesh;
            }
        }
        return null;
    }
    
    public GameObject GetBlockAll(ColorCode colorCode)
    {
        foreach (BlockColorConfig config in blockColorConfigs)
        {
            if (config.colorCode == colorCode)
            {
                return config.blockAll;
            }
        }
        return null;
    }

    public BlockShapeData GetBlockShapeData(BlockShape blockShape)
    {
        foreach (BlockShapeData shapeData in blockShapeDatas)
        {
            if (shapeData.blockShape == blockShape)
            {
                return shapeData;
            }
        }

        return null;
    }

    [Button]
    public void CheckHasCoordinate(int x, int y)
    {
        int count = 0;
        for (var i = 0; i < blockShapeDatas.Count; i++)
        {
            if(CheckHasCoordinate(blockShapeDatas[i], x, y))
            {
                count++;
                Debug.Log($"block shape {(int)(blockShapeDatas[i].blockShape)} found");
            }
        }
        Debug.Log($"{count} block has coordinate ({x},{y})");
    }
    public bool CheckHasCoordinate(BlockShapeData blockShapeData, int x, int y)
    {
        for (var i1 = 0; i1 < blockShapeData.blockCoordinates.Count; i1++)
        {
            if(blockShapeData.blockCoordinates[i1].xIndex == x && blockShapeData.blockCoordinates[i1].yIndex == y)
            {
                return true;
            }
        }
        return false;
    }

    public BlockShape GetBlockShape(List<Coordinate> coordinates)
    {
        for (var i = 0; i < blockShapeDatas.Count; i++)
        {
            if(CheckHasCoordinate(blockShapeDatas[i], coordinates))
            {
                return blockShapeDatas[i].blockShape;
            }
        }
        return 0;
    }
    
    bool CheckHasCoordinate(BlockShapeData blockShapeData, List<Coordinate> coordinates)
    {
        int match = 0;
        for (var i = 0; i < coordinates.Count; i++)
        {
            for (var i1 = 0; i1 < blockShapeData.blockCoordinates.Count; i1++)
            {
                if(blockShapeData.blockCoordinates[i1].xIndex == coordinates[i].xIndex && 
                   blockShapeData.blockCoordinates[i1].yIndex == coordinates[i].yIndex)
                {
                    match++;
                    break; // No need to check further for this coordinate
                }
            }
        }
        return match == blockShapeData.blockCoordinates.Count 
               && coordinates.Count == blockShapeData.blockCoordinates.Count;
    }
}

[System.Serializable]
public class BlockColorConfig
{
    public ColorCode colorCode;
    [PreviewField] public Material blockMaterial;
    [PreviewField] public Material grainMaterial;
    [PreviewField] public GameObject blockAll;
    public Mesh gateMesh;
}

[System.Serializable]
public class BlockShapeData
{
    public BlockShape blockShape;
    public string blockName;
    public List<Coordinate> blockCoordinates;
    public Vector3 blockPos;
    public Vector3 blockAngle;
    public Vector3 blockScale = new Vector3(1,1,1);
}

public enum ColorCode
{
    Color1 = 1,
    Color2 = 2,
    Color3 = 3,
    Color4 = 4,
    Color5 = 5,
    Color6 = 6,
    Color7 = 7,
    Color8 = 8,
    Color9 = 9,
    Color10 = 10,
}

public enum BlockShape
{
    B1 = 1,
    B21 = 2,
    B22 = 3,
    B31 = 4,
    B32 = 5,    
    B41 = 6,    // Big Square
    B42 = 7,    // 4 strait blocks horizontal
    B43 = 8,    // 4 strait blocks vertical
    T41 = 9,    // T shape 
    T42 = 10,
    T43 = 11,
    T44 = 12,
    T5 = 13,    // + shape
    L31 = 14,
    L32 = 15,
    L33 = 16,
    L34 = 17,
    L41 = 18,
    L42 = 19,
    L43 = 20,
    L44 = 21,
    L45 = 22,
    L46 = 23,
    L47 = 24,
    L48 = 25,
}