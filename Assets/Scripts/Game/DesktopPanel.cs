using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using TouhouSha.Core.UIs;
using TouhouSha.Core.UIs.Events;
using TouhouSha.Core.AIs;

public class DesktopPanel : MonoBehaviour
{
    #region Number

    private DesktopCardBoardCore core;
    public DesktopCardBoardCore Core
    {
        get
        {
            return this.core;
        }
        set
        {
            this.core = value;
            if (core == null)
            {
                for (int i = 0; i < groups.Count(); i++)
                {
                    DesktopGroup group = groups[i];
                    group.BoardCore = null;
                    group.Core = null;
                    App.Hide(group);
                }
                EnterOrLeaveTimeout();
                return;
            }

            float maxwidth = 0;
            float totalheight = 0;
            CenterResize resize = gameObject.GetComponent<CenterResize>();

            message.text = core.Message;
            if (((core.Flag & Enum_DesktopCardBoardFlag.CannotNo) != Enum_DesktopCardBoardFlag.None))
                App.Hide(no);
            else
                App.Show(no);
            if (((core.Flag & Enum_DesktopCardBoardFlag.SelectCardAndYes) != Enum_DesktopCardBoardFlag.None))
                App.Hide(yes);
            else
                App.Show(yes);

            while (groups.Count() < core.Zones.Count())
            {
                GameObject go0 = groups[0].gameObject;
                GameObject go1 = GameObject.Instantiate(go0, Grid);
                DesktopGroup group = go1.GetComponent<DesktopGroup>();
                groups.Add(group);
            }
            for (int i = 0; i < core.Zones.Count(); i++)
            {
                DesktopGroup group = groups[i];
                RectTransform group_rt = group.gameObject.GetComponent<RectTransform>();
                App.Show(group);
                group.BoardCore = Core;
                group.Core = core.Zones[i];
                //group.UpdateCards();
                group_rt.localPosition = new Vector3(0, -totalheight);
                maxwidth = Math.Max(maxwidth, group_rt.rect.width);
                totalheight += group_rt.rect.height;
            }
            for (int i = core.Zones.Count(); i < groups.Count(); i++)
            {
                DesktopGroup group = groups[i];
                group.BoardCore = null;
                group.Core = null;
                App.Hide(group);
            }
            RectTransform grid_rt = Grid.GetComponent<RectTransform>();
            if (grid_rt != null)
                grid_rt.sizeDelta = new Vector2(
                    maxwidth, totalheight);
            if (resize != null)
            {
                resize.Width = maxwidth + 80;
                resize.Height = totalheight + 120;
            }
            card2removes.Clear();
            EnterOrLeaveTimeout();
            UpdateCardsSelectable();
        }
    }

    private bool iscontrolling;
    public bool IsControlling
    {
        get
        {
            return this.iscontrolling;
        }
        set
        {
            if (iscontrolling == value) return;
            this.iscontrolling = value;
            EnterOrLeaveTimeout();
            UpdateCardsSelectable();
        }
    }

    public RectTransform Grid;

    private List<DesktopGroup> groups = new List<DesktopGroup>();
    private List<Card> card2removes = new List<Card>();
    private Text message;
    private Scrollbar timeout;
    private ScrollRect scrollrect;
    private Button yes;
    private Button no;
    private bool iscountingtime;
    private float timemax = 100;
    private float timeremain = 100;
    private DesktopGroup removegroup = null;
    private DesktopGroup insertgroup = null;

    #endregion

    #region MonoBehavior

    void Awake()
    {
        foreach (DesktopGroup group in gameObject.GetComponentsInChildren<DesktopGroup>())
        {
            if (groups.Contains(group)) continue;
            groups.Add(group);
            App.Hide(group);
        }
        foreach (Scrollbar scrollbar in gameObject.GetComponentsInChildren<Scrollbar>())
        {
            switch (scrollbar.gameObject.name)
            {
                case "Timeout":
                    timeout = scrollbar;
                    message = timeout.gameObject.GetComponentInChildren<Text>();
                    break;
            }
        }
        foreach (Button button in gameObject.GetComponentsInChildren<Button>())
        {
            switch (button.gameObject.name)
            {
                case "Yes":
                    yes = button;
                    yes.onClick.AddListener(OnYes);
                    break;
                case "No":
                    no = button;
                    no.onClick.AddListener(OnNo);
                    break;
            }
        }
        scrollrect = gameObject.GetComponentInChildren<ScrollRect>();
    }

