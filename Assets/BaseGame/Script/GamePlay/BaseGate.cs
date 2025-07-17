using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class BaseGate : MonoBehaviour
{
    public FloorTileWall floorTileWall;
    [OnValueChanged("OnChangeColorCode")]
    public ColorCode colorCode;
    public MeshFilter meshRenderer;
    public MeshRenderer meshRendererR;
    public Material baseMaterial;
    public Material lightMaterial;

    public void OnChangeColorCode()
    {
        meshRenderer.mesh = ColorGlobalConfig.Instance.GetGateMesh(colorCode);
    }
    
    public void InitColor(ColorCode relativeColor)
    {
        if (meshRenderer != null)
        {
            meshRenderer.mesh = ColorGlobalConfig.Instance.GetGateMesh(colorCode);
        }
    }

    public void EliminateGate(float delay)
    {
        DOVirtual.DelayedCall(delay, EliminateGate);
    }

    void EliminateGate()
    {
        floorTileWall.SwitchWallType(WallType.Wall);
    }
    
    public void SetHighlighted(bool isHighlighted)
    {
        if (meshRendererR == null)
            return;
        if (isHighlighted)
        {
            meshRendererR.material = lightMaterial;
        }
        else
        {
            meshRendererR.material = baseMaterial;
        }
    }
}
