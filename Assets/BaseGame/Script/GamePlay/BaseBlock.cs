using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class BaseBlock : MonoBehaviour
{
    [OnValueChanged("OnChangeColorCode")]
    public ColorCode colorCode;
    public MapConstructor mapConstructor;
    public Coordinate coordinate;
    public List<Coordinate> blockCoordinates;
    public IndexShow indexShowPrefab;
    public List<IndexShow> indexShows;
    public Transform blockModel;
    public Vector3 clickPosition;
    public Vector3 clickDelta;
    public Rigidbody rigidbody;
    public Vector3 newVelocity;
    Queue<Vector3> velocityQueue = new Queue<Vector3>();
    Vector3 lastClickPos = Vector3.zero;
    public MeshRenderer meshRenderer;

    private void Start()
    {
        InitFirstValue();
    }

    void InitFirstValue()
    {
        transform.position = mapConstructor.GetNearestCordinate(transform.position, out coordinate);
        rigidbody.velocity = Vector3.zero;
        rigidbody.constraints = RigidbodyConstraints.FreezeAll;
    }
    public void OnChangeColorCode()
    {
        meshRenderer.material = ColorGlobalConfig.Instance.GetBlockMaterial(colorCode);
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

    #endregion

    #region Init

    public void InitColor(ColorCode relativeColor)
    {
        if (meshRenderer != null)
        {
            meshRenderer.material = ColorGlobalConfig.Instance.GetBlockMaterial(relativeColor);
        }
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
        transform.position = mapConstructor.GetNearestCordinate(transform.position, out coordinate);
        mapConstructor.DeSelectBlock();
        rigidbody.velocity = Vector3.zero;
        SetBGBlock(true);
    }
    
    public void SetBlockPosition(Vector3 position)
    {
        if(position.y != 0)
            return;
        lastClickPos = position;
        Vector3 distance = position - clickDelta - rigidbody.position;
        distance = Vector3.ClampMagnitude(distance, 1);
        newVelocity = (distance) * 20;
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

    #endregion

    
}