    void Update()
    {
        if (core != null)
        {
            float maxwidth = 0;
            float totalheight = 0;
            CenterResize resize = gameObject.GetComponent<CenterResize>();
            for (int i = 0; i < core.Zones.Count(); i++)
            {
                DesktopGroup group = groups[i];
                RectTransform group_rt = group.gameObject.GetComponent<RectTransform>();
                group_rt.localPosition = new Vector3(0, -totalheight);
                maxwidth = Math.Max(maxwidth, group_rt.rect.width);
                totalheight += group_rt.rect.height;
            }
            for (int i = core.Zones.Count(); i < groups.Count(); i++)
            {
                DesktopGroup group = groups[i];
                group.BoardCore = null;
                group.Core = null;
                App.Hide(group);
            }
            RectTransform grid_rt = Grid.GetComponent<RectTransform>();
            if (grid_rt != null)
                grid_rt.sizeDelta = new Vector2(
                    maxwidth, totalheight);
            if (resize != null)
            {
                resize.Width = maxwidth + 80;
                resize.Height = totalheight + 120;
            }
        }
        if (iscountingtime)
        {
            timeremain = Math.Max(0, timeremain - Time.deltaTime);
            timeout.size = timeremain / timemax;
            if (timeremain <= 0)
            {
                GameBoard gb = gameObject.transform.GetComponentInParent<GameBoard>();
                iscountingtime = false;
                // 如果可以取消，默认取消。
                if ((Core.Flag & Enum_DesktopCardBoardFlag.CannotNo) == Enum_DesktopCardBoardFlag.None)
                {
                    gb?.UI_DesktopBoard_No();
                    return;
                }
                // 如果选择不合法，使用AI自动选择。
                if (Core.CardFilter?.Fulfill(App.World.GetContext(), Core.SelectedCards) != true)
                {
                    IPlayerConsole console = Core.Controller.TrusteeshipConsole;
                    Core.SelectedCards.Clear();
                    console.ControlDesktop(Core);
                }
                // 将自动处理的结果报告返回。
                gb?.UI_DesktopBoard_Yes();
            }
        }
    }

    #endregion

    #region Method

    /// <summary>
    /// 进入或者退出计时。
    /// </summary>
    protected void EnterOrLeaveTimeout()
    {
        if (Core != null && IsControlling)
        {
            if (!iscountingtime)
            {
                timemax = Core.Timeout;
                timeremain = timemax;
                iscountingtime = true;
            }
        }
        else
        {
            if (iscountingtime)
            {
                iscountingtime = false;
                timemax = 100;
                timeremain = 100;
            }
        }
    }

    /// <summary>
    /// 获取一张卡的视觉对象。
    /// </summary>
    /// <param name="card">卡</param>
    /// <returns></returns>
    public CardBe GetCardView(Card card)
    {
        foreach (DesktopGroup group in groups)
        {
            if (group.Core == null) continue;
            int index = group.Cards.IndexOf(card);
            if (index < 0) continue;
            if (index >= group.CardViews.Count()) continue;
            return group.CardViews[index];
        }
        return null;
    }

    /// <summary>
    /// 更新每张卡的可选择性。
    /// </summary>
    protected void UpdateCardsSelectable()
    {
        Context ctx = App.World.GetContext();
        DesktopCardBoardCore core = Core;
        if (core == null) return;
        // 卡片是否支持拖动。
        bool candrag = ((core.Flag & Enum_DesktopCardBoardFlag.CanDrag) != Enum_DesktopCardBoardFlag.None);
        // 选择卡的安放区，如果有这个区，所有要选择的卡通过拖动的方式放置到这里。
        DesktopCardBoardZone selectzone = core.Zones.FirstOrDefault(_zone => (_zone.Flag & Enum_DesktopZoneFlag.AsSelected) != Enum_DesktopZoneFlag.None);
        if (selectzone != null) candrag = true;
        // 更新每个区的每张卡的可选择性。
        foreach (DesktopGroup group in groups)
        {
            DesktopCardBoardZone zone = group.Core;
            if (zone == null) continue;
            for (int i = 0; i < group.Cards.Count(); i++)
            {
                Card card = group.Cards[i];
                CardBe cardview = group.CardViews[i];
                bool canselect = false;
                // 拖动模式，所在区的Loster检查是否能拖动。
                if (candrag && zone.CardLoster?.CanLost(zone, card) == true)
                    canselect = true;
                // 选择模式，由选择器判定是否能选择。
                if (!candrag && core.CardFilter?.CanSelect(ctx, core.SelectedCards, card) == true)
                    canselect = true;
                // 如果已经被拿走，就不能被选择。
                if (card2removes.Contains(card))
                    canselect = false;
                // 是否允许拖动
                cardview.CanDrag = IsControlling && canselect && candrag;
                // 已经被拿走
                cardview.IsDesktopMoveOut = card2removes.Contains(card);
                // 进入选择模式
                cardview.IsEnterSelecting = IsControlling;
                // 更新是否能选择的属性。
                cardview.CanSelect = canselect;
                // 更新已经被选择的属性，如果有选择区可以设置为false，以是否在选择区进行判定。
                cardview.IsSelected = selectzone == null && core.SelectedCards.Contains(card);
            }
        }
        // 是否可以结束选择。
        yes.interactable = core.CardFilter.Fulfill(ctx, core.SelectedCards);
    }

