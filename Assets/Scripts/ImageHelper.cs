using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TouhouSha.Core;

public static class ImageHelper
{
    static public Sprite CreateSprite(string path)
    {
        Texture2D tex2d = Resources.Load<Texture2D>(path);
        float w0 = tex2d.width;
        float h0 = tex2d.height;
        Rect rect0 = new Rect(0, 0, w0, h0);
        return Sprite.Create(tex2d, rect0, new Vector2(0.5f, 0.5f));
    }

    static public Sprite CreateSprite(string path, Rect rect)
    {
        Texture2D tex2d = Resources.Load<Texture2D>(path);
        float w0 = tex2d.width;
        float h0 = tex2d.height;
        float w1 = rect.width;
        float h1 = rect.height;
        Rect rect0 = new Rect(0, 0, w0, h0);
        if (w0 / h0 < w1 / h1)
        {
            float hd = h0 - w0 * h1 / w1;
            rect0 = new Rect(0, hd / 2, rect0.width, rect0.height - hd);
        }
        else if (w0 / h1 > w1 / h1)
        {
            float wd = w0 - h0 * w1 / h1;
            rect0 = new Rect(wd / 2, 0, rect0.width - wd, rect0.height);
        }
        return Sprite.Create(tex2d, rect0, new Vector2(0.5f, 0.5f));
    }

    static public Sprite CreateSprite(Charactor char0)
    {
        string imagename = char0.GetInfo().ImageName;
        Texture2D tex2d = Resources.Load<Texture2D>(String.Format("Charactors/{0}/{0}", imagename));
        CharactorImageContext ctx = App.GetImageContext(char0);
        float w0 = tex2d.width;
        float h0 = tex2d.height;
        float x0 = w0 * ctx.Rect.x;
        float y0 = h0 * (1 - ctx.Rect.y - ctx.Rect.height);
        w0 *= ctx.Rect.width;
        h0 *= ctx.Rect.height;
        Rect rect0 = new Rect(x0, y0, w0, h0);
        return Sprite.Create(tex2d, rect0, new Vector2(0.5f, 0.5f));
    }

    static public Sprite CreateSprite(Charactor char0, Rect rect)
    {
        string imagename = char0.GetInfo().ImageName;
        Texture2D tex2d = Resources.Load<Texture2D>(String.Format("Charactors/{0}/{0}", imagename));
        //Debug.Log(String.Format("path={0} tex2d={1} rect={2}", String.Format("Charactors/{0}/{0}", imagename), tex2d, rect));
        CharactorImageContext ctx = App.GetImageContext(char0);
        float w0 = tex2d.width;
        float h0 = tex2d.height;
        float x0 = w0 * ctx.Rect.x;
        float y0 = h0 * (1 - ctx.Rect.y - ctx.Rect.height);
        float w1 = rect.width;
        float h1 = rect.height;
        w0 *= ctx.Rect.width;
        h0 *= ctx.Rect.height;
        Rect rect0 = new Rect(x0, y0, w0, h0);
        if (w0 / h0 < w1 / h1)
        {
            float hd = h0 - w0 * h1 / w1;
            rect0 = new Rect(x0, y0 + hd / 2, rect0.width, rect0.height - hd);
        }
        else if (w0 / h1 > w1 / h1)
        {
            float wd = w0 - h0 * w1 / h1;
            rect0 = new Rect(x0 + wd / 2, y0, rect0.width - wd, rect0.height);
        }
        return Sprite.Create(tex2d, rect0, new Vector2(0.5f, 0.5f));
    }

    static public Sprite CreateSprite(Card card)
    {
        string imagename = card.GetInfo().ImageName;
        return CreateSprite(String.Format("Cards/{0}/{0}", imagename));
    }

    static public Sprite CreateDelayIcon(Card card)
    {
        string imagename = card.GetInfo().DelayIconName;
        return CreateSprite(String.Format("Delays/{0}/{0}", imagename));
    }
}
