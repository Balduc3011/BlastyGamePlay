using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class BaseGate : MonoBehaviour
{
    public FloorTileWall floorTileWall;
    [OnValueChanged("OnChangeColorCode")]
    public ColorCode colorCode;
    public MeshFilter meshRenderer;

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
}