    /// <summary>
    /// 点击一个卡片，进行选择或者取消选择。
    /// </summary>
    /// <param name="cardview"></param>
    public void Click(CardBe cardview)
    {
        if (!IsControlling) return;
        DesktopCardBoardCore core = Core;
        if (core == null) return;
        Context ctx = App.World.GetContext();
        bool candrag = ((core.Flag & Enum_DesktopCardBoardFlag.CanDrag) != Enum_DesktopCardBoardFlag.None);
        DesktopCardBoardZone selectzone = core.Zones.FirstOrDefault(_zone => (_zone.Flag & Enum_DesktopZoneFlag.AsSelected) != Enum_DesktopZoneFlag.None);
        if (selectzone != null) candrag = true;
        #region 选择模式
        if (!candrag)
        {
            // 取消选择。
            if (cardview.IsSelected)
            {
                core.SelectedCards.Remove(cardview.Core);
                UpdateCardsSelectable();
            }
            // 进行选择。
            else if (cardview.CanSelect)
            {
                core.SelectedCards.Add(cardview.Core);
                UpdateCardsSelectable();
            }
            // 仅选择一个可以直接返回。
            if (core.CardFilter?.Fulfill(ctx, core.SelectedCards) == true
             && core.SelectedCards.Count() == 1
             && (core.Flag & Enum_DesktopCardBoardFlag.SelectCardAndYes) != Enum_DesktopCardBoardFlag.None)
            {
                GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
                gb?.UI_DesktopBoard_Yes();
            }
        }
        #endregion
    }

    #region Drag & Drop

    public void DragEnter(CardBe cardview)
    {
        scrollrect.vertical = false;
        scrollrect.horizontal = false;
        DesktopCardBoardZone zone = cardview.Core.Zone as DesktopCardBoardZone;
        if (zone == null) return;
        DesktopGroup group = groups.FirstOrDefault(_group => _group.Core == zone);
        if (group == null) return;
        group.Remove(cardview);
        removegroup = group;
        insertgroup = null;
        cardview.gameObject.transform.parent = gameObject.transform;
    }

    public void DragMove(CardBe cardview)
    {
        insertgroup = null;
        foreach (DesktopGroup group in groups)
        {
            if (group.Core == null) continue;
            if (group.InsertPrepare(cardview))
                insertgroup = group;
        }
    }

    public void Drop(CardBe cardview)
    {
        scrollrect.vertical = true;
        scrollrect.horizontal = true;
        if (insertgroup != null)
            insertgroup.Insert(cardview);
        else
            removegroup.Restore(cardview);
        insertgroup = null;
        removegroup = null;
    }

    #endregion 

    #endregion 

    #region Event Handler

    private void OnYes()
    {
        GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
        gb?.UI_DesktopBoard_Yes();
    }

    private void OnNo()
    {
        GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
        gb?.UI_DesktopBoard_No();
    }

    internal void IvCardMoved(List<UIMoveCardEvent> moveevents)
    {
        foreach (UIMoveCardEvent ev in moveevents)
            foreach (Card card in ev.MovedCards)
                if (!card2removes.Contains(card))
                {
                    CardBe cardview = GetCardView(card);
                    card2removes.Add(card);
                    if (cardview != null
                     && ev.NewZone?.Owner != null)
                        cardview.DesktopComment = ev.NewZone.Owner.Name;
                }
        UpdateCardsSelectable();

    }

    #endregion
}
