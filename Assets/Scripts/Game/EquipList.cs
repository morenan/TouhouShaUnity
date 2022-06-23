using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using TouhouSha.Core.Filters;

public class EquipList : MonoBehaviour, IGameBoardArea
{
    #region Resources

    public class EquipCellHander
    {
        public EquipList Parent;
        public EquipCell Cell;
        public Button Button;

        public void OnClick()
        {
            if (Parent == null) return;
            GameBoard gb = Parent.gameObject.GetComponentInParent<GameBoard>();
            if (gb == null) return;
            if (!Cell.IsEnterSelecting) return;
            if (Cell.Card == null) return;
            if (Cell.IsSelected)
                gb.EquipUnselect(Cell.Card);
            else if (Cell.CanSelect)
                gb.EquipSelect(Cell.Card);
        }
    }

    #endregion

    #region Number

    public Player Player
    {
        get
        {
            GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
            return gb?.CurrentPlayer;
        }
    }

    public EquipCell[] Cells;
    private List<EquipCellHander> cellhanders = new List<EquipCellHander>();

    #endregion

    #region MonoBehavior
    
    void Awake()
    {
        while (cellhanders.Count() < Cells.Length)
        {
            EquipCellHander handler = new EquipCellHander();
            cellhanders.Add(handler);
        }
        for (int i = 0; i < Cells.Length; i++)
        {
            EquipCellHander handler = cellhanders[i];
            handler.Cell = Cells[i];
            handler.Parent = this;
            handler.Button = handler.Cell.gameObject.GetComponent<Button>();
            if (handler.Button != null)
                handler.Button.onClick.AddListener(handler.OnClick);
        }
    }

    void Update()
    {
        Player player = Player;
        float x = 4;
        if (player == null)
        {
            ClearVisual();
            return;
        }
        EquipZone equipzone = player.Zones.FirstOrDefault(_zone => _zone is EquipZone) as EquipZone;
        if (equipzone == null)
        {
            ClearVisual();
            return;    
        }
        Card[] equipcards = equipzone.Cards.ToArray();
        TouhouSha.Core.EquipCell[] equipcells = equipzone.Cells.ToArray();
        for (int i = 0; i < Cells.Length; i++)
        {
            if (i >= equipcells.Length) { Cells[i].Card = null; continue; }
            TouhouSha.Core.EquipCell equipcell = equipcells[i];
            if (!equipcell.IsEnabled
             || equipcell.CardIndex < 0 
             || equipcell.CardIndex >= equipcards.Length) { Cells[i].Card = null; continue; }
            Cells[i].Card = equipcards[equipcell.CardIndex];
        }
    }

    #endregion

    #region IGameBoardArea

    bool IGameBoardArea.KeptCards
    {
        get
        {
            return false;
        }
    }

    IList<Card> IGameBoardArea.Cards
    {
        get
        {
            return new List<Card>();
        }
    }

    Vector3? IGameBoardArea.GetExpectedPosition(Card card)
    {
        RectTransform rt = gameObject.GetComponent<RectTransform>();
        return new Vector3(
            rt.position.x + rt.rect.width / 2,
            rt.position.y + rt.rect.height / 2);
    }

    #endregion

    #region Method

    protected void ClearVisual()
    {
        foreach (EquipCell cell in Cells)
            cell.Card = null;
    }
    
    internal void UpdateAboutSelections(Context ctx, CardFilter cardfilter,
        IList<Card> selections, Dictionary<Card, Card> selectedinitcards,
        ISkillInitative skillinit, ISkillCardConverter skillconv)
    {
        #region 无最终筛选器
        if (cardfilter == null)
        {
            foreach (EquipCell cell in Cells)
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
            foreach (EquipCell cell in Cells)
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
            foreach (EquipCell cell in Cells)
            {
                cell.IsEnterSelecting = true;
                Card card = cell.Card;
                if (card != null) card = ctx.World.CalculateCard(ctx, card);
                #region 无装备
                if (card == null)
                {
                    cell.IsSelected = false;
                    cell.CanSelect = false;
                }
                #endregion
                #region 已经被选择
                else if (selectedinitcards.ContainsKey(cell.Card))
                {
                    cell.IsSelected = true;
                    cell.CanSelect = false;
                }
                #endregion
                #region 作为初动技能使用
                else if (skillinit is Skill && card.Skills.Contains(skillinit as Skill))
                {
                    cell.IsSelected = true;
                    cell.CanSelect = false;
                }
                #endregion
                #region 作为转化技能使用
                else if (skillconv is Skill && card.Skills.Contains(skillconv as Skill))
                {
                    cell.IsSelected = true;
                    cell.CanSelect = false;
                }
                #endregion
                #region 出牌阶段使用牌
                else if (cardfilter is UseCardStateCardFilter)
                {
                    // 可以使用装备带有的转化技能。
                    ISkillCardConverter conv = card.Skills.FirstOrDefault(_skill => _skill is ISkillCardConverter) as ISkillCardConverter;
                    // 可以使用装备带有的初动技能。
                    ISkillInitative init = card.Skills.FirstOrDefault(_skill => _skill is ISkillInitative) as ISkillInitative;
                    // 要使用装备，所以不要选择。
                    cell.IsSelected = false;
                    // 存在初动技能，判定能使用。
                    if (init != null)
                    {
                        ConditionFilter usecondition = init.UseCondition;
                        if (usecondition != null) usecondition = ctx.World.TryReplaceNewCondition(usecondition, null);
                        cell.CanSelect = usecondition?.Accept(ctx) == true;
                    }
                    // 存在转化技能，判定能使用。
                    else if (conv != null)
                    {
                        ConditionFilter usecondition = conv.UseCondition;
                        if (usecondition != null) usecondition = ctx.World.TryReplaceNewCondition(usecondition, null);
                        cell.CanSelect = usecondition?.Accept(ctx) == true;
                    }
                    // 不能使用不带可行技能的装备。
                    else
                    {
                        cell.CanSelect = false;
                    }
                }
                #endregion
                #region 要求响应特定牌
                else if (cardfilter is ICardFilterRequiredCardTypes)
                {
                    // 获取响应要求。
                    ICardFilterRequiredCardTypes required = (ICardFilterRequiredCardTypes)cardfilter;
                    // 可以使用武器带有的转化技能。
                    ISkillCardConverter conv = card.Skills.FirstOrDefault(_skill => _skill is ISkillCardConverter) as ISkillCardConverter;
                    // 响应牌不是装备，不选择装备而使用装备。
                    cell.IsSelected = false;
                    // 存在转化技能，判定能使用。
                    if (conv is ISkillCardMultiConverter)
                    {
                        ISkillCardMultiConverter multi = (ISkillCardMultiConverter)conv;
                        bool isenabled = false;
                        foreach (string cardtype in multi.GetCardTypes(ctx))
                        {
                            if (required.RequiredCardTypes.Contains(cardtype)) isenabled = true;
                            if (isenabled) break;
                        }
                        cell.CanSelect = isenabled;
                    }
                    // 不实现以上接口，就视作锁定转化。
                    else
                    {
                        cell.CanSelect = false;
                    }
                }
                #endregion
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
        List<Card> convertedcards, List<Card> remainscards, Dictionary<Card, Card> selectedinitcards,
        ISkillInitative skillinit, ISkillCardConverter skillconv)
    {
        foreach (EquipCell cell in Cells)
        {
            cell.IsEnterSelecting = true;
            Card card = cell.Card;
            if (card != null) card = ctx.World.CalculateCard(ctx, card);
            #region 无装备
            if (card == null)
            {
                cell.IsSelected = false;
                cell.CanSelect = false;
            }
            #endregion
            #region 已经被选择
            else if (selectedinitcards.ContainsKey(cell.Card))
            {
                cell.IsSelected = true;
                cell.CanSelect = false;
            }
            #endregion
            #region 作为初动技能使用
            else if (skillinit is Skill && card.Skills.Contains(skillinit as Skill))
            {
                cell.IsSelected = true;
                cell.CanSelect = false;
            }
            #endregion
            #region 作为转化技能使用
            else if (skillconv is Skill && card.Skills.Contains(skillconv as Skill))
            {
                cell.IsSelected = true;
                cell.CanSelect = false;
            }
            #endregion
            #region 不能通过转换技能的筛选器
            else if (!skillconvert.CardFilter.CanSelect(ctx, remainscards, card))
            {
                cell.IsSelected = false;
                cell.CanSelect = false;
            }
            #endregion
            #region 通过了转换技能的筛选器
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
            #endregion
        }
    }

    #endregion
}

