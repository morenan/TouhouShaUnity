using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;

public class SkillList : MonoBehaviour
{
    #region Number

    public Player Player
    {
        get
        {
            GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
            return gb?.CurrentPlayer;
        }
    }

    private List<SkillButton> buttons = new List<SkillButton>();
    private Dictionary<Skill, bool> toggledowns = new Dictionary<Skill, bool>();
    private bool ignoretogglechanged;

    #endregion

    #region MonoBehavior

    void Awake()
    {
        foreach (SkillButton button in gameObject.GetComponentsInChildren<SkillButton>())
        {
            if (buttons.Contains(button)) continue;
            buttons.Add(button);
            App.Hide(button);
        }
    }

    void Update()
    {
        Player player = Player;
        float x = 4;
        if (player == null) return;
        Skill[] skills = player.Skills.ToArray();
        while (buttons.Count < skills.Length)
        {
            GameObject go0 = buttons[0].gameObject;
            GameObject go1 = GameObject.Instantiate(go0, gameObject.transform);
            SkillButton button = go1.GetComponent<SkillButton>();
            buttons.Add(button);
        }
        for (int i = skills.Length - 1; i >= 0; i--)
        {
            SkillButton button = buttons[i];
            RectTransform rt = button.gameObject.GetComponent<RectTransform>();
            button.Skill = skills[i];
            App.Show(button);
            rt.localPosition = new Vector3(x, 0);
            x -= rt.rect.width + 8;
        }
        for (int i = skills.Length; i < buttons.Count; i++)
        {
            SkillButton button = buttons[i];
            button.Skill = null;
            App.Hide(button);
        }
    }

    #endregion

    #region Method

    /// <summary>
    /// 进入卡牌选择状态，可以使用卡牌转化的技能。
    /// </summary>
    /// <param name="ctx">上下文</param>
    /// <param name="cardfilter">卡牌选择器</param>
    public void EnterCardSelect(Context ctx, CardFilter cardfilter)
    {
        ignoretogglechanged = true;
        foreach (SkillButton btn in buttons)
        {
            Skill skill = btn.Skill;
            // 保存之前的按下状态（技能自动应答是和否）。
            if (skill != null)
            {
                if (!toggledowns.ContainsKey(skill))
                    toggledowns.Add(skill, btn.IsChecked == true);
                else
                    toggledowns[skill] = btn.IsChecked == true;
            }
            // 初始状态为未按下。
            btn.IsChecked = false;
            // 只能转化卡牌。
            if (!(skill is ISkillCardConverter))
            {
                btn.IsEnabled = false;
                continue;
            }
            // 条件不通过。
            ISkillCardConverter conv = (ISkillCardConverter)skill;
            if (ctx.World.TryReplaceNewCondition(conv.UseCondition, null)?.Accept(ctx) != true)
            {
                btn.IsEnabled = false;
                continue;
            }
            // 知道技能可以转换哪些卡（ISkillCardMultiConverter）
            // 选择器也可以知道将要转成哪些卡（ICardFilterRequiredCardTypes）
            // 将要转成的卡集合带入技能，检查技能是否能转换至少一个。
            if (skill is ISkillCardMultiConverter
             && cardfilter is ICardFilterRequiredCardTypes)
            {
                ISkillCardMultiConverter multi = (ISkillCardMultiConverter)skill;
                ICardFilterRequiredCardTypes required = (ICardFilterRequiredCardTypes)cardfilter;
                bool isenabled = false;
                foreach (string cardtype in multi.GetCardTypes(ctx))
                {
                    if (required.RequiredCardTypes.Contains(cardtype)) isenabled = true;
                    if (isenabled) break;
                }
                if (!isenabled)
                {
                    btn.IsEnabled = false;
                    continue;
                }
            }
            // 如果响应阶段不能确定使用什么卡，通常是丢弃任意卡的场合。
            // 转化丢弃是不可实现的。
            if (!(cardfilter is ICardFilterRequiredCardTypes))
            {
                btn.IsEnabled = false;
                continue;
            }
            // 转换卡的技能可以使用。
            btn.IsEnabled = true;
        }
        ignoretogglechanged = false;
    }

    /// <summary>
    /// 离开卡牌选择状态。还原每个按钮之前的状态。
    /// </summary>
    public void LeaveCardSelect()
    {
        ignoretogglechanged = true;
        foreach (SkillButton btn in buttons)
        {
            bool toggledown = false;
            btn.IsEnabled = false;
            if (btn.Skill != null
             && toggledowns.TryGetValue(btn.Skill, out toggledown)
             && toggledown)
                btn.IsChecked = true;
            else
                btn.IsChecked = false;
        }
        ignoretogglechanged = false;
    }

