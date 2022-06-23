using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class CenterResize : MonoBehaviour
{
    public float HorizontalMargin = 280;
    public float VerticalMargin = 280;
    public float Width = 300;
    public float Height = 300;
    
    void Update()
    {
        RectTransform r0 = gameObject.transform.parent as RectTransform;
        RectTransform r1 = gameObject.GetComponent<RectTransform>();
        if (r0 == null) return;
        if (r1 == null) return;
        r1.sizeDelta = new Vector2(
            Math.Min(r0.rect.width - HorizontalMargin * 2, Width), 
            Math.Min(r0.rect.height - VerticalMargin * 2, Height));
        //float w0 = (r0.rect.width - Width) / 2;
        //float h0 = (r0.rect.height - Height) / 2;
        //r1.anchorMin = new Vector2(w0, h0);
        //r1.anchorMax = new Vector2(r0.rect.width - w0, r0.rect.height - h0);
    }
}
