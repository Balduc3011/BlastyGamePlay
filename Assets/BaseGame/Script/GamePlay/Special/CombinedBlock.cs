using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombinedBlock : MonoBehaviour
{
    public BaseBlock baseBlock;
    public List<GameObject> lockers;
    
    public void Active(bool active)
    {
        gameObject.SetActive(active);
        enabled = active;
    }
}