    /// <summary> 进入出牌阶段状态，可以使用初动技能和转化技能。 </summary>
    /// <param name="ctx"></param>
    public void EnterUseCardState(Context ctx)
    {
        ignoretogglechanged = true;
        foreach (SkillButton btn in buttons)
        {
            Skill skill = btn.Skill;
            if (skill != null)
            {
                if (!toggledowns.ContainsKey(skill))
                    toggledowns.Add(skill, btn.IsChecked == true);
                else
                    toggledowns[skill] = btn.IsChecked == true;
            }
            btn.IsChecked = false;
            #region 转化技能检查
            if (skill is ISkillCardConverter)
            {
                ISkillCardConverter conv = (ISkillCardConverter)skill;
                // 条件不通过。
                if (ctx.World.TryReplaceNewCondition(conv.UseCondition, null)?.Accept(ctx) != true)
                {
                    btn.IsEnabled = false;
                    continue;
                }
                // 多种卡的转换技能，只要有一种能够使用就通过。
                if (conv is ISkillCardMultiConverter)
                {
                    ISkillCardMultiConverter conv1 = (ISkillCardMultiConverter)conv;
                    List<string> enabledtypes = new List<string>();
                    Zone hand = Player.Zones.FirstOrDefault(_zone => _zone.KeyName?.Equals(Zone.Hand) == true);
                    // 枚举每种卡。
                    foreach (string cardtype in conv1.GetCardTypes(ctx))
                    {
                        // 创建这个种类的虚拟卡，放置到手牌建立虚拟场景，检验其是否可以使用。
                        Card cardinst = ctx.World.GetCardInstance(cardtype);
                        if (cardinst == null) continue;
                        cardinst = cardinst.Clone();
                        bool canuse = false;
                        using (ZoneLock zoneenv = new ZoneLock(cardinst, hand))
                        {
                            // 对虚拟转化卡做标记，用于检查使用条件。例如不计入出杀，会跳过次数检查。
                            if (conv1 is ISkillCardConverterMark)
                                ((ISkillCardConverterMark)conv1).Mark(ctx, cardinst);
                            // 检查使用条件。
                            ConditionFilter usecondition = cardinst.UseCondition;
                            if (usecondition != null) usecondition = ctx.World.TryReplaceNewCondition(usecondition, null);
                            canuse = usecondition?.Accept(ctx) == true;
                        }
                        if (canuse)
                        {
                            enabledtypes.Add(cardtype);
                            break;
                        }
                    }
                    // 只要有一种能够使用就通过，否则不通过。
                    if (enabledtypes.Count() == 0)
                    {
                        btn.IsEnabled = false;
                        continue;
                    }
                }
                // 转换卡的技能可以使用。
                btn.IsEnabled = true;
            }
            #endregion
            #region 初动技能检查
            else if (skill is ISkillInitative)
            {
                ISkillInitative init = (ISkillInitative)skill;
                btn.IsEnabled = ctx.World.TryReplaceNewCondition(init.UseCondition, null)?.Accept(ctx) == true;
            }
            #endregion
            #region 触发技能不能使用
            else
            {
                btn.IsEnabled = false;
            }
            #endregion
        }
        ignoretogglechanged = false;
    }

    /// <summary> 离开出牌阶段状态。还原每个按钮之前的状态。 </summary>
    public void LeaveUseCardState()
    {
        ignoretogglechanged = true;
        foreach (SkillButton btn in buttons)
        {
            bool toggledown = false;
            btn.IsEnabled = false;
            if (btn.Skill != null
             && toggledowns.TryGetValue(btn.Skill, out toggledown)
             && toggledown)
                btn.IsChecked = true;
            else
                btn.IsChecked = false;
        }
        ignoretogglechanged = false;
    }

    /// <summary>
    /// 将一个技能的按钮设为【按下状态】。
    /// </summary>
    /// <param name="skill"></param>
    public void SetCheck(Skill skill)
    {
        foreach (SkillButton btn in buttons)
        {
            if (btn.Skill == skill)
            {
                btn.IsChecked = true;
                break;
            }
        }
    }

    /// <summary>
    /// 将一个技能的按钮设为【未按下状态】。
    /// </summary>
    public void SetUncheck(Skill skill)
    {
        foreach (SkillButton btn in buttons)
        {
            if (btn.Skill == skill)
            {
                btn.IsChecked = false;
                break;
            }
        }
    }

    #endregion

    #region Event Handler

    public void SkillButton_Checked(SkillButton btn)
    {
        if (ignoretogglechanged) return;
        if (btn.Skill == null) return;
        GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
        gb?.UI_Skills_SkillChecked(btn.Skill);
    }

    public void SkillButton_Unchecked(SkillButton btn)
    {
        if (ignoretogglechanged) return;
        if (btn.Skill == null) return;
        GameBoard gb = gameObject.GetComponentInParent<GameBoard>();
        gb?.UI_Skills_SkillUnchecked(btn.Skill);
    }

    #endregion
}

public class SkillCheckedEventArgs : EventArgs
{
    public SkillCheckedEventArgs(Skill _skill)
    {
        this.skill = _skill;
    }

    private Skill skill;
    public Skill Skill { get { return this.skill; } }
}

public class SkillUncheckedEventArgs : EventArgs
{
    public SkillUncheckedEventArgs(Skill _skill)
    {
        this.skill = _skill;
    }

    private Skill skill;
    public Skill Skill { get { return this.skill; } }
}

public delegate void SkillCheckedEventHandler(object sender, SkillCheckedEventArgs e);

public delegate void SkillUncheckedEventHandler(object sender, SkillUncheckedEventArgs e);
