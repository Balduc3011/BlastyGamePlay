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
    public MoveDirection moveDirection;
    
    
    public MapConstructor mapConstructor;
    [HideInInspector] public List<BoxCollider> colliders;
    public Coordinate coordinate;
    public List<Coordinate> blockCoordinates;
    public List<Coordinate> blockAbsoluteCoordinates;
    [HideInInspector] public IndexShow indexShowPrefab;
    [HideInInspector] public List<IndexShow> indexShows;
    public Outline outline;
    public Transform blockModel;
    [HideInInspector] public Vector3 clickPosition;
    [HideInInspector] public Vector3 clickDelta;
    public Rigidbody rigidbody;
    [HideInInspector] public Vector3 newVelocity;
    [HideInInspector] Queue<Vector3> velocityQueue = new Queue<Vector3>();
    [HideInInspector] Vector3 lastClickPos = Vector3.zero;
    [HideInInspector] public List<Transform> blockChild;
    public Transform blockParent;
    public Transform blockBase;
    [HideInInspector] public bool markedToEliminate = false;
    [HideInInspector] public List<ChildBlock> childBlocks;
    [HideInInspector] public List<int> checkedCoordinate = new List<int>();
    private float blockModelY = 0;
    public MeshRenderer selectedMeshRenderer;
    Material[] baseMaterials = new Material[2];
    Material[] lightMaterials = new Material[2];
    public Material lightMaterial;
    Coordinate lastCheckCoordinate;
    bool pickingUp = false;
    bool isHighlighted = false;
    // SpecialBlock
    
    [OnValueChanged("ActiveIced")]
    public bool isIced;
    [ShowIf("isIced")] public IceBlock iceBlock;
    [OnValueChanged("ActiveCombined")]
    public bool isCombined;
    [ShowIf("isCombined")] public List<CombinedBlock> combinedBlocks;
    [ShowIf("isCombined")] public BaseBlock combinedTarget;
    
    private void Start()
    {
        InitFirstValue();
    }

    void InitFirstValue()
    {
        transform.position = mapConstructor.GetNearestCordinate(transform.position, out coordinate);
        rigidbody.velocity = Vector3.zero;
        rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        blockModelY = blockModel.localPosition.y;
        outline.enabled = false;
        SetBlockNewPos();
        InitMaterial();
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

    public void Init()
    {
        OnChangeColorCode();
    }
    
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
        outline.OutlineColor = ColorGlobalConfig.Instance.GetOutlineColor(colorCode);
        if (go != null)
        {
            GameObject newSpawn = (GameObject)PrefabUtility.InstantiatePrefab(go);
            blockBase = newSpawn.transform;
            blockBase.SetParent(blockParent);
            blockBase.localPosition = Vector3.zero;
            blockChild = GetListTransformsByName(blockBase, "Block", "BlockModel");
        }
        OnChangeShape();
    }

    [Button]
    public void ClearBlock()
    {
        if(blockBase != null)
            DestroyImmediate(blockBase.gameObject);
    }
    [Button]
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
            selectedMeshRenderer = selectedBlock.GetComponent<MeshRenderer>();
        }
        CheckIndexShow(blockShapeData.blockCoordinates);
        InitCollider();
    }

    void InitCollider()
    {
        for (var i = 0; i < blockCoordinates.Count; i++)
        {
            if (i >= colliders.Count)
            {
                BoxCollider newCollider = gameObject.AddComponent<BoxCollider>();
                newCollider.material = ColorGlobalConfig.Instance.blockPhysicMaterial;
                colliders.Add(newCollider);
            }
            colliders[i].enabled = true;
            colliders[i].size = new Vector3(0.9f, 1f, 0.9f);
            colliders[i].center = new Vector3(blockCoordinates[i].xIndex, 0.5f, blockCoordinates[i].yIndex);
        }
        for(int i = blockCoordinates.Count; i < colliders.Count; i++)
        {
            colliders[i].enabled = false;
        }
    }

    void InitMaterial()
    {
        if(selectedMeshRenderer == null)
            return;
        baseMaterials = selectedMeshRenderer.materials;
        lightMaterials = new Material[2];
        for (var i = 0; i < baseMaterials.Length; i++)
        {
            if (baseMaterials[i].name == "M_Color"
                || baseMaterials[i].name == "M_Color (Instance)")
            {
                lightMaterials[i] = lightMaterial;
            }
            else
            {
                lightMaterials[i] = baseMaterials[i];
            }
        }
    }

    public List<Transform> GetListTransformsByName(Transform parent, string nameContains, string negative)
    {
        List<Transform> matchingTransforms = new List<Transform>();
        Transform[] allTransforms = parent.GetComponentsInChildren<Transform>();

        foreach (Transform child in allTransforms)
        {
            Debug.Log(child.name);
            if (child.name.Contains(nameContains))
            {
                matchingTransforms.Add(child);
            }
        }
        return matchingTransforms;
    }

    void ActiveIced()
    {
        if (isIced)
        {
            if(iceBlock == null)
                iceBlock = gameObject.AddComponent<IceBlock>();
            if(iceBlock.iceCounter == null)
                iceBlock.iceCounter = Instantiate(ColorGlobalConfig.Instance.iceCounterPrefab, indexShowPrefab.transform.parent);
            iceBlock.baseBlock = this;
            iceBlock.iceCounter.transform.localPosition = Vector3.up * 1f;
            iceBlock.Active(true);
        }
        else
        {
            if(iceBlock != null)
            {
                iceBlock.Active(false);
            }
        }
    }

    void ActiveCombined()
    {
        
    }
    
    #endregion

    #region GamePlay

    public void OnBlockSelected(Vector3 clickPosition)
    {
        this.clickPosition = clickPosition;
        clickDelta = clickPosition - transform.position;
        blockModel.localPosition = Vector3.up * (blockModelY + 0.2f);
        mapConstructor.SetSelectedBlock(this);
        SetBGBlock(false);
        pickingUp = true;
        outline.enabled = pickingUp;
    }
    
    public void OnBlockDeselected()
    {
        blockModel.localPosition = Vector3.up * blockModelY;
        SetBlockNewPos();
        mapConstructor.DeSelectBlock();
        rigidbody.velocity = Vector3.zero;
        SetBGBlock(true);
        pickingUp = false;
        outline.enabled = pickingUp;
    }

    public void SetHighlighted(bool isHighlighted)
    {
        if (selectedMeshRenderer == null)
            return;
        if (isHighlighted && !this.isHighlighted)
        {
            outline.enabled = false;
            selectedMeshRenderer.materials = lightMaterials;
        }
        else if(!isHighlighted && this.isHighlighted)
        {
            outline.enabled = false;
            selectedMeshRenderer.materials = baseMaterials;
        }
        this.isHighlighted = isHighlighted;
        outline.enabled = pickingUp;
    }
    
    void SetBlockNewPos()
    {
        transform.position = mapConstructor.GetNearestCordinate(transform.position, out coordinate);
        if(lastCheckCoordinate == null)
            lastCheckCoordinate = new Coordinate(coordinate.xIndex, coordinate.yIndex);
        lastCheckCoordinate.xIndex = coordinate.xIndex;
        lastCheckCoordinate.yIndex = coordinate.yIndex;
        SetWorldCoordinate();
    }

    void SetWorldCoordinate(Coordinate sample = null)
    {
        if(sample == null)
            sample = new Coordinate(coordinate.xIndex, coordinate.yIndex);
        for (int i = 0; i < blockCoordinates.Count; i++)
        {
            if(i >= blockAbsoluteCoordinates.Count) 
                blockAbsoluteCoordinates.Add(new Coordinate(0, 0));
            blockAbsoluteCoordinates[i].xIndex = blockCoordinates[i].xIndex + sample.xIndex;
            blockAbsoluteCoordinates[i].yIndex = blockCoordinates[i].yIndex + sample.yIndex;
        }
    }

    public bool IsBlockMoveable()
    {
        if(isIced && iceBlock != null && !iceBlock.IsIceBroke())
        {
            return false;
        }

        return true;
    }
    
    public void SetBlockPosition(Vector3 position)
    {
        if(position.y != 0)
            return;
        if(!IsBlockMoveable()) 
            return;
        lastClickPos = position;
        Vector3 distance = position - clickDelta - rigidbody.position;
        distance = Vector3.ClampMagnitude(distance, 1);
        distance = ClampVelocity(distance);
        newVelocity = (distance) * 30;
        velocityQueue.Enqueue(newVelocity);
    }
    
    Vector3 ClampVelocity(Vector3 velocity)
    {
        if (moveDirection == MoveDirection.Horizontal)
        {
            velocity.z = 0;
        }
        else if (moveDirection == MoveDirection.Vertical)
        {
            velocity.x = 0;
        }
        return velocity;
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
            CheckHighLightBlock();
        }
    }

    void CheckHighLightBlock()
    {
        Coordinate checkCoordinate;
        if (mapConstructor.CheckLastCheckCoordinate(transform.position, out checkCoordinate))
        {
            if(checkCoordinate.xIndex != lastCheckCoordinate.xIndex || 
               checkCoordinate.yIndex != lastCheckCoordinate.yIndex)
            {
                lastCheckCoordinate.xIndex = checkCoordinate.xIndex;
                lastCheckCoordinate.yIndex = checkCoordinate.yIndex;
                SetWorldCoordinate(checkCoordinate);
                mapConstructor.CheckHighLightBlock();
            }
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
        if (!IsBlockMoveable())
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
    
    public bool CheckHasAbsoluteCoordinate(Coordinate coordinate)
    {
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
        SpawnNewChild();
    }
    
    void CheckHasCoordinate()
    {
        if(childBlocks == null)
            childBlocks = new List<ChildBlock>();
        checkedCoordinate = new List<int>();
        childBlocks.Clear();
        while (blockCoordinates.Count > 0)
        {
            ChildBlock childBlock = new ChildBlock();
            childBlocks.Add(childBlock);
            childBlock.firstCoordinate = new Coordinate(blockCoordinates[0]);
            childBlock.sampleList = new List<Coordinate>();
            checkedCoordinate.Clear();
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
            BaseBlock newBlock = Instantiate(ColorGlobalConfig.Instance.baseBlockPrefab, transform.parent);
            newBlock.blockShape = child.blockShape;
            newBlock.colorCode = colorCode;
            newBlock.mapConstructor = mapConstructor;
            mapConstructor.AddBlock(newBlock);
            newBlock.Init();
            if (ColorGlobalConfig.Instance.CheckHasCoordinate(child.blockShape, 0, 0))
            {
                newBlock.transform.localPosition = transform.position + 
                    new Vector3(child.firstCoordinate.xIndex, 0, child.firstCoordinate.yIndex);
            }
            else
            {
                newBlock.transform.localPosition = transform.position;
            }
        }
    }
    
    public void OnOneLineResolved()
    {
        if (isIced && iceBlock != null)
        {
            iceBlock.OnOneLineResolved();
            if (iceBlock.IsIceBroke())
            {
                isIced = false;
                ActiveIced();
            }
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

public enum MoveDirection
{
    All = 0,
    Horizontal = 1,
    Vertical = 2,
}