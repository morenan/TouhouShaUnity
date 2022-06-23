using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using TouhouSha.Core.UIs;

public class CardTooltip : MonoBehaviour
{
    #region Member

    private CardBe card;
    public CardBe Card
    {
        get
        {
            return this.card;
        }
        set
        {
            this.card = value;
            if (card != null) App.Show(this); else App.Hide(this);
            UpdateText();
        }
    }

    public Text Text;

    #endregion

    #region Mono

    void Update()
    {
        if (Card == null) return;
        RectTransform rt_card = Card.transform as RectTransform;
        RectTransform rt_canvas = gameObject.transform.parent as RectTransform;
        RectTransform rt_this = gameObject.GetComponent<RectTransform>();
        float prefer_y = rt_card.position.y - rt_card.rect.height / 2;
        prefer_y = Math.Min(prefer_y, rt_canvas.rect.height - rt_this.rect.height);
        if (rt_card.position.x - rt_card.rect.width / 2 >= rt_canvas.rect.width / 2)
        {
            rt_this.position = new Vector3(
                rt_card.position.x - rt_card.rect.width / 2 - rt_this.rect.width,
                prefer_y);
        }
        else
        {
            rt_this.position = new Vector3(
                rt_card.position.x + rt_card.rect.width / 2,
                prefer_y);
        }
    }

    #endregion

    #region Method

    protected string GetEmoji(Card card)
    {
        switch (card.CardColor?.E)
        {
            case Enum_CardColor.Heart: return "♥"; 
            case Enum_CardColor.Diamond: return "♦"; 
            case Enum_CardColor.Spade: return "♠"; 
            case Enum_CardColor.Club: return "♣"; 
        }
        return "";
    }

    protected string GetPoint(Card card)
    {
        switch (card.CardPoint)
        {
            case 1: return "A"; 
            case 11: return "J";
            case 12: return "Q"; 
            case 13: return "K"; 
            default: return card.CardPoint.ToString(); 
        }
    }
    public void UpdateText()
    {
        if (this.card == null) return;
        Card card = this.card.Core;
        if (card == null) return;
        CardInfo ci = card.GetInfo();
        if (ci == null) { Text.text = ""; return; }
        StringBuilder sb = new StringBuilder();
        if (card.CardColor?.SeemAs(Enum_CardColor.Red) == true)
            sb.Append("<color=#FF0000>");
        sb.Append(GetEmoji(card));
        sb.Append(GetPoint(card));
        sb.Append(" ");
        sb.Append(ci.Name);
        if (card.CardColor?.SeemAs(Enum_CardColor.Red) == true)
            sb.Append("</color>");
        sb.Append("\n");
        sb.Append(ci.Description);
        Text.text = sb.ToString();
    }

    #endregion
}
