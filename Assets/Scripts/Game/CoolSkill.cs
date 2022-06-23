using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using TouhouSha.Core.UIs.Events;

public class CoolSkill : MonoBehaviour
{
    private UISkillCoolAnimEvent ev;
    public UISkillCoolAnimEvent Event
    {
        get
        {
            return this.ev;
        }
        set
        {
            this.ev = value;
            if (ev == null) return;

            RectTransform rt = gameObject.GetComponent<RectTransform>();
            RectTransform rt_face = Face.gameObject.GetComponent<RectTransform>();
            Charactor char0 = ev.Source.Charactors.FirstOrDefault();
            if (char0 != null)
            {
                CharactorImageContext ctx = App.GetImageContext(char0);
                Sprite sprite = ImageHelper.CreateSprite(char0);
                float imageheight = sprite.rect.height / sprite.rect.width * rt.rect.width;
                rt_face.localPosition = new Vector3(0, imageheight * (ctx.FacePoint.y - 0.5f));
            }
            
            Comment0.text = ev.Comment0;
            Comment1.text = ev.Comment1;
        }
    }

    public Image Flow0;
    public Image Flow1;
    public Image Face;
    public Text Comment0;
    public Text Comment1;
    public RectTransform FaceMask;
    public RectTransform CommentMask;
    public float TotalTime = 0.0f;
    public float MaxTime = 4.0f;
   
    void Update()
    {
        if (ev == null) return;
        if (TotalTime > MaxTime + 2.0f) return;
        RectTransform rt = gameObject.GetComponent<RectTransform>();
        #region 移动速度线
        RectTransform flow0_rt = Flow0.gameObject.GetComponent<RectTransform>();
        RectTransform flow1_rt = Flow1.gameObject.GetComponent<RectTransform>();
        float flowwidth = Flow0.sprite.rect.width / Flow0.sprite.rect.height * rt.rect.height;
        flow0_rt.localPosition = new Vector3((TotalTime % 1 - 0.5f) * 2 * flowwidth, 0);
        flow1_rt.localPosition = new Vector3(((TotalTime + 0.5f) % 1 - 0.5f) * 2 * flowwidth, 0);
        #endregion
        #region 黑幕/速度线透明度
        float opacity = 0.0f;
        Image screen = gameObject.GetComponent<Image>();
        if (TotalTime < 0.25f)
            opacity = TotalTime * 4;
        else if (TotalTime >= MaxTime - 0.25f)
            opacity = Math.Max(0, (MaxTime - TotalTime) * 4);
        else
            opacity = 1.0f;
        Color color = screen.color;
        color.a = opacity;
        screen.color = color;
        color = Flow0.color;
        color.a = opacity;
        Flow0.color = color;
        color = Flow1.color;
        color.a = opacity;
        Flow1.color = color;
        color = Comment1.color;
        color.a = opacity;
        Comment1.color = color;
        color = Face.color;
        color.a = opacity;
        Face.color = color;
        #endregion
        #region 文本0透明度
        if (TotalTime < 0.5f)
            opacity = 0;
        else if (TotalTime < 1.0f)
            opacity = (TotalTime - 0.5f) * 2;
        else if (TotalTime >= MaxTime - 0.25f)
            opacity = Math.Max(0, (MaxTime - TotalTime) * 4);
        else
            opacity = 1.0f;
        color = Comment0.color;
        color.a = opacity;
        Comment0.color = color;
        #endregion
        #region 文本1蒙版
        float maskwidth = 0.0f;
        if (TotalTime < 0.5f)
            maskwidth = 0;
        else if (TotalTime < 1.5f)
            maskwidth = rt.rect.width * (TotalTime - 0.5f);
        else
            maskwidth = rt.rect.width;
        Vector2 size = CommentMask.sizeDelta;
        size.x = maskwidth;
        CommentMask.sizeDelta = size;
        #endregion
        #region 脸部蒙版
        float maskheight = 0.0f;
        if (TotalTime < 0.5f)
            maskheight = 0;
        else if (TotalTime < 1.5f)
            maskheight = 600 * (TotalTime - 0.5f);
        else
            maskheight = 600;
        size = FaceMask.sizeDelta;
        size.y = maskheight;
        FaceMask.sizeDelta = size;
        #endregion
        TotalTime += Time.deltaTime;

    }

}
