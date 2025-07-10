using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class IndexShow : MonoBehaviour
{
    public Coordinate coordinate;
    public TextMeshProUGUI indexTxt;
    
    public void SetIndex(int x, int y)
    {
        coordinate.xIndex = x;
        coordinate.yIndex = y;
        indexTxt.text = $"({x},{y})";
    }
}
