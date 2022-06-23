using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using TouhouSha.Core.UIs;

public class ZoneTooltip : MonoBehaviour
{
    #region Member

    private PlayerBe player;
    public PlayerBe Player
    {
        get
        {
            return this.player;
        }
        set
        {
            this.player = value;
        }
    }

    private Zone zone;
    public Zone Zone
    {
        get
        {
            return this.zone;
        }
        set
        {
            this.zone = value;
            if (zone != null) App.Show(this); else App.Hide(this);
            UpdateText();
        }
    }

    public Text Text;
    public Button Close;

    #endregion

    #region Mono

    void Awake()
    {
        Close.onClick.AddListener(OnClose);    
    }

    void Update()
    {
        if (Player != null)
        {
            RectTransform rt_player = Player.transform as RectTransform;
            RectTransform rt_canvas = gameObject.transform.parent as RectTransform;
            RectTransform rt_this = gameObject.GetComponent<RectTransform>();
            float prefer_y = rt_player.position.y - rt_player.rect.height / 2;
            prefer_y = Math.Min(prefer_y, rt_canvas.rect.height - rt_this.rect.height);
            if (rt_player.position.x - rt_player.rect.width / 2 >= rt_canvas.rect.width / 2)
            {
                rt_this.position = new Vector3(
                    rt_player.position.x - rt_player.rect.width / 2 - rt_this.rect.width,
                    prefer_y);
            }
            else
            {
                rt_this.position = new Vector3(
                    rt_player.position.x + rt_player.rect.width / 2,
                    prefer_y);
            }
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
        if (zone == null) return;
        StringBuilder sb = new StringBuilder();
        foreach (Card card in zone.Cards)
        {
            CardInfo ci = card.GetInfo();
            if (card.CardColor?.SeemAs(Enum_CardColor.Red) == true)
                sb.Append("<color=#FF0000>");
            sb.Append(GetEmoji(card));
            sb.Append(GetPoint(card));
            sb.Append(" ");
            sb.Append(ci.Name);
            if (card.CardColor?.SeemAs(Enum_CardColor.Red) == true)
                sb.Append("</color>");
            sb.Append("\n");
        }
        Text.text = sb.ToString();
    }

    #endregion

    #region Event Handler

    private void OnClose()
    {
        Zone = null;
        Player = null;
    }

    #endregion 
}