using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class HoverBlock : BaseBlock
{
    public Image sprite;
    public RectTransform spriteRectTransform;
    public List<ParticleSystem> particleSystems;
    public override void Start()
    {
        for (var i = 0; i < colliders.Count; i++)
        {
            colliders[i].enabled = false;
        }
        rigidbody.velocity = Vector3.zero;
        rigidbody.constraints = RigidbodyConstraints.FreezeAll;
    }
    
    [Button]
    public void Init(ColorCode colorCode, float size)
    {
        this.colorCode = colorCode;
        sprite.color = ColorGlobalConfig.Instance.GetOutlineColor(colorCode);
        OnChangeColorCode();
        for (var i = 0; i < particleSystems.Count; i++)
        {
            var particleSystem = particleSystems[i];
            var emission = particleSystem.emission;
            emission.rateOverTime = size * 8;
            var shape = particleSystem.shape;
            shape.scale = new Vector3(size, 0.01f, 1f);
            var main = particleSystem.main;
            main.startColor = sprite.color;
        }
        spriteRectTransform.sizeDelta = new Vector2((size + 1) * 256f, spriteRectTransform.sizeDelta.y);
    }

    public override void OnChangeColorCode()
    {
        
    }
    
    public override void OnChangeShape()
    {
        
    }
}
