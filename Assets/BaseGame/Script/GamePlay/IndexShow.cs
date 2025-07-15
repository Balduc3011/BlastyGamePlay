using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class IndexShow : MonoBehaviour
{
    public Coordinate coordinate;
    public TextMeshProUGUI indexTxt;
    public bool coordinateChecked = false;
    
    public void SetIndex(int x, int y)
    {
        coordinate.xIndex = x;
        coordinate.yIndex = y;
        indexTxt.text = $"({x},{y})";
    }

    public void CheckCoordinate(Coordinate toCheck)
    {
        if(coordinateChecked)
            return;
        if(coordinate.xIndex == toCheck.xIndex && coordinate.yIndex == toCheck.yIndex)
        {
            indexTxt.color = Color.white;
            coordinateChecked = true;
        }
        else
        {
            indexTxt.color = Color.black;
        }
    }
}
