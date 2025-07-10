using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class BaseGate : MonoBehaviour
{
    public FloorTileWall floorTileWall;
    [OnValueChanged("OnChangeColorCode")]
    public ColorCode colorCode;
    public MeshRenderer meshRenderer;

    public void OnChangeColorCode()
    {
        meshRenderer.material = ColorGlobalConfig.Instance.GetBlockMaterial(colorCode);
    }
}
