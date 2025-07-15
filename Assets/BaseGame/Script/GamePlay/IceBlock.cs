using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class IceBlock : MonoBehaviour
{
    public BaseBlock baseBlock;
    [OnValueChanged("UpdateRemain")]
    public int icedRemain;
    public IceCounter iceCounter;

    public void UpdateRemain()
    {
        iceCounter.UpdateRemain(icedRemain);
    }

    public void Active(bool active)
    {
        iceCounter.gameObject.SetActive(active);
        enabled = active;
    }

    public bool IsIceBroke()
    {
        return icedRemain <= 0;
    }
    public void OnOneLineResolved()
    {
        icedRemain--;
        UpdateRemain();
    }
}
