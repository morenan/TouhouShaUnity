using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TouhouSha.Core;
using TouhouSha.Core.UIs;
using TouhouSha.Core.UIs.Events;

public class WorldClient
{
    #region Member

    private GameBoard gameboard;
    public GameBoard GameBoard
    {
        get
        {
            return this.gameboard;
        }
        set
        {
            this.gameboard = value;
        }
    }

    private World world;
    public World World
    {
        get
        {
            return this.world;
        }
        set
        {
            this.world = value;
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
            {
                reader.UIEventReceived -= Reader_UIEventReceived;
                reader.PackageReceived -= Reader_PackageReceived;
            }
            this.reader = value;
            if (reader != null)
            {
                reader.UIEventReceived += Reader_UIEventReceived;
                reader.PackageReceived += Reader_PackageReceived;
            }
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
            if (writer == value) return;
            this.writer = value;
        }
    }

    #endregion

    #region Event Handler

    private void Reader_PackageReceived(object sender, ComPackageReceivedEventArgs e)
    {
        #region 通知包
        switch (e.Pack.Code)
        {
            case ComPackage.StartCode.DeterminePlayerCount:
                World?.RemoteDeterminePlayerCount(((ComPackageDeterminePlayerCount)(e.Pack)).PlayerCount);
                break;
            case ComPackage.StartCode.DetermineChairIndex:
                if (World != null)
                {
                    ComPackageDetermineChairIndex pack = (ComPackageDetermineChairIndex)(e.Pack);
                    GameBoard.PlayerRegisterInClient(pack.PunIds);
                }
                break;
            case ComPackage.StartCode.BuildPlayers:
                World?.RemoteBuildPlayers(((ComPackageBuildPlayers)(e.Pack)).Players);
                break;
            case ComPackage.StartCode.BuildCommonZones:
                World?.RemoteBuildCommonZones(((ComPackageBuildCommonZones)(e.Pack)).Zones);
                break;
            case ComPackage.StartCode.AllocAsses:
                World?.RemoteAllocAsses(((ComPackageAllocAsses)(e.Pack)).Players.ToDictionary(_player => _player, _player => _player.Ass));
                break;
            case ComPackage.StartCode.LeaderSelectCharactor:
                if (World != null)
                {
                    Player leader = World.Players.FirstOrDefault(_player => _player.Ass.E == Enum_PlayerAss.Leader);
                    if (leader != null)
                        World?.RemoteLeaderSelectCharactor(leader, leader.Charactors);
                }
                break;
            case ComPackage.StartCode.AllSelectCharactor:
                if (World != null)
                    World.RemoteAllSelectCharactor(World.Players.ToDictionary(
                        _player => _player, 
                        _player => _player.Charactors.Cast<Charactor>()));
                break;
            case ComPackage.StartCode.BuildCardInstances:
                if (World != null)
                {
                    ComPackageBuildCardInstances pack = (ComPackageBuildCardInstances)(e.Pack);
                    World.RemoteBuildCardInstances(pack.Privates, pack.Instances);
                }
                break;
            case ComPackage.StartCode.InitializeDrawZone:
                if (World != null)
                {
                    ComPackageInitializeDrawZone pack = (ComPackageInitializeDrawZone)(e.Pack);
                    World.RemoteInitializeDrawZone(pack.Cards);
                }
                break;
            case ComPackage.StartCode.BuildGlobalTrigger:
                if (World != null)
                {
                    ComPackageBuildGlobalTrigger pack = (ComPackageBuildGlobalTrigger)(e.Pack);
                    World.RemoteBuildGlobalTrigger(
                        pack.TriggerBefores,
                        pack.TriggerAfters,
                        pack.CalculatorBefores,
                        pack.CalculatorAfters,
                        pack.CardCalculatorBefores,
                        pack.CardCalculatorAfters);
                }
                break;
        }
        #endregion
        #region 命令包
        // 已走的路径： RemoteConsole => GameCom ... GameCom => WorldClient 
        // 要走的路径： WorldClient => World => GameBoardConsole => GameBoard(UI)
        switch (e.Pack.Code)
        {
            case ComPackage.StartCode.Ask:
                if (World != null)
                {
                    ComPackageAsk pack = (ComPackageAsk)(e.Pack);
                    World.RemoteAsk(pack.Controller, pack.KeyName, pack.Message, pack.Timeout);
                }
                break;
            case ComPackage.StartCode.SelectCharactor:
                if (World != null)
                {
                    ComPackageSelectCharactor pack = (ComPackageSelectCharactor)(e.Pack);
                    World.RemoteSelectCharactor(pack.Core);
                }
                break;
            case ComPackage.StartCode.SelectPlayer:
                if (World != null)
                {
                    ComPackageSelectPlayer pack = (ComPackageSelectPlayer)(e.Pack);
                    World.RemoteSelectPlayers(pack.Core);
                }
                break;
            case ComPackage.StartCode.SelectCard:
                if (World != null)
                {
                    ComPackageSelectCard pack = (ComPackageSelectCard)(e.Pack);
                    World.RemoteSelectCards(pack.Core);
                }
                break;
            case ComPackage.StartCode.SelectDesktop:
                if (World != null)
                {
                    ComPackageSelectDesktop pack = (ComPackageSelectDesktop)(e.Pack);
                    switch (pack.Action)
                    {
                        case ComPackageSelectDesktop.ActionCode.Open:
                            World.RemoteSelectDesktop(pack.Core);
                            break;
                        case ComPackageSelectDesktop.ActionCode.Control:
                            World.RemoteControlDesktop(pack.Core);
                            break;
                        case ComPackageSelectDesktop.ActionCode.Close:
                            World.RemoteCloseDesktop(pack.Core);
                            break;
                    }
                }
                break;
            case ComPackage.StartCode.SelectList:
                if (World != null)
                {
                    ComPackageSelectList pack = (ComPackageSelectList)(e.Pack);
                    World.RemoteSelectList(pack.Core);
                }
                break;
            case ComPackage.StartCode.QuestInUseCardState:
                if (World != null)
                {
                    ComPackageQuestInUseCardState pack = (ComPackageQuestInUseCardState)(e.Pack);
                    World.RemoteQuestEventInUseCardState(pack.Controller);
                }
                break;
            case ComPackage.StartCode.BreakLastAction:
                if (World != null)
                {
                    ComPackageBreakLastAction pack = (ComPackageBreakLastAction)(e.Pack);
                    World.RemoteBreakLastAction(pack.Controller);
                }
                break;
            case ComPackage.StartCode.HandMatch:
                if (World != null)
                {
                    ComPackageHandMatch pack = (ComPackageHandMatch)(e.Pack);
                    World.RemoteHandMatch(pack.Controller);
                }
                break;
            case ComPackage.StartCode.KOF:
                if (World != null)
                {
                    ComPackageKOF pack = (ComPackageKOF)(e.Pack);
                    switch (pack.Action)
                    {
                        case ComPackageKOF.ActionCode.Open:
                            World.RemoteOpenKOF(pack.Core);
                            break;
                        case ComPackageKOF.ActionCode.Control:
                            World.RemoteControlKOF(pack.Core, pack.Controller);
                            break;
                        case ComPackageKOF.ActionCode.Close:
                            World.RemoteCloseKOF(pack.Core);
                            break;
                    }
                }
                break;
        }
        #endregion
    }

    private void Reader_UIEventReceived(object sender, UIEventReceivedEventArgs e)
    {
        World?.RemoteUIEvent(e.Ev);
    }


    #endregion
}
