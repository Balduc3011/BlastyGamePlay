using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    public MapConstructor mapConstructor;
    public List<BaseBlock> blocks;
    public List<RelativeColor> relativeColors;

    private void Start()
    {
        blocks = mapConstructor.blocks;
        InitBlockColor();
        mapConstructor.SetFloorTiled();
    }

    void InitBlockColor()
    {
        for (var i = 0; i < blocks.Count; i++)
        {
            blocks[i].InitColor(GetRelativeColor(blocks[i].colorCode));
        }
    }

    public ColorCode GetRelativeColor(ColorCode baseColor)
    {
        for (var i = 0; i < relativeColors.Count; i++)
        {
            var relativeColor = relativeColors[i];
            if (relativeColor.baseColor == baseColor)
            {
                return relativeColor.relativeColor;
            }
        }

        return GetNewRelativeColor(baseColor).relativeColor;
    }

    public RelativeColor GetNewRelativeColor(ColorCode baseColor)
    {
        RelativeColor relativeColor = new RelativeColor();
        relativeColors.Add(relativeColor);
        relativeColor.baseColor = baseColor;
        int colorLenght = 0;
        List<ColorCode> colorCodes = new List<ColorCode>();
        foreach (ColorCode colorCode in System.Enum.GetValues(typeof(ColorCode)))
        {
            colorCodes.Add(colorCode);
            colorLenght++;
        }

        ColorCode randomColor = ColorCode.Color1;
        while (colorLenght > 0)
        {
            colorLenght--;
            randomColor = colorCodes[UnityEngine.Random.Range(1, colorLenght + 1)];
            if (!CheckHasRelativeColor(randomColor))
                break;
        }
        relativeColor.relativeColor = randomColor;
        return relativeColor;
    }
    
    bool CheckHasRelativeColor(ColorCode relativeColor)
    {
        for (var i = 0; i < relativeColors.Count; i++)
        {
            if (relativeColors[i].relativeColor == relativeColor)
            {
                return true;
            }
        }
        return false;
    }
}

[System.Serializable]
public class RelativeColor
{
    public ColorCode baseColor;
    public ColorCode relativeColor;
}