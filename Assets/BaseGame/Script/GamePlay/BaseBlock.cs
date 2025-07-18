using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [OnValueChanged("OnChangeMoveDirection")]
    public MoveDirection moveDirection;
    
    public MapConstructor mapConstructor;
    [HideInInspector] public List<BoxCollider> colliders;
    public Coordinate coordinate;
    public List<Coordinate> blockCoordinates;
    public List<Coordinate> blockAbsoluteCoordinates;
    [HideInInspector] public IndexShow indexShowPrefab;
    [HideInInspector] public List<IndexShow> indexShows;
    public Outline blockOutline;
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
    [HideInInspector] public Material[] baseMaterials = new Material[2];
    [HideInInspector] public Material[] lightMaterials = new Material[2];
    public Material lightMaterial;
    Coordinate lastCheckCoordinate;
    bool pickingUp = false;
    bool isHighlighted = false;
    public bool isResolved;
    // SpecialBlock
    public Transform arrowBase;
    public List<Transform> arrows;
    
    [OnValueChanged("ActiveIced")]
    public bool isIced;
    [ShowIf("isIced")] public IceBlock iceBlock;
    
    [field:SerializeField] public bool isCombined { get; set; }
    [ShowIf("isCombined")] public Transform combinedBlockParent;
    [ShowIf("isCombined")] public List<CombinedBlock> combinedBlocks;
    [ShowIf("isCombined")] public List<BaseBlock> combinedTargets;
    [ShowIf("isCombined")] public List<BaseBlock> realCombinedTargets;
    [ShowIf("isCombined")] public List<FixedJoint> combineJoints;
    
    public virtual void Start()
    {
        InitFirstValue();
        InitCombined();
    }

    void InitFirstValue()
    {
        transform.position = mapConstructor.GetNearestCordinate(transform.position, out coordinate);
        rigidbody.velocity = Vector3.zero;
        rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        blockModelY = blockModel.localPosition.y;
        if(blockOutline != null)
            blockOutline.enabled = false;
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
        // foreach (var indexShow in indexShows)
        // {
        //     indexShow.coordinateChecked = false;
        // }
        //
        // for (var i = 0; i < indexShows.Count; i++)
        // {
        //     for (var i1 = 0; i1 < coordinates.Count; i1++)
        //     {
        //         indexShows[i].CheckCoordinate(coordinates[i1]);
        //     }
        // }
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

    public void InitCoordinate()
    {
        SetBlockNewPos();
        SetWorldCoordinate();
    }
    public virtual void OnChangeColorCode()
    {
        ClearBlock();
        GameObject go = ColorGlobalConfig.Instance.GetBlockAll(colorCode);
        if(blockOutline != null)
            blockOutline.OutlineColor = ColorGlobalConfig.Instance.GetOutlineColor(colorCode);
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
    public virtual void OnChangeShape()
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
            float xSize = 0.9f;
            float ySize = 0.9f;
            if (CheckHasLocalCoordinate(blockCoordinates[i].xIndex + 1, blockCoordinates[i].yIndex))
                xSize = 1f;
            if (CheckHasLocalCoordinate(blockCoordinates[i].xIndex, blockCoordinates[i].yIndex + 1))
                ySize = 1f;
            
            colliders[i].size = new Vector3(xSize, 1f, ySize);
            colliders[i].center = new Vector3(blockCoordinates[i].xIndex + (xSize == 1f ? 0.05f : 0f), 0.5f, blockCoordinates[i].yIndex + (ySize == 1f ? 0.05f : 0f));
        }
        for(int i = blockCoordinates.Count; i < colliders.Count; i++)
        {
            colliders[i].enabled = false;
        }
    }

    public void InitMaterial()
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
            if (child.name.Contains(nameContains))
            {
                matchingTransforms.Add(child);
            }
        }
        return matchingTransforms;
    }
    
    bool CheckHasLocalCoordinate(int x, int y)
    {
        for (var i = 0; i < blockCoordinates.Count; i++)
        {
            if (blockCoordinates[i].xIndex == x && 
                blockCoordinates[i].yIndex == y)
            {
                return true;
            }
        }
        return false;
    }

    #region Arrow

    void OnChangeMoveDirection()
    {
        if (moveDirection == MoveDirection.All)
        {
            if(arrowBase != null)
                arrowBase.gameObject.SetActive(false);
            return;
        }
        if (arrowBase == null)
        {
            arrowBase = Instantiate(ColorGlobalConfig.Instance.arrowBasePrefab, blockParent);
            arrows = GetListTransformsByName(arrowBase, "Arrow_", "Negative");
        }
        arrowBase.localPosition = Vector3.zero;
        arrowBase.gameObject.SetActive(true);
        if(moveDirection == MoveDirection.Horizontal)
        {
            SetHorizontalArrow();
        }
        else if(moveDirection == MoveDirection.Vertical)
        {
            SetVerticalArrow();
        }
    }

    void SetHorizontalArrow()
    {
        List<int> yIndexs = new List<int>();
        int maxX = 0;
        for (int i = 0; i < blockCoordinates.Count; i++)
        {
            if (!yIndexs.Contains(blockCoordinates[i].yIndex))
                yIndexs.Add(blockCoordinates[i].yIndex);
            if(blockCoordinates[i].xIndex > maxX)
               maxX = blockCoordinates[i].xIndex;
        }
        int maxLenght = 0;
        int selectedYIndex = 0;
        int maxCount = 0;
        for (int i = 0; i < yIndexs.Count; i++)
        {
            int curLenght = 0;
            for (int j = 0; j < maxX + 1; j++)
            {
                if(CheckHasLocalCoordinate(j, yIndexs[i]))
                {
                    curLenght++;
                }
            }
            if(curLenght >= maxLenght)
            {
                maxLenght = curLenght;
                selectedYIndex = yIndexs[i];
                maxCount++;
            }
        }
        float yIndex = selectedYIndex;
        if(maxCount == yIndexs.Count)
        {
            yIndex = (maxCount - 1) * 0.5f;
        }
        Transform toShowArrow = GetArrow(maxLenght);
        toShowArrow.localEulerAngles = Vector3.up * 90;
        toShowArrow.localPosition = new Vector3((maxLenght - 1) * 0.5f, 0, yIndex);
    }

    void SetVerticalArrow()
    {
        List<int> xIndexes = new List<int>();
        int maxY = 0;

        // Collect unique xIndexes and find the maximum yIndex
        for (int i = 0; i < blockCoordinates.Count; i++)
        {
            if (!xIndexes.Contains(blockCoordinates[i].xIndex))
                xIndexes.Add(blockCoordinates[i].xIndex);
            if (blockCoordinates[i].yIndex > maxY)
                maxY = blockCoordinates[i].yIndex;
        }

        int maxLength = 0;
        int selectedXIndex = 0;
        int maxCount = 0;
        // Determine the xIndex with the longest vertical line
        for (int i = 0; i < xIndexes.Count; i++)
        {
            int currentLength = 0;
            for (int j = 0; j < maxY + 1; j++)
            {
                if (CheckHasLocalCoordinate(xIndexes[i], j))
                {
                    currentLength++;
                }
            }
            if (currentLength >= maxLength)
            {
                maxLength = currentLength;
                selectedXIndex = xIndexes[i];
                maxCount++;
            }
        }
        float xIndex = selectedXIndex;
        if(maxCount == xIndexes.Count)
        {
            xIndex = (maxCount - 1) * 0.5f;
        }
        Transform toShowArrow = GetArrow(maxLength);
        toShowArrow.localEulerAngles = Vector3.zero;
        toShowArrow.localPosition = new Vector3(xIndex, 0, (maxLength - 1) * 0.5f);
    }

    Transform GetArrow(int lenght)
    {
        string name = $"Arrow_0{lenght}";
        int index = 0;
        for (int i = 0; i < arrows.Count; i++)
        {
            if(arrows[i].gameObject.name == name)
            {
                index = i;
            }
            arrows[i].gameObject.SetActive(arrows[i].gameObject.name == name);
        }
        return arrows[index];
    }

    #endregion

    #region Ice

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

    #endregion

    #region CombinedBlock

    public void ActiveCombined()
    {
        isCombined = true;
    }
    
    public void AddCombinedTarget(BaseBlock block = null)
    {
        if (block != null && block != this)
        {
            if (combinedTargets == null)
                combinedTargets = new List<BaseBlock>();
            if (!combinedTargets.Contains(block))
                combinedTargets.Add(block);
        }
    }
    
    public void RemoveCombinedTarget(BaseBlock block)
    {
        if (combinedTargets == null || !combinedTargets.Contains(block))
            return;
        combinedTargets.Remove(block);
    }
    
    [Button]
    public void InitCombined()
    {
        if (combinedTargets == null || !isCombined)
            return;
        for (var i = 0; i < combinedBlocks.Count; i++)
        {
            combinedBlocks[i].gameObject.SetActive(false);
        }
        for (int i = 0; i < combinedTargets.Count; i++)
        {
            CheckCombinedTarget(combinedTargets[i]);
        }
        CheckJoint();
    }

    void CheckJoint()
    {
        if(combineJoints == null)
            combineJoints = new List<FixedJoint>();
        for (int i = 0; i < realCombinedTargets.Count; i++)
        {
            if (i >= combineJoints.Count)
            {
                FixedJoint joint = gameObject.AddComponent<FixedJoint>();
                combineJoints.Add(joint);
            }
        }

        for (int i = combineJoints.Count - 1; i >= 0; i--)
        {
            if(i < realCombinedTargets.Count)
            {
                combineJoints[i].connectedBody = realCombinedTargets[i].rigidbody;
                combineJoints[i].breakForce = 1000000f;
                combineJoints[i].breakTorque = 1000000f;
            }
            else
            {
                Destroy(combineJoints[i]);
                combineJoints.RemoveAt(i);
            }
        }
    }
    
    void OnJointBreak(float breakForce)
    {
        Debug.Log($"Joint broke with force: {breakForce}");
        // Recreate the joint if necessary
        for (int i = 0; i < combineJoints.Count; i++)
        {
            if (combineJoints[i] == null || combineJoints[i].connectedBody == null)
            {
                FixedJoint newJoint = gameObject.AddComponent<FixedJoint>();
                newJoint.connectedBody = combinedTargets[i].rigidbody;
                newJoint.breakForce = 1000000f; // Set higher breakForce
                newJoint.breakTorque = 1000000f; // Set higher breakTorque
                combineJoints[i] = newJoint;
            }
        }
    }
    
    void CheckCombinedTarget(BaseBlock target)
    {
        InitCoordinate();
        CheckCombineBlock(target);
        if (!target.isCombined)
        {
            target.AddCombinedTarget(this);
            target.ActiveCombined();
            for (int i = 0; i < combinedTargets.Count(); i++)
            {
                target.AddCombinedTarget(combinedTargets[i]);
            }
            target.InitCombined();
        }
    }

    void CheckCombineBlock(BaseBlock target)
    {
        target.InitCoordinate();
        for (int i = 0; i < blockAbsoluteCoordinates.Count; i++)
        {
            CheckCombinedCoordinate(blockAbsoluteCoordinates[i], target);
        }
    }

    void CheckCombinedCoordinate(Coordinate root, BaseBlock target)
    {
        for (int x = -1; x <= 1 ; x++)
        {
            for (int y = -1; y <= 1 ; y++)
            {
                if (x == 0 && y == 0)
                    continue;
                if ((x == 0 && y != 0) || (x != 0 && y == 0))
                {
                    Coordinate checkCoordinate = new Coordinate(root.xIndex + x, root.yIndex + y);
                    if (target.CheckHasAbsoluteCoordinate(checkCoordinate))
                    {
                        CombinedBlock combinedBlock = SpawmCombinedBlock();
                        combinedBlock.Active(true);
                        combinedBlock.SetAngle(x, y);
                        int index = blockAbsoluteCoordinates.IndexOf(root);
                        combinedBlock.transform.localPosition = new Vector3(blockCoordinates[index].xIndex, 0, blockCoordinates[index].yIndex);
                        if (!realCombinedTargets.Contains(target))
                        {
                            realCombinedTargets.Add(target);
                        }
                    }
                }
            }
        }
    }
    
    CombinedBlock SpawmCombinedBlock()
    {
        if (combinedBlocks == null)
            combinedBlocks = new List<CombinedBlock>();
        for (var i = 0; i < combinedBlocks.Count; i++)
        {
            if (!combinedBlocks[i].gameObject.activeSelf)
            {
                combinedBlocks[i].gameObject.SetActive(true);
                return combinedBlocks[i];
            }
        }
        CombinedBlock combinedBlock = Instantiate(ColorGlobalConfig.Instance.combinedBlockPrefab, combinedBlockParent);
        combinedBlock.baseBlock = this;
        combinedBlock.gameObject.SetActive(true);
        combinedBlock.transform.localPosition = transform.localPosition;
        combinedBlocks.Add(combinedBlock);
        return combinedBlock;
    }
    
    void PickUpCombinedTargets(bool pick)
    {
        if (combinedTargets == null || combinedTargets.Count == 0)
            return;
        for (var i = 0; i < combinedTargets.Count; i++)
        {
            if (combinedTargets[i] != null && combinedTargets[i] != this)
            {
                combinedTargets[i].SetBGBlock(!pick);
                if(combinedTargets[i].blockOutline != null)
                    combinedTargets[i].blockOutline.enabled = pick;
                if (!pick)
                {
                    combinedTargets[i].rigidbody.velocity = Vector3.zero;
                    combinedTargets[i].SetBlockNewPos();
                }
            }
        }
    }

    public void CallReCheckCombined()
    {
        for (var i = 0; i < combinedTargets.Count; i++)
        {
            combinedTargets[i].ReCheckCombined();   
        }
    }
    
    public void ReCheckCombined()
    {
        if (!isCombined || combinedTargets == null || combinedTargets.Count == 0)
            return;
        for (int i = combinedTargets.Count - 1; i >= 0; i--)
        {
            if (!realCombinedTargets.Contains(combinedTargets[i]))
            {
                bool hasConection = false;
                for (var i1 = 0; i1 < realCombinedTargets.Count; i1++)
                {
                    if(realCombinedTargets[i1].realCombinedTargets.Contains(combinedTargets[i]))
                    {
                        hasConection = true;
                        break;
                    }
                }
                if (!hasConection)
                {
                    realCombinedTargets.Remove(combinedTargets[i]);
                    RemoveCombinedTarget(combinedTargets[i]);
                }
            }
            else
            {
                if (combinedTargets[i].isResolved)
                {
                    realCombinedTargets.Remove(combinedTargets[i]);
                    RemoveCombinedTarget(combinedTargets[i]);
                }
            }
            
        }

        InitCombined();
    }

    #endregion
    
    #endregion

    #region GamePlay

    public void OnBlockSelected(Vector3 clickPosition)
    {
        this.clickPosition = clickPosition;
        clickDelta = clickPosition - transform.position;
        blockModel.localPosition = Vector3.up * (blockModelY + 0.2f);
        mapConstructor.SetSelectedBlock(this);
        SetBGBlock(false);
        PickUpCombinedTargets(true);
    }
    
    public void OnBlockDeselected()
    {
        blockModel.localPosition = Vector3.up * blockModelY;
        SetBlockNewPos();
        mapConstructor.DeSelectBlock();
        rigidbody.velocity = Vector3.zero;
        SetBGBlock(true);
        PickUpCombinedTargets(false);
    }

    public void SetHighlighted(bool isHighlighted)
    {
        if (selectedMeshRenderer == null)
            return;
        if (isHighlighted && !this.isHighlighted)
        {
            if(blockOutline != null)
                blockOutline.enabled = false;
            selectedMeshRenderer.materials = lightMaterials;
        }
        else if(!isHighlighted && this.isHighlighted)
        {
            if(blockOutline != null)
                blockOutline.enabled = false;
            selectedMeshRenderer.materials = baseMaterials;
        }
        this.isHighlighted = isHighlighted;
        if(blockOutline != null)
            blockOutline.enabled = pickingUp;
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

    public virtual void FixedUpdate()
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
        if(mapConstructor == null)
            return;
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
            pickingUp = true;
            if(blockOutline != null)
                blockOutline.enabled = pickingUp;
        }
        else
        {
            rigidbody.isKinematic = true;
            rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            pickingUp = false;
            if(blockOutline != null)
                blockOutline.enabled = pickingUp;
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
        isResolved = true;
        CallReCheckCombined();
        mapConstructor.RemoveBlock(this);
        gameObject.SetActive(false);
        isResolved = true;
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