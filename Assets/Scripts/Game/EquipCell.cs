using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using TouhouSha.Koishi.Cards.Weapons;

public class EquipCell : MonoBehaviour
{
    #region Number

    private Card card;
    public Card Card
    {
        get
        {
            return this.card;
        }
        set
        {
            if (card == value) return;
            this.card = value;
            UpdateInfo();
            UpdateSelect();
        }
    }

    private bool isenterselecting = false;
    public bool IsEnterSelecting
    {
        get
        {
            return this.isenterselecting;
        }
        set
        {
            if (isenterselecting == value) return;
            this.isenterselecting = value;
            UpdateSelect();
        }
    }

    private bool canselect = false;
    public bool CanSelect
    {
        get
        {
            return this.canselect;
        }
        set
        {
            if (canselect == value) return;
            this.canselect = value;
            UpdateSelect();
        }
    }

    private bool isselected = false;
    public bool IsSelected
    {
        get
        {
            return this.isselected;
        }
        set
        {
            if (isselected == value) return;
            this.isselected = value;
            UpdateSelect();
        }
    }

    private float opacity = 1.0f;
    public float Opacity
    {
        get
        {
            return this.opacity;
        }
        set
        {
            if (opacity == value) return;
            this.opacity = value;
            foreach (Image image in gameObject.GetComponentsInChildren<Image>())
            {
                Color c = image.color;
                c.a = value;
                image.color = c;
            }
            foreach (Text text in gameObject.GetComponentsInChildren<Text>())
            {
                Color c = text.color;
                c.a = value;
                text.color = c;
            }
        }
    }

    private Color color = new Color(1.0f, 1.0f, 1.0f);
    public Color Color
    {
        get
        {
            return this.color;
        }
        set
        {
            if (color == value) return;
            this.color = value;
            foreach (Image image in gameObject.GetComponentsInChildren<Image>())
            {
                Color c = image.color;
                c.r = color.r;
                c.g = color.g;
                c.b = color.b;
                image.color = c;
            }
            foreach (Button button in gameObject.GetComponentsInChildren<Button>())
            {
                ColorBlock cb = button.colors;
                Color c = cb.normalColor;
                c.r = color.r;
                c.g = color.g;
                c.b = color.b;
                cb.normalColor = c;
                c = cb.highlightedColor;
                c.r = color.r;
                c.g = color.g;
                c.b = color.b;
                cb.highlightedColor = c;
                button.colors = cb;
            }
        }
    }

    private Image image;
    private Text colortext;
    private Text cardname;

    #endregion

    #region MonoBehavior

    void Awake()
    {
        image = gameObject.GetComponent<Image>();
        foreach (Text text in gameObject.GetComponentsInChildren<Text>())
        {
            switch (text.gameObject.name)
            {
                case "Color":
                    colortext = text; 
                    break;
                case "Name":
                    cardname = text;
                    break;
            }
        }
        UpdateInfo();
        UpdateSelect();
    }

    #endregion

    #region Method

    #region Text

    protected void SetColor(Text text, Card card)
    {
        if (card?.CardColor?.SeemAs(Enum_CardColor.Red) == true)
            text.color = new Color(1.0f, 0.75f, 0.75f);
        else
            text.color = new Color(1.0f, 1.0f, 1.0f);
    }

    protected void SetEmoji(Text text, Card card)
    {
        switch (card?.CardColor?.E)
        {
            case Enum_CardColor.Heart: text.text = "♥"; break;
            case Enum_CardColor.Diamond: text.text = "♦"; break;
            case Enum_CardColor.Spade: text.text = "♠"; break;
            case Enum_CardColor.Club: text.text = "♣"; break;
            default: text.text = ""; break;
        }
    }

    protected void SetPoint(Text text, Card card)
    {
        if (card == null)
        {
            text.text = "";
            return;
        }
        switch (card.CardPoint)
        {
            case 1: text.text = "A"; break;
            case 11: text.text = "J"; break;
            case 12: text.text = "Q"; break;
            case 13: text.text = "K"; break;
            default: text.text = card.CardPoint.ToString(); break;
        }
    }

    protected void SetName(Text text, Card card)
    {
        string point = "";
        if (card == null)
        {
            text.text = "";
            return;
        }
        switch (card.CardPoint)
        {
            case 1: point = "A"; break;
            case 11: point = "J"; break;
            case 12: point = "Q"; break;
            case 13: point = "K"; break;
            default: point = card.CardPoint.ToString(); break;
        }
        if (card is SelfWeapon)
            text.text = String.Format("{0} {1}{2}", point, card.Name, ((SelfWeapon)card).GetWeaponRange());
        else
            text.text = String.Format("{0} {1}", point, card.Name);
    }

    #endregion

    #region Update

    protected void UpdateInfo()
    {
        if (colortext == null) return;
        if (cardname == null) return;
        SetColor(colortext, card);
        SetColor(cardname, card);
        SetEmoji(colortext, card);
        SetName(cardname, card);
    }

    protected void UpdateSelect()
    {
        #region 选择状态
        if (IsEnterSelecting)
        {
            if (IsSelected)
            {
                if (image != null)
                    image.sprite = ImageHelper.CreateSprite("Borders/EquipsSelected");
                Opacity = 1.0f;
                Color = new Color(0.5f, 1.0f, 0.5f);
            }
            else if (CanSelect)
            {
                if (image != null)
                    image.sprite = ImageHelper.CreateSprite("Borders/Equips");
                Opacity = 1.0f;
                Color = new Color(1.0f, 1.0f, 1.0f);
            }
            else
            {
                if (image != null)
                    image.sprite = ImageHelper.CreateSprite("Borders/Equips");
                Opacity = 0.75f;
                Color = new Color(0.5f, 0.5f, 0.5f);
            }
        }
        #endregion
        #region 默认状态
        else
        {
            if (image != null)
                image.sprite = ImageHelper.CreateSprite("Borders/Equips");
            Opacity = 1.0f;
            Color = new Color(1.0f, 1.0f, 1.0f);
        }
        #endregion
    }

    #endregion

    #endregion
}