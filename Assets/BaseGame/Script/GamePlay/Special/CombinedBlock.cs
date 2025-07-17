using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class CombinedBlock : MonoBehaviour
{
    public BaseBlock baseBlock;
    public List<GameObject> lockers;
    public ColorCode colorCode;
    
    public void Active(bool active)
    {
        gameObject.SetActive(active);
        enabled = active;
        if(active)
            SetupColor();
    }
    
    public void SetupColor()
    {
        this.colorCode = baseBlock.colorCode;
        string lockerName = ColorGlobalConfig.Instance.GetBlockAll(colorCode).name;
        for (var i = 0; i < lockers.Count; i++)
        {
            lockers[i].SetActive(lockers[i].name == lockerName);
        }
    }
    
    [Button]
    public void SetAngle(int x, int y)
    {
        float lockerAngle = (x == 1 ? 180f : 0) + (y == 1 ? 90f : 0) + (y == -1 ? -90f : 0);
        transform.eulerAngles = new Vector3(0, lockerAngle, 0);
    }
}
