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
                grainMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/BaseGame/3D/Material/BlockColor/" + $"Color {(int)colorCode}.mat")
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
}

[System.Serializable]
public class BlockColorConfig
{
    public ColorCode colorCode;
    [PreviewField] public Material blockMaterial;
    [PreviewField] public Material grainMaterial;
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
}