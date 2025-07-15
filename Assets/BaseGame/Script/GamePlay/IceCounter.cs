using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class IceCounter : MonoBehaviour
{
    public TextMeshProUGUI counterTxt;
    
    public void UpdateRemain(int remain)
    {
        if (counterTxt != null)
        {
            counterTxt.text = $"{remain}";
        }
    }
}
