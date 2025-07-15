using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public BaseBlock selectedBlock;
    public bool blockSelected = false;
    public LayerMask blockLayerMask;
    public LayerMask groundLayerMask;
    void Update()
    {
        CheckBlock();
        CheckDragCoordinate();
    }

    void CheckBlock()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                BaseBlock baseBlock = hit.collider.GetComponent<BaseBlock>();
                if (baseBlock != null)
                {
                    if (baseBlock.IsBlockMoveable())
                    {
                        selectedBlock = baseBlock;
                        blockSelected = true;
                        selectedBlock.OnBlockSelected(GetMousePointOnGround());
                    }
                }
            }
        }
        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            if(selectedBlock != null)
            {
                selectedBlock.OnBlockDeselected();
            }
            selectedBlock = null;
            blockSelected = false;
        }
    }

    void CheckDragCoordinate()
    {
        if (blockSelected)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1000, groundLayerMask))
            {
                SetBlockPosition(GetMousePointOnGround());
            }
        }
    }
    
    Vector3 GetMousePointOnGround()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000, groundLayerMask))
        {
            return hit.point;
        }
        return Vector3.up * 1000;
    }

    void SetBlockPosition(Vector3 position)
    {
        if (blockSelected && selectedBlock != null)
        {
            selectedBlock.SetBlockPosition(position);
        }
    }
    
}
