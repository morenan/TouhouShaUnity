using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class DigitItem : MonoBehaviour
{
    private DigitColor color = DigitColor.Black;
    public DigitColor Color
    {
        get { return this.color; }
        set { this.color = value; UpdateSprite(); }
    }

    private int digit = 0;
    public int Digit
    {
        get { return this.digit; }
        set { this.digit = value; UpdateSprite(); }
    }

    private Image image;

    void Awake()
    {
        image = gameObject.GetComponent<Image>();
        UpdateSprite();
    }

    public void UpdateSprite()
    {
        string s0 = color == DigitColor.Black ? "" : color.ToString().Substring(0, 1).ToLower();
        string s1 = digit.ToString().Substring(0, 1);
        if (image != null)
        {
            RectTransform rt = image.gameObject.GetComponent<RectTransform>();
            image.sprite = ImageHelper.CreateSprite(String.Format("Digits/{0}{1}", s0, s1), rt.rect);
        }
    }

}

public enum DigitColor
{
    Black,
    Red,
    Yellow,
    Green,
}
