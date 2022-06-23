using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using TouhouSha.Core.UIs;

public class ExtraZoneList : MonoBehaviour
{
    #region Resources

    public class ButtonHandler
    {
        public ExtraZoneList Parent;
        public EquipCell Cell;
        public Button Button;
        
        public void OnClick()
        {
            if (Parent == null) return;
            GameBoard gb = Parent.gameObject.GetComponentInParent<GameBoard>();
            if (Cell.Card == null) return;
            if (Cell.IsSelected)
                gb?.ExtraZoneCardUnselected(Cell.Card);
            else if (Cell.CanSelect)
                gb?.ExtraZoneCardSelected(Cell.Card);
        }
    }

    #endregion

    #region Number

    private Zone zone;
    public Zone Zone
    {
        get
        {
            return this.zone;
        }
        set
        {
            if (zone == value) return;
            this.zone = value;
            UpdateCards();
        }
    }

    private Text nametext;
    private ScrollRect scrollrect;
    private List<EquipCell> cells = new List<EquipCell>();
    private List<ButtonHandler> buttonhandlers = new List<ButtonHandler>();
    
    #endregion 

    #region MonoBehavior

    void Awake()
    {
        scrollrect = gameObject.GetComponentInChildren<ScrollRect>();
        scrollrect.verticalNormalizedPosition = 1;
        nametext = gameObject.GetComponentInChildren<Text>();
        foreach (EquipCell cell in gameObject.GetComponentsInChildren<EquipCell>())
        {
            if (cells.Contains(cell)) continue;
            cells.Add(cell);
            App.Hide(cell);
            Button button = cell.gameObject.GetComponent<Button>();
            ButtonHandler handler = new ButtonHandler();
            handler.Button = button;
            handler.Cell = cell;
            handler.Parent = this;
            buttonhandlers.Add(handler);
            button.onClick.AddListener(handler.OnClick);
        }
        UpdateCards();
    }

    #endregion

    #region Method

    public void UpdateCards()
    {
        if (zone == null) return;
        if (cells.Count() == 0) return;
        float y = 0;
        Card[] cards = zone.Cards.ToArray();
        nametext.text = zone.KeyName;
        while (cells.Count() < zone.Cards.Count())
        {
            GameObject go0 = cells[0].gameObject;
            GameObject go1 = GameObject.Instantiate(go0, go0.transform.parent);
            EquipCell cell = go1.GetComponent<EquipCell>();
            cells.Add(cell);
            Button button = go1.GetComponent<Button>();
            ButtonHandler handler = new ButtonHandler();
            handler.Button = button;
            handler.Cell = cell;
            handler.Parent = this;
            buttonhandlers.Add(handler);
            button.onClick.AddListener(handler.OnClick);
        }
        for (int i = 0; i < cards.Count(); i++)
        {
            EquipCell cell = cells[i];
            RectTransform cell_rt = cell.gameObject.GetComponent<RectTransform>();
            cell.Card = cards[i];
            App.Show(cell);
        }
        for (int i = cards.Count(); i < cells.Count(); i++)
        {
            EquipCell cell = cells[i];
            cell.Card = null;
            App.Hide(cell);
        }
    }

    internal void UpdateAboutSelections(Context ctx, CardFilter cardfilter,
        IList<Card> selections, Dictionary<Card, Card> selectedinitcards)
    {
        #region 无最终筛选器
        if (cardfilter == null)
        {
            foreach (EquipCell cell in cells)
            {
                cell.IsEnterSelecting = false;
                cell.CanSelect = false;
                cell.IsSelected = false;
            }
        }
        #endregion
        #region 筛选器强制筛选全部
        else if ((cardfilter.GetFlag(ctx) & Enum_CardFilterFlag.ForceAll) != Enum_CardFilterFlag.None)
        {
            foreach (EquipCell cell in cells)
            {
                cell.IsEnterSelecting = true;
                Card card = cell.Card;
                if (card == null || selectedinitcards.ContainsKey(card)) continue;
                card = ctx.World.CalculateCard(ctx, card);
                if (card != null && cardfilter.CanSelect(ctx, selections, card))
                {
                    selections.Add(card);
                    selectedinitcards.Add(cell.Card, card);
                    cell.CanSelect = false;
                    cell.IsSelected = true;
                }
                else
                {
                    cell.CanSelect = false;
                    cell.IsSelected = false;
                }
            }
        }
        #endregion
        #region 使用筛选器校验
        else
        {
            foreach (EquipCell cell in cells)
            {
                cell.IsEnterSelecting = true;
                Card card = cell.Card;
                if (card != null) card = ctx.World.CalculateCard(ctx, card);
                if (card == null)
                {
                    cell.IsSelected = false;
                    cell.CanSelect = false;
                }
                else if (selectedinitcards.ContainsKey(cell.Card))
                {
                    cell.IsSelected = true;
                    cell.CanSelect = false;
                }
                else
                {
                    cell.IsSelected = false;
                    cell.CanSelect = cardfilter.CanSelect(ctx, selections, card);
                }
            }
        }
        #endregion
    }

    internal void UpdateWithConvertSkill(Context ctx, CardFilter cardfilter, ISkillCardConverter skillconvert,
        List<Card> convertedcards, List<Card> remainscards, Dictionary<Card, Card> selectedinitcards)
    {
        foreach (EquipCell cell in cells)
        {
            cell.IsEnterSelecting = true;
            Card card = cell.Card;
            if (card != null) card = ctx.World.CalculateCard(ctx, card);
            if (card == null)
            {
                cell.IsSelected = false;
                cell.CanSelect = false;
            }
            // 已经被选过了。
            else if (selectedinitcards.ContainsKey(cell.Card))
            {
                cell.IsSelected = true;
                cell.CanSelect = false;
            }
            // 不能通过转换技能的筛选器。
            else if (!skillconvert.CardFilter.CanSelect(ctx, remainscards, card))
            {
                cell.IsSelected = false;
                cell.CanSelect = false;
            }
            // 通过了转换技能的筛选器。
            else
            {
                // 暂时加入等待队列。
                remainscards.Add(card);
                // 如果满足转换立即转换，检验转换卡和之前的选择是否通过最终筛选器。
                if (skillconvert.CardFilter.Fulfill(ctx, remainscards))
                {
                    Card newconv = remainscards.Count() == 1
                        ? skillconvert.CardConverter.GetValue(ctx, remainscards[0])
                        : skillconvert.CardConverter.GetCombine(ctx, remainscards);
                    cell.IsSelected = false;
                    cell.CanSelect = cardfilter.CanSelect(ctx, convertedcards, newconv);
                }
                // 转换不满足，继续加卡转换。
                else
                {
                    cell.IsSelected = false;
                    cell.CanSelect = true;
                }
                // 离开暂时加入的等待队列。
                remainscards.Remove(card);
            }
        }
    }


    #endregion

}