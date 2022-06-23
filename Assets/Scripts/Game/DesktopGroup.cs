using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using TouhouSha.Core.UIs;

public class DesktopGroup : MonoBehaviour
{
    #region Number

    public DesktopCardBoardCore BoardCore;

    private DesktopCardBoardZone core;
    public DesktopCardBoardZone Core
    {
        get
        {
            return this.core;
        }
        set
        {
            this.core = value;
            UpdateCards();
        }
    }

    private List<Card> cards = new List<Card>();
    public List<Card> Cards
    {
        get
        {
            return this.cards;
        }
    }

    private List<CardBe> cardviews = new List<CardBe>();
    public List<CardBe> CardViews
    {
        get
        {
            return this.cardviews;
        }
    }

    new private Text name;
    private GameObject grid;
    private int removeindex = -1;
    private int insertindex = -1;

    #endregion

    #region MonoBehavior

    void Awake()
    {
        for (int i0 = 0; i0 < gameObject.transform.childCount; i0++)
        {
            Transform t0 = gameObject.transform.GetChild(i0);
            GameObject g0 = t0.gameObject;
            switch (g0.name)
            {
                case "Name":
                    name = g0.GetComponent<Text>();
                    break;
                case "CardList":
                    grid = g0;
                    break;
            }
        }
        UpdateCards();
    }

    void Update()
    {
        
    }

    #endregion

    #region Method

    public void UpdateCards()
    {
        if (grid == null) return;
        GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
        RectTransform gb_rt = gb.gameObject.GetComponent<RectTransform>();
        RectTransform rt = gameObject.GetComponent<RectTransform>();
        Vector3 p = grid.transform.position;
        float maxwidth = gb_rt.rect.width - 560;
        float totalwidth = 0;
        float totalheight = 0;
        int columns = 0;
        name.text = core?.KeyName ?? "";
        cards.Clear();
        if (core != null) cards.AddRange(core.Cards);
        while (cardviews.Count() < cards.Count())
        {
            GameObject go0 = Resources.Load<GameObject>("Card");
            GameObject go1 = GameObject.Instantiate(go0, grid.transform);
            CardBe cardview = go1.GetComponent<CardBe>();
            cardviews.Add(cardview);
        }
        for (int i = 0; i < cards.Count(); i++)
        {
            CardBe cardview = cardviews[i];
            RectTransform cardview_rt = cardview.gameObject.GetComponent<RectTransform>();
            if ((core.Flag & Enum_DesktopZoneFlag.CanZip) == Enum_DesktopZoneFlag.None
             && totalwidth + cardview_rt.rect.width > maxwidth) break;
            totalwidth += cardview_rt.rect.width;
            columns++;
        }
        for (int i = 0; i < cards.Count(); i++)
        {
            CardBe cardview = cardviews[i];
            RectTransform cardview_rt = cardview.gameObject.GetComponent<RectTransform>();
            int xi = i % columns;
            int yi = i / columns;
            App.Show(cardview);
            cardview.Core = cards[i];
            cardview.IsLocalPosition = true;
            cardview.Position = new Vector3((xi + 0.5f) * cardview_rt.rect.width, 0);
            cardview.IsFaceDown = ((core.Flag & Enum_DesktopZoneFlag.FaceDown) != Enum_DesktopZoneFlag.None);
            cardview.CanDrag = ((core.Flag & Enum_DesktopZoneFlag.CanSort) != Enum_DesktopZoneFlag.None);
            if (xi == 0) totalheight += cardview_rt.rect.height;
        }
        for (int i = cards.Count(); i < cardviews.Count(); i++)
        {
            CardBe cardview = cardviews[i];
            App.Hide(cardview);
        }
        rt.sizeDelta = new Vector2(
            totalwidth + 64,
            totalheight + 80);
    }

