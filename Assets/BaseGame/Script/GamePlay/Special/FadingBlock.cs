using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FadingBlock : BaseBlock
{
    public Image sprite;
    public CanvasGroup spriteCG;
    public RectTransform spriteRectTransform;
    public Sequence sequence;
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
        RunEffect(size);
    }
    
    void RunEffect(float size)
    {
        if(sequence != null)
            sequence.Kill();
        sequence = DOTween.Sequence();
        spriteCG.alpha = 1;
        Vector2 startSize = new Vector2(0, spriteRectTransform.sizeDelta.y);
        Vector2 endSize = new Vector2((size) * 256f, spriteRectTransform.sizeDelta.y);
        spriteRectTransform.sizeDelta = startSize;
        sequence.Append(DOVirtual.Vector2(startSize, endSize, ((size + 1) / 2) * 0.15f + 0.15f, value =>
        {
            spriteRectTransform.sizeDelta = value;
        }).SetDelay(0.15f));
        sequence.Append(spriteCG.DOFade(0, 0.25f).SetDelay(0.1f));
        sequence.OnComplete(DeativeBlock);
    }

    void DeativeBlock()
    {
        gameObject.SetActive(false);
    }

    public override void OnChangeColorCode()
    {
        
    }
    
    public override void OnChangeShape()
    {
        
    }
}
