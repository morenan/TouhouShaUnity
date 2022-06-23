using System;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TouhouSha.Core;
using UnityEngine;

public class App
{
    static public string NickName = "Knight of Koishi";

    static public World World;
    static public bool IsOnlineGame = false;
    static public DataPool DefaultDataPool;


    static public Rect ParseRect(string text)
    {
        Rect rect = new Rect();
        float x = 0;
        float y = 0;
        float w = 1;
        float h = 1;
        text = text.Trim();
        if (text.StartsWith("{") && text.EndsWith("}"))
            text = text.Substring(1, text.Length - 2);
        string[] items = text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < items.Length; i++)
        {
            switch (items.Length - i)
            {
                case 4: float.TryParse(items[i].Trim(), out x); break;
                case 3: float.TryParse(items[i].Trim(), out y); break;
                case 2: float.TryParse(items[i].Trim(), out w); break;
                case 1: float.TryParse(items[i].Trim(), out h); break;
            }
        }
        rect.x = x;
        rect.y = y;
        rect.width = w;
        rect.height = h;
        return rect;
    }

    static public Vector2 ParseVector2(string text)
    {
        Vector2 vec = new Vector2();
        float x = 0;
        float y = 0;
        text = text.Trim();
        if (text.StartsWith("{") && text.EndsWith("}"))
            text = text.Substring(1, text.Length - 2);
        string[] items = text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < items.Length; i++)
        {
            switch (items.Length - i)
            {
                case 2: float.TryParse(items[i].Trim(), out x); break;
                case 1: float.TryParse(items[i].Trim(), out y); break;
            }
        }
        vec.x = x;
        vec.y = y;
        return vec;
    }

    static public CharactorImageContext GetImageContext(Charactor char0)
    {
        string imagename = char0.GetInfo().ImageName;
        TextAsset text = Resources.Load<TextAsset>(String.Format("Charactors/{0}/{0}", imagename));
        string[] lines = text.text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        CharactorImageContext ctx = new CharactorImageContext();
        foreach (string line in lines)
        {
            int i1 = line.IndexOf('=');
            if (i1 <= 0) continue;
            string key = line.Substring(0, i1).Trim();
            string value = line.Substring(i1 + 1).Trim();
            switch (key.ToUpper())
            {
                case "AUTHOR":
                    ctx.Author = value;
                    break;
                case "PIXIV ID":
                    ctx.PixivID = value;
                    break;
                case "RECT":
                    ctx.Rect = ParseRect(value);
                    break;
                case "FACEPOINT":
                    ctx.FacePoint = ParseVector2(value);
                    break;
            }
        }
        return ctx;
    }

    static public void Hide(GameObject go, bool setactive = true)
    {
        if (setactive)
            go.SetActive(false);
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0.0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }

    static public void Hide(MonoBehaviour mb, bool setactive = true)
    {
        Hide(mb.gameObject, setactive);
    }

    static public void Show(GameObject go, bool setactive = true)
    {
        if (setactive)
            go.SetActive(true);
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1.0f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }

    static public void Show(MonoBehaviour mb, bool setactive = true)
    {
        Show(mb.gameObject, setactive);
    }

    static public bool IsVisible(GameObject go)
    {
        if (!go.activeInHierarchy) return false;
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg != null) return cg.alpha > 0.0f;
        return true;
    }

    static public bool IsVisible(MonoBehaviour mb)
    {
        return IsVisible(mb.gameObject);
    }
}

public struct CharactorImageContext
{
    public string Author;
    public string PixivID;
    public Rect Rect;
    public Vector2 FacePoint;
}

