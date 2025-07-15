using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class BaseBlock : MonoBehaviour
{
    [OnValueChanged("OnChangeColorCode")]
    public ColorCode colorCode;
    [OnValueChanged("OnChangeShape")]
    public BlockShape blockShape;
    public MapConstructor mapConstructor;
    public Coordinate coordinate;
    public List<Coordinate> blockCoordinates;
    public List<Coordinate> blockAbsoluteCoordinates;
    public IndexShow indexShowPrefab;
    public List<IndexShow> indexShows;
    public Transform blockModel;
    public Vector3 clickPosition;
    public Vector3 clickDelta;
    public Rigidbody rigidbody;
    public Vector3 newVelocity;
    Queue<Vector3> velocityQueue = new Queue<Vector3>();
    Vector3 lastClickPos = Vector3.zero;
    public List<Transform> blockChild;
    public Transform blockParent;
    public Transform blockBase;
    public bool markedToEliminate = false;
    public List<ChildBlock> childBlocks;
    public List<int> checkedCoordinate = new List<int>();
    private void Start()
    {
        InitFirstValue();
    }

    void InitFirstValue()
    {
        transform.position = mapConstructor.GetNearestCordinate(transform.position, out coordinate);
        rigidbody.velocity = Vector3.zero;
        rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        SetBlockNewPos();
    }
    

    #region Editor

    [Button]
    public void SetupIndexShows()
    {
        ClearIndexShows();
        indexShows = new List<IndexShow>();
        foreach (var coordinate in blockCoordinates)
        {
            IndexShow indexShow = Instantiate(indexShowPrefab, transform);
            indexShow.gameObject.SetActive(true);
            indexShow.transform.localPosition = new Vector3(coordinate.xIndex, 1.01f, coordinate.yIndex);
            indexShow.SetIndex(coordinate.xIndex, coordinate.yIndex);
            indexShows.Add(indexShow);
            indexShow.transform.SetParent(indexShowPrefab.transform.parent);
        }
    }
    
    [Button]
    public void ClearIndexShows()
    {
        foreach (var indexShow in indexShows)
        {
            DestroyImmediate(indexShow.gameObject);
        }
        indexShows.Clear();
    }

    void CheckIndexShow(List<Coordinate> coordinates)
    {
        foreach (var indexShow in indexShows)
        {
            indexShow.coordinateChecked = false;
        }

        for (var i = 0; i < indexShows.Count; i++)
        {
            for (var i1 = 0; i1 < coordinates.Count; i1++)
            {
                indexShows[i].CheckCoordinate(coordinates[i1]);
            }
        }
    }
    

    #endregion

    #region Init

    public void InitColor(ColorCode relativeColor)
    {
        // if (meshRenderer != null)
        // {
        //     meshRenderer.material = ColorGlobalConfig.Instance.GetBlockMaterial(relativeColor);
        // }
    }
    public void OnChangeColorCode()
    {
        ClearBlock();
        GameObject go = ColorGlobalConfig.Instance.GetBlockAll(colorCode);
        if (go != null)
        {
            GameObject newSpawn = (GameObject)PrefabUtility.InstantiatePrefab(go);
            blockBase = newSpawn.transform;
            blockBase.SetParent(blockParent);
            blockBase.localPosition = Vector3.zero;
            blockChild = GetListTransformsByName(blockBase, "Block_");
        }
        OnChangeShape();
    }

    [Button]
    public void ClearBlock()
    {
        if(blockBase != null)
            DestroyImmediate(blockBase.gameObject);
    }

    public void OnChangeShape()
    {
        BlockShapeData blockShapeData = ColorGlobalConfig.Instance.GetBlockShapeData(blockShape);
        if (blockShapeData == null)
        {
            return;
        }
        Transform selectedBlock = null;
        for (var i = 0; i < blockChild.Count; i++)
        {
            blockChild[i].gameObject.SetActive(blockChild[i].gameObject.name == blockShapeData.blockName);
            if(selectedBlock == null)
                selectedBlock = blockChild[i].gameObject.name == blockShapeData.blockName ? blockChild[i] : null;
        }

        if (selectedBlock != null)
        {
            selectedBlock.transform.localPosition = blockShapeData.blockPos;
            selectedBlock.localEulerAngles = blockShapeData.blockAngle;
            selectedBlock.localScale = blockShapeData.blockScale;
            blockCoordinates = new List<Coordinate>();
            blockCoordinates.AddRange(blockShapeData.blockCoordinates);
        }
        CheckIndexShow(blockShapeData.blockCoordinates);
    }

    public List<Transform> GetListTransformsByName(Transform parent, string nameContains)
    {
        List<Transform> matchingTransforms = new List<Transform>();
        Transform[] allTransforms = parent.GetComponentsInChildren<Transform>();

        foreach (Transform child in allTransforms)
        {
            if (child.name.Contains(nameContains))
            {
                matchingTransforms.Add(child);
            }
        }
        return matchingTransforms;
    }
    #endregion

    #region GamePlay

    public void OnBlockSelected(Vector3 clickPosition)
    {
        this.clickPosition = clickPosition;
        clickDelta = clickPosition - transform.position;
        blockModel.localPosition = Vector3.up * 0.2f;
        mapConstructor.SetSelectedBlock(this);
        SetBGBlock(false);
    }
    
    public void OnBlockDeselected()
    {
        blockModel.localPosition = Vector3.zero;
        SetBlockNewPos();
        mapConstructor.DeSelectBlock();
        rigidbody.velocity = Vector3.zero;
        SetBGBlock(true);
    }

    void SetBlockNewPos()
    {
        transform.position = mapConstructor.GetNearestCordinate(transform.position, out coordinate);
        SetWorldCoordinate();
    }

    void SetWorldCoordinate()
    {
        for (int i = 0; i < blockCoordinates.Count; i++)
        {
            if(i >= blockAbsoluteCoordinates.Count) 
                blockAbsoluteCoordinates.Add(new Coordinate(0, 0));
            blockAbsoluteCoordinates[i].xIndex = blockCoordinates[i].xIndex + coordinate.xIndex;
            blockAbsoluteCoordinates[i].yIndex = blockCoordinates[i].yIndex + coordinate.yIndex;
        }
    }
    
    public void SetBlockPosition(Vector3 position)
    {
        if(position.y != 0)
            return;
        lastClickPos = position;
        Vector3 distance = position - clickDelta - rigidbody.position;
        distance = Vector3.ClampMagnitude(distance, 1);
        newVelocity = (distance) * 30;
        velocityQueue.Enqueue(newVelocity);
    }

    private void FixedUpdate()
    {
        if (velocityQueue != null)
        {
            if(velocityQueue.Count > 0)
            {
                Vector3 velocity = velocityQueue.Dequeue();
                rigidbody.velocity = velocity;
            }
            else
            {
                rigidbody.velocity = Vector3.zero;
            }
            velocityQueue.Clear();
        }
    }

    public void SetBGBlock(bool isBGBlock)
    {
        if (!isBGBlock)
        {
            rigidbody.isKinematic = false;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        }
        else
        {
            rigidbody.isKinematic = true;
            rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
    }
    
    public bool HasBlockOnTile(Coordinate coordinate, ColorCode colorCode)
    {
        if(this.colorCode != colorCode)
            return false;
        for (var i = 0; i < blockAbsoluteCoordinates.Count; i++)
        {
            if(blockAbsoluteCoordinates[i].xIndex == coordinate.xIndex && 
               blockAbsoluteCoordinates[i].yIndex == coordinate.yIndex)
            {
                return true;
            }
        }
        return false;
    }
    
    public void EliminateBlockCoordinate(Coordinate toEliminamteCoordinate)
    {
        for (var i = 0; i < blockAbsoluteCoordinates.Count; i++)
        {
            if(blockAbsoluteCoordinates[i].xIndex == toEliminamteCoordinate.xIndex && 
               blockAbsoluteCoordinates[i].yIndex == toEliminamteCoordinate.yIndex)
            {
                markedToEliminate = true;
                blockAbsoluteCoordinates.RemoveAt(i);
                blockCoordinates.RemoveAt(i);
                break;
            }
        }
    }

    public void EliminateBlock()
    {
        if(!markedToEliminate)
            return;
        mapConstructor.RemoveBlock(this);
        gameObject.SetActive(false);
        CheckNewChildBlock();
    }
    [Button]
    public void CheckNewChildBlock()
    {
        CheckHasCoordinate();
        //SpawnNewChild();
    }
    
    void CheckHasCoordinate()
    {
        if(childBlocks == null)
            childBlocks = new List<ChildBlock>();
        childBlocks.Clear();
        while (blockCoordinates.Count > 0)
        {
            ChildBlock childBlock = new ChildBlock();
            childBlocks.Add(childBlock);
            childBlock.firstCoordinate = new Coordinate(blockCoordinates[0]);
            childBlock.sampleList = new List<Coordinate>();
            checkedCoordinate = new List<int>();
            childBlock. sampleList = CheckHasCoordinate(new Coordinate(blockCoordinates[0]));
            childBlock.sampleList = RemoveDuplicateCoordinates(childBlock.sampleList);
            RemoveBlockCoordinate(childBlock.sampleList);
            SimplifyCoordinates(childBlock.sampleList);
            childBlock.blockShape = ColorGlobalConfig.Instance.GetBlockShape(childBlock.sampleList);
        }
    }
    
    List<Coordinate> CheckHasCoordinate(Coordinate coordinate)
    {
        List<Coordinate> foundCoordinates = new List<Coordinate>();
        for (var i = 0; i < blockCoordinates.Count; i++)
        {
            if(!checkedCoordinate.Contains(i)
                && blockCoordinates[i].xIndex == coordinate.xIndex 
                && blockCoordinates[i].yIndex == coordinate.yIndex)
            {
                checkedCoordinate.Add(i);
                foundCoordinates.Add(coordinate);
                foundCoordinates.AddRange(CheckHasCoordinate(new Coordinate(coordinate.xIndex + 1, coordinate.yIndex)));
                foundCoordinates.AddRange(CheckHasCoordinate(new Coordinate(coordinate.xIndex, coordinate.yIndex + 1)));
                foundCoordinates.AddRange(CheckHasCoordinate(new Coordinate(coordinate.xIndex - 1, coordinate.yIndex)));
                foundCoordinates.AddRange(CheckHasCoordinate(new Coordinate(coordinate.xIndex, coordinate.yIndex - 1)));
                break;
            }
        }
        return foundCoordinates;
    }
    
    public List<Coordinate> RemoveDuplicateCoordinates(List<Coordinate> coordinates)
    {
        HashSet<string> uniqueCoordinates = new HashSet<string>();
        List<Coordinate> filteredCoordinates = new List<Coordinate>();

        foreach (var coordinate in coordinates)
        {
            string key = $"{coordinate.xIndex},{coordinate.yIndex}";
            if (!uniqueCoordinates.Contains(key))
            {
                uniqueCoordinates.Add(key);
                filteredCoordinates.Add(coordinate);
            }
        }
        return filteredCoordinates;
    }
    
    public void SimplifyCoordinates(List<Coordinate> coordinates)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        for (var i = 0; i < coordinates.Count; i++)
        {
            if(coordinates[i].xIndex < minX)
                minX = coordinates[i].xIndex;
            if(coordinates[i].yIndex < minY)
                minY = coordinates[i].yIndex;
        }
        for (var i = 0; i < coordinates.Count; i++)
        {
            coordinates[i].xIndex -= minX;
            coordinates[i].yIndex -= minY;
        }
    }

    void RemoveBlockCoordinate(List<Coordinate> coordinates)
    {
        for (var i = 0; i < coordinates.Count; i++)
        {
            for (var i1 = blockCoordinates.Count - 1; i1 >= 0; i1--)
            {
                if(blockCoordinates[i1].xIndex == coordinates[i].xIndex && 
                   blockCoordinates[i1].yIndex == coordinates[i].yIndex)
                {
                    blockCoordinates.RemoveAt(i1);
                }
            }
        }
    }

    void SpawnNewChild()
    {
        for (var i = 0; i < childBlocks.Count; i++)
        {
            var child = childBlocks[i];
            BaseBlock newBlock = Instantiate(mapConstructor.baseBlockPrefab, transform.parent);
            newBlock.blockShape = child.blockShape;
            newBlock.colorCode = colorCode;
        }
    }
    #endregion
}

[System.Serializable]
public class ChildBlock
{
    public BlockShape blockShape;
    public Coordinate firstCoordinate;
    public List<Coordinate> sampleList = new List<Coordinate>();
}