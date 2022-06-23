using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TouhouSha.Core;
using TouhouSha.Core.Events;
using TouhouSha.Core.UIs;

public class RemoteConsole : IPlayerConsole
{
    public RemoteConsole(Player _controller, string _punid)
    {
        this.controller = _controller;
        this.punid = _punid;
        this.worldstop = new AutoResetEvent(false);
    }

    #region Member

    private string punid;
    public string PunId
    {
        get
        {
            return this.punid;
        }
    }

    private GameCom writer;
    public GameCom Writer
    {
        get
        {
            return this.writer;
        }
        set
        {
            this.writer = value;           
        }
    }

    private GameCom reader;
    public GameCom Reader
    {
        get
        {
            return this.reader;
        }
        set
        {
            if (reader == value) return;
            if (reader != null)
                reader.PackageReceived -= OnPackageReceived;
            this.reader = value;
            if (reader != null)
                reader.PackageReceived += OnPackageReceived;
        }
    }


    private Player controller;
    public Player Controller
    {
        get { return this.controller; }
    }

    Player IPlayerConsole.Owner
    {
        get { return Controller; }
    }

    /// <summary> 等待事件，等待UI操作完毕。 </summary>
    private AutoResetEvent worldstop;
    /// <summary> 当前正在发送的包，等待接受对应的结果包。 </summary>
    private ComPackage sending;
    
    #endregion

    #region Method

    protected void SendPackage(ComPackage pack)
    {
        if (Writer != null) Writer.SendPackage(pack);
    }

    internal void WorldContiune()
    {
        worldstop.Set();
    }

    protected bool CanSwitch(KOFSelectCore core)
    {
        int remains = core.Charactors.Count(_c => _c != null);
        if (remains == 0) return true;
        int n0 = 0;
        int n1 = 0;
        foreach (KeyValuePair<Player, List<Charactor>> kvp in core.PlayerSelecteds)
        {
            if (kvp.Key == Controller)
                n0 = kvp.Value.Count();
            else
                n1 = kvp.Value.Count();
        }
        return n0 <= n1;
    }

    #endregion

    #region IPlayerConsole

    bool IPlayerConsole.Ask(Context ctx, string keyname, string message, int timeout)
    {
        ComPackageAsk pack = new ComPackageAsk();
        pack.Controller = Controller;
        pack.KeyName = keyname;
        pack.Message = message;
        pack.IsYes = true;
        sending = pack;
        SendPackage(pack);
        worldstop.WaitOne(timeout * 1000);
        sending = null;
        return pack.IsYes;
    }

    void IPlayerConsole.SelectCharactor(SelectCharactorBoardCore core)
    {
        ComPackageSelectCharactor pack = new ComPackageSelectCharactor();
        pack.Core = core;
        core.SelectedCharactor = null;
        sending = pack;
        SendPackage(pack);
        worldstop.WaitOne(core.Timeout * 1000);
        sending = null;
        if (core.SelectedCharactor == null)
            Controller.TrusteeshipConsole.SelectCharactor(core);
    }

    void IPlayerConsole.SelectCards(SelectCardBoardCore core)
    {
        ComPackageSelectCard pack = new ComPackageSelectCard();
        pack.Core = core;
        core.IsYes = false;
        core.SelectedCards.Clear();
        sending = pack;
        SendPackage(pack);
        worldstop.WaitOne(core.Timeout * 1000);
        sending = null;
        if (!core.IsYes && !core.CanCancel)
            Controller.TrusteeshipConsole.SelectCards(core);
    }

    void IPlayerConsole.SelectPlayers(SelectPlayerBoardCore core)
    {
        ComPackageSelectPlayer pack = new ComPackageSelectPlayer();
        pack.Core = core;
        core.IsYes = false;
        core.SelectedPlayers.Clear();
        sending = pack;
        SendPackage(pack);
        worldstop.WaitOne(core.Timeout * 1000);
        sending = null;
        if (!core.IsYes && !core.CanCancel)
            Controller.TrusteeshipConsole.SelectPlayers(core);
    }

    void IPlayerConsole.SelectDesktop(DesktopCardBoardCore core)
    {
        ComPackageSelectDesktop pack = new ComPackageSelectDesktop();
        pack.Action = ComPackageSelectDesktop.ActionCode.Control;
        pack.Core = core;
        if (core.IsAsync) pack.Action = ComPackageSelectDesktop.ActionCode.Open;
        core.IsYes = false;
        core.SelectedCards.Clear();
        sending = pack;
        SendPackage(pack);
        if (!core.IsAsync)
            worldstop.WaitOne(core.Timeout * 1000);
        sending = null;
        if (!core.IsAsync && !core.IsYes && (core.Flag & Enum_DesktopCardBoardFlag.CannotNo) != Enum_DesktopCardBoardFlag.None)
            Controller.TrusteeshipConsole.SelectDesktop(core);
    }

    void IPlayerConsole.ControlDesktop(DesktopCardBoardCore core)
    {
        ComPackageSelectDesktop pack = new ComPackageSelectDesktop();
        pack.Action = ComPackageSelectDesktop.ActionCode.Control;
        pack.Core = core;
        core.Controller = Controller;
        core.IsYes = false;
        core.SelectedCards.Clear();
        sending = pack;
        SendPackage(pack);
        worldstop.WaitOne(core.Timeout * 1000);
        sending = null;
        if (!core.IsYes && (core.Flag & Enum_DesktopCardBoardFlag.CannotNo) != Enum_DesktopCardBoardFlag.None)
            Controller.TrusteeshipConsole.SelectDesktop(core);
    }


    void IPlayerConsole.CloseDesktop(DesktopCardBoardCore core)
    {
        ComPackageSelectDesktop pack = new ComPackageSelectDesktop();
        pack.Action = ComPackageSelectDesktop.ActionCode.Close;
        pack.Core = core;
        sending = pack;
        SendPackage(pack);
        sending = null;
    }

    void IPlayerConsole.SelectList(ListBoardCore core)
    {
        ComPackageSelectList pack = new ComPackageSelectList();
        pack.Core = core;
        core.IsYes = false;
        core.SelectedItems.Clear();
        sending = pack;
        SendPackage(pack);
        worldstop.WaitOne(core.Timeout * 1000);
        sending = null;
        if (!core.IsYes && !core.CanCancel)
            Controller.TrusteeshipConsole.SelectList(core);
    }

    void IPlayerConsole.BreakLastAction()
    {
        ComPackageBreakLastAction pack = new ComPackageBreakLastAction();
        pack.Controller = Controller;
        sending = pack;
        SendPackage(pack);
        sending = null;
    }

    TouhouSha.Core.Event IPlayerConsole.QuestEventInUseCardState(Context ctx)
    {
        ComPackageQuestInUseCardState pack = new ComPackageQuestInUseCardState();
        pack.Controller = Controller;
        pack.Event = new LeaveUseCardStateEvent(ctx.World.GetPlayerState());
        sending = pack;
        SendPackage(pack);
        worldstop.WaitOne(Config.GameConfig.Timeout_UseCard * 1000);
        sending = null;
        return pack.Event;
    }

    HandMatchGesture IPlayerConsole.HandMatch()
    {
        Random random = new Random();
        ComPackageHandMatch pack = new ComPackageHandMatch();
        pack.Controller = Controller;
        pack.Result = (HandMatchGesture)(random.Next() % 3);
        sending = pack;
        SendPackage(pack);
        worldstop.WaitOne(Config.GameConfig.Timeout_Handle * 1000);
        sending = null;
        return pack.Result;
    }

    void IPlayerConsole.OpenKOF(KOFSelectCore core)
    {
        ComPackageKOF pack = new ComPackageKOF();
        pack.Action = ComPackageKOF.ActionCode.Open;
        pack.Controller = Controller;
        pack.Core = core;
        sending = pack;
        SendPackage(pack);
        sending = null;
    }

    void IPlayerConsole.ControlKOF(KOFSelectCore core)
    {
        ComPackageKOF pack = new ComPackageKOF();
        pack.Action = ComPackageKOF.ActionCode.Control;
        pack.Controller = Controller;
        pack.Core = core;
        sending = pack;
        SendPackage(pack);
        worldstop.WaitOne(core.Timeout * 1000);
        sending = null;
        if (!CanSwitch(core))
            Controller.TrusteeshipConsole.ControlKOF(core);
    }

    void IPlayerConsole.CloseKOF(KOFSelectCore core)
    {
        ComPackageKOF pack = new ComPackageKOF();
        pack.Action = ComPackageKOF.ActionCode.Close;
        pack.Controller = Controller;
        pack.Core = core;
        sending = pack;
        SendPackage(pack);
        sending = null;
    }

    #endregion

    #region Event Handler

    private void OnPackageReceived(object sender, ComPackageReceivedEventArgs e)
    {
        #region 回应应答(Ask)
        if (sending is ComPackageAsk
         && e.Pack is ComPackageAskResult)
        {
            ComPackageAsk pack0 = (ComPackageAsk)sending;
            ComPackageAskResult pack1 = (ComPackageAskResult)e.Pack;
            pack0.IsYes = pack1.IsYes;
            WorldContiune();
            return;
        }
        #endregion 
        #region 回应角色选择(SelectCharactor)
        if (sending is ComPackageSelectCharactor 
         && e.Pack is ComPackageSelectCharactorResult)
        {
            ComPackageSelectCharactor pack0 = (ComPackageSelectCharactor)sending;
            ComPackageSelectCharactorResult pack1 = (ComPackageSelectCharactorResult)e.Pack;
            pack0.Core.SelectedCharactor = pack1.SelectedCharactor;
            WorldContiune();
            return;
        }
        #endregion
        #region 回应卡牌选择(SelectCard)
        if (sending is ComPackageSelectCard
         && e.Pack is ComPackageSelectCardResult)
        {
            ComPackageSelectCard pack0 = (ComPackageSelectCard)sending;
            ComPackageSelectCardResult pack1 = (ComPackageSelectCardResult)(e.Pack);
            pack0.Core.IsYes = pack1.IsYes;
            pack0.Core.SelectedCards.Clear();
            pack0.Core.SelectedCards.AddRange(pack1.SelectedCards);
            pack0.Core.UsedConverter = pack1.UsedCardConverter as ISkillCardConverter;
            pack0.Core.UsedConverterFromCard = pack1.UsedCardConverterFromCard;
            WorldContiune();
            return;
        }
        #endregion
        #region 回应目标选择(SelectPlayer)
        if (sending is ComPackageSelectPlayer
         && e.Pack is ComPackageSelectPlayerResult)
        {
            ComPackageSelectPlayer pack0 = (ComPackageSelectPlayer)sending;
            ComPackageSelectPlayerResult pack1 = (ComPackageSelectPlayerResult)e.Pack;
            pack0.Core.IsYes = pack1.IsYes;
            pack0.Core.SelectedPlayers.Clear();
            pack0.Core.SelectedPlayers.AddRange(pack1.SelectedPlayers);
            WorldContiune();
            return;
        }
        #endregion
        #region 回应目标和卡牌选择(SelectPlayerAndCard)
        if (sending is ComPackageSelectPlayerAndCard
         && e.Pack is ComPackageSelectPlayerAndCardResult)
        {
            ComPackageSelectPlayerAndCard pack0 = (ComPackageSelectPlayerAndCard)sending;
            ComPackageSelectPlayerAndCardResult pack1 = (ComPackageSelectPlayerAndCardResult)e.Pack;
            pack0.Core.IsYes = pack1.IsYes;
            pack0.Core.SelectedPlayers.Clear();
            pack0.Core.SelectedPlayers.AddRange(pack1.SelectedPlayers);
            pack0.Core.SelectedCards.Clear();
            pack0.Core.SelectedCards.AddRange(pack1.SelectedCards);
            //pack0.Core.UsedConverter = pack1.UsedCardConverter as ISkillCardConverter;
            //pack0.Core.UsedConverterFromCard = pack1.UsedCardConverterFromCard;
            WorldContiune();
            return;
        }
        #endregion
        #region 回应桌面选择(SelectDesktop)
        if (sending is ComPackageSelectDesktop
         && e.Pack is ComPackageSelectDesktopResult)
        {
            // 使用索引引用而不是单独的副本。引用对象已修改。
            //ComPackageSelectDesktop pack0 = (ComPackageSelectDesktop)sending;
            //ComPackageSelectDesktopResult pack1 = (ComPackageSelectDesktopResult)e.Pack;
            WorldContiune();
            return;
        }
        #endregion
        #region 回应列表选择(SelectList)
        if (sending is ComPackageSelectList
         && e.Pack is ComPackageSelectListResult)
        {
            ComPackageSelectList pack0 = (ComPackageSelectList)sending;
            ComPackageSelectListResult pack1 = (ComPackageSelectListResult)e.Pack;
            pack0.Core.IsYes = pack1.IsYes;
            pack0.Core.SelectedItems.Clear();
            pack0.Core.SelectedItems.AddRange(pack1.SelectedItems);
            WorldContiune();
            return;
        }
        #endregion
        #region 回应出牌阶段选择(QuestEventInUseCardState)
        if (sending is ComPackageQuestInUseCardState
         && e.Pack is ComPackageEventInUseCardState)
        {
            ComPackageQuestInUseCardState pack0 = (ComPackageQuestInUseCardState)sending;
            ComPackageEventInUseCardState pack1 = (ComPackageEventInUseCardState)sending;
            pack0.Event = pack1.Event;
            WorldContiune();
            return;
        }
        #endregion
        #region 回应猜拳选择(HandMatch)
        if (sending is ComPackageHandMatch
         && e.Pack is ComPackageHandMatchResult)
        {
            ComPackageHandMatch pack0 = (ComPackageHandMatch)sending;
            ComPackageHandMatchResult pack1 = (ComPackageHandMatchResult)sending;
            pack0.Result = pack1.Result;
            WorldContiune();
            return;
        }
        #endregion
        #region 回应KOF角色选择
        if (sending is ComPackageKOF
         && e.Pack is ComPackageKOFResult)
        {
            ComPackageKOF pack0 = (ComPackageKOF)sending;
            ComPackageKOFResult pack1 = (ComPackageKOFResult)e.Pack;
            pack0.Core.Select(Controller, pack1.SelectedCharactor);
            if (CanSwitch(pack0.Core)) WorldContiune();
            return;
        }
        #endregion
    }

    #endregion
}
