using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class ExploseBlock : BaseBlock
{
    public override void Start()
    {
        for (var i = 0; i < colliders.Count; i++)
        {
            colliders[i].enabled = false;
        }
        rigidbody.velocity = Vector3.zero;
        rigidbody.constraints = RigidbodyConstraints.FreezeAll;
    }

    public void Init(ColorCode colorCode, float delay)
    {
        this.colorCode = colorCode;
        OnChangeColorCode();
        InitMaterial();
        outline = null;
        selectedMeshRenderer.materials = lightMaterials;
        Explose(delay);
    }
    
    public override void FixedUpdate()
    {
    }
    [Button]
    public void Explose(float delay)
    {
        transform.localScale = Vector3.one;
        transform.DOScale(0, 0.15f).SetEase(Ease.InBack).SetDelay(delay * 0.1f)
            .OnComplete(Deactivate);
    }

    void Deactivate()
    {
        Destroy(gameObject);
    }
}
