using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class FloorTileWall : MonoBehaviour
{
    [OnValueChanged("OnChangeWallType")]
    public WallType wallType;

    public FloorTile floorTile;
    [field: SerializeField] public Transform Transform {get; private set;}
    public TouchDirection touchDirection;
    public GameObject wallObj;
    public GameObject gateObj;
    public BaseGate gate;
    
    [Button]
    public void SetTouchDirection(TouchDirection direction)
    {
        touchDirection = direction;
        switch (direction)
        {
            case TouchDirection.Up:
                Transform.localRotation = Quaternion.Euler(0, 0, 0);
                break;
            case TouchDirection.Down:
                Transform.localRotation = Quaternion.Euler(0, 180, 0);
                break;
            case TouchDirection.Left:
                Transform.localRotation = Quaternion.Euler(0, -90, 0);
                break;
            case TouchDirection.Right:
                Transform.localRotation = Quaternion.Euler(0, 90, 0);
                break;
            default:
                Transform.localRotation = Quaternion.identity;
                break;
        }
    }

    public void OnChangeWallType()
    {
        switch (wallType)
        {
            case WallType.Wall:
                wallObj.SetActive(true);
                gateObj.SetActive(false);
                floorTile.mapConstructor.RemoveGate(gate);
                break;
            case WallType.Gate:
                wallObj.SetActive(false);
                gateObj.SetActive(true);
                gate.OnChangeColorCode();
                floorTile.mapConstructor.RegisterGate(gate);
                gate.floorTileWall = this;
                break;
            default:
                wallObj.SetActive(true);
                gateObj.SetActive(false);
                floorTile.mapConstructor.RemoveGate(gate);
                break;
        }
    }
}

public enum WallType
{
    Wall = 0,
    Gate = 1,
}