    public void Remove(CardBe cardview)
    {
        Card card = cardview.Core;
        removeindex = cards.IndexOf(card);
        if (removeindex < 0) return;
        cards.RemoveAt(removeindex);
        cardviews.RemoveAt(removeindex);
        core.Cards.RemoveAt(removeindex);
        if ((core.Flag & Enum_DesktopZoneFlag.AsSelected) != Enum_DesktopZoneFlag.None)
            BoardCore.SelectedCards.RemoveAt(removeindex);
        UpdateAnimation();
    }

    public bool InsertPrepare(CardBe drop)
    {
        int oldinsertindex = insertindex;
        int newinsertindex = -1;
        Vector3 p = grid.transform.position;
        RectTransform drop_rt = drop.gameObject.GetComponent<RectTransform>();
        float ty = p.y - 0.5f * drop_rt.rect.height;
        if (Math.Abs(drop_rt.position.y - ty) < drop_rt.rect.height / 2)
        {
            newinsertindex = (int)((drop_rt.position.x - p.x) / drop_rt.rect.width);
            if (newinsertindex < 0) newinsertindex = -1;
            if (newinsertindex > cards.Count()) newinsertindex = cards.Count();
        }
        if (newinsertindex >= 0)
        {
            // 当前区为选择区。
            if ((core.Flag & Enum_DesktopZoneFlag.AsSelected) != Enum_DesktopZoneFlag.None)
            {
                // 检验可以选择。
                if (!BoardCore.CardFilter.CanSelect(App.World.GetContext(), cards, drop.Core))
                    newinsertindex = -1;
            }
            // 当前区为自由拖动区。
            else
            {
                // 当前区的Getter不允许插入时。
                if (core.CardGetter?.CanGet(core, drop.Core, newinsertindex) != true)
                    newinsertindex = -1;
            }
        }
        insertindex = newinsertindex;
        if (oldinsertindex != newinsertindex)
            UpdateAnimation();
        return newinsertindex >= 0;
    }

    public void Insert(CardBe drop)
    {
        if (insertindex < 0) return;
        cards.Insert(insertindex, drop.Core);
        cardviews.Insert(insertindex, drop);
        core.Cards.Insert(insertindex, drop.Core);
        if ((core.Flag & Enum_DesktopZoneFlag.AsSelected) != Enum_DesktopZoneFlag.None)
            BoardCore.SelectedCards.Insert(insertindex, drop.Core);
        drop.gameObject.transform.parent = grid.transform;
        App.Show(drop);
        UpdateAnimation();
    }

    public void Restore(CardBe cardview)
    {
        if (removeindex < 0) return;
        cards.Insert(removeindex, cardview.Core);
        cardviews.Insert(removeindex, cardview);
        core.Cards.Insert(removeindex, cardview.Core);
        if ((core.Flag & Enum_DesktopZoneFlag.AsSelected) != Enum_DesktopZoneFlag.None)
            BoardCore.SelectedCards.Insert(removeindex, cardview.Core);
        cardview.gameObject.transform.parent = grid.transform;
        App.Show(cardview);
        UpdateAnimation();

    }

    public void UpdateAnimation()
    {
        int index = 0;
        Vector3 p = new Vector3(0, 0);
        RectTransform rt = gameObject.GetComponent<RectTransform>();
        float maxheight = 0;
        foreach (CardBe cardview in cardviews)
        {
            if (cardview.Core == null) continue;
            RectTransform cardview_rt = cardview.gameObject.GetComponent<RectTransform>();
            if (index == insertindex)
            {
                index++;
                p.x += cardview_rt.rect.width;
            }
            cardview.IsLocalPosition = true;
            cardview.Move(new Vector3(
                p.x + 0.5f * cardview_rt.rect.width,
                p.y));
            index++;
            p.x += cardview_rt.rect.width;
            maxheight = Math.Max(maxheight, cardview_rt.rect.height);
        }
        rt.sizeDelta = new Vector2(
            p.x + 64, 
            maxheight + 80);
    }

    #endregion
}
