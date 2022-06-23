using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Photon.Pun;
using Photon.Realtime;
using TouhouSha.Core;
using TouhouSha.Core.UIs;

public abstract class ComPackage
{
    public enum StartCode
    {
        Ask,
        AskResult,
        SelectCharactor,
        SelectCharactorResult,
        SelectCard,
        SelectCardResult,
        SelectPlayer,
        SelectPlayerResult,
        SelectPlayerAndCard,
        SelectPlayerAndCardResult,
        SelectDesktop,
        SelectDesktopResult,
        SelectList,
        SelectListResult,
        BreakLastAction,
        QuestInUseCardState,
        EventInUseCardState,
        HandMatch,
        HandMatchResult,
        KOF,
        KOFResult,

        DeterminePlayerCount,
        DetermineChairIndex,
        BuildPlayers,
        BuildCommonZones,
        AllocAsses,
        LeaderSelectCharactor,
        AllSelectCharactor,
        BuildCardInstances,
        InitializeDrawZone,
        BuildGlobalTrigger,
        EnterState,
        EnterEvent,
        LeaveStack,
        UIEvent,
    }

    static public ComPackage Create(StartCode code)
    {
        switch (code)
        {
            case StartCode.Ask: return new ComPackageAsk();
            case StartCode.AskResult: return new ComPackageAskResult();
        }
        return null;
    }

    static public void Write(PhotonStream s, GameCom com, ComPackage pack)
    {
        s.SendNext(pack.Code);
        pack.WriteOverride(s, com);    
    }

    static public ComPackage Read(PhotonStream s, GameCom com)
    {
        StartCode code = (StartCode)(int)(s.ReceiveNext());
        ComPackage pack = Create(code);
        if (pack == null) return pack;
        pack.ReadOverride(s, com);
        return pack;
    }
    
    public abstract StartCode Code { get; } 

    protected abstract void WriteOverride(PhotonStream s, GameCom com);
    protected abstract void ReadOverride(PhotonStream s, GameCom com);
}

public class ComPackageReceivedEventArgs : EventArgs
{
    public ComPackageReceivedEventArgs(ComPackage _pack)
    {
        this.pack = _pack;
    }

    private ComPackage pack;
    public ComPackage Pack => pack;
}

public delegate void ComPackageReceivedEventHandler(object sender, ComPackageReceivedEventArgs e);

public class ComPackageAsk : ComPackage
{
    public override StartCode Code => StartCode.Ask;

    public TouhouSha.Core.Player Controller;
    public string KeyName;
    public string Message;
    public int Timeout;
    public bool IsYes;
    
    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        com.SendNext(s, Controller);
        s.SendNext(KeyName);
        s.SendNext(Message);
        s.SendNext(Timeout);
    }

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        Controller = com.ReceiveNext(s) as TouhouSha.Core.Player;
        KeyName = (string)s.ReceiveNext();
        Message = (string)s.ReceiveNext();
        Timeout = (int)s.ReceiveNext();
    }
}

public class ComPackageAskResult : ComPackage
{
    public override StartCode Code => StartCode.AskResult;

    public bool IsYes;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext(IsYes);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        IsYes = (bool)s.ReceiveNext();
    }
}

public class ComPackageSelectCharactor : ComPackage
{
    public override StartCode Code => StartCode.SelectCharactor;

    public SelectCharactorBoardCore Core;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext(Core.Message);
        s.SendNext(Core.Controller);
        com.SendNext(s, Core.Charactors);
        s.SendNext(Core.Timeout);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        Core = new SelectCharactorBoardCore();
        Core.Message = (string)s.ReceiveNext();
        Core.Controller = com.ReceiveNext(s) as TouhouSha.Core.Player;
        Core.Charactors.Clear();
        foreach (Charactor char0 in ((IEnumerable)(com.ReceiveNext(s))))
            Core.Charactors.Add(char0);
        Core.Timeout = (int)s.ReceiveNext();    
    }
}

public class ComPackageSelectCharactorResult : ComPackage
{
    public override StartCode Code => StartCode.SelectCharactorResult;

    public Charactor SelectedCharactor;


    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        com.SendNext(s, SelectedCharactor);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        SelectedCharactor = com.ReceiveNext(s) as Charactor;
    }
}

public class ComPackageSelectCard : ComPackage
{
    public override StartCode Code => StartCode.SelectCard;

    public SelectCardBoardCore Core;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext(Core.KeyName);
        s.SendNext(Core.Message);
        com.SendNext(s, Core.Controller);
        com.SendNext(s, Core.CardFilter);
        s.SendNext(Core.CanCancel);
        s.SendNext(Core.Timeout);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        Core = new SelectCardBoardCore();
        Core.KeyName = (string)s.ReceiveNext();
        Core.Message = (string)s.ReceiveNext();
        Core.Controller = com.ReceiveNext(s) as TouhouSha.Core.Player;
        Core.CardFilter = com.ReceiveNext(s) as CardFilter;
        Core.CanCancel = (bool)s.ReceiveNext();
        Core.Timeout = (int)s.ReceiveNext();
    }
}

public class ComPackageSelectCardResult : ComPackage
{
    public override StartCode Code => StartCode.SelectCardResult;

    public bool IsYes;
    public List<Card> SelectedCards = new List<Card>();
    public Skill UsedCardConverter;
    public Card UsedCardConverterFromCard;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext(IsYes);
        com.SendNext(s, SelectedCards);
        com.SendNext(s, UsedCardConverter);
        com.SendNext(s, UsedCardConverterFromCard);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        IsYes = (bool)s.ReceiveNext();
        SelectedCards = new List<Card>();
        foreach (Card card in ((IEnumerable)(com.ReceiveNext(s))))
            SelectedCards.Add(card);
        UsedCardConverter = com.ReceiveNext(s) as Skill;
        UsedCardConverterFromCard = com.ReceiveNext(s) as Card;
    }
}

public class ComPackageSelectPlayer : ComPackage
{
    public override StartCode Code => StartCode.SelectPlayer;

    public SelectPlayerBoardCore Core;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext(Core.KeyName);
        s.SendNext(Core.Message);
        com.SendNext(s, Core.Controller);
        com.SendNext(s, Core.PlayerFilter);
        s.SendNext(Core.CanCancel);
        s.SendNext(Core.Timeout);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        Core = new SelectPlayerBoardCore();
        Core.KeyName = (string)s.ReceiveNext();
        Core.Message = (string)s.ReceiveNext();
        Core.Controller = com.ReceiveNext(s) as TouhouSha.Core.Player;
        Core.PlayerFilter = com.ReceiveNext(s) as PlayerFilter;
        Core.CanCancel = (bool)s.ReceiveNext();
        Core.Timeout = (int)s.ReceiveNext();
    }
}

public class ComPackageSelectPlayerResult : ComPackage
{
    public override StartCode Code => StartCode.SelectPlayerResult;

    public bool IsYes;
    public List<TouhouSha.Core.Player> SelectedPlayers = new List<TouhouSha.Core.Player>();

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext(IsYes);
        com.SendNext(s, SelectedPlayers);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        IsYes = (bool)s.ReceiveNext();
        SelectedPlayers = new List<TouhouSha.Core.Player>();
        foreach (TouhouSha.Core.Player player in ((IEnumerable)(com.ReceiveNext(s))))
            SelectedPlayers.Add(player);

    }
}

public class ComPackageSelectPlayerAndCard : ComPackage
{
    public override StartCode Code => StartCode.SelectPlayerAndCard;

    public SelectPlayerAndCardBoardCore Core;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext(Core.KeyName);
        s.SendNext(Core.Message);
        com.SendNext(s, Core.Controller);
        com.SendNext(s, Core.PlayerFilter);
        com.SendNext(s, Core.CardFilter);
        s.SendNext(Core.CanCancel);
        s.SendNext(Core.Timeout);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        Core = new SelectPlayerAndCardBoardCore();
        Core.KeyName = (string)s.ReceiveNext();
        Core.Message = (string)s.ReceiveNext();
        Core.Controller = com.ReceiveNext(s) as TouhouSha.Core.Player;
        Core.PlayerFilter = com.ReceiveNext(s) as PlayerFilter;
        Core.CardFilter = com.ReceiveNext(s) as CardFilter;
        Core.CanCancel = (bool)s.ReceiveNext();
        Core.Timeout = (int)s.ReceiveNext();
    }
}

public class ComPackageSelectPlayerAndCardResult : ComPackage
{
    public override StartCode Code => StartCode.SelectPlayerAndCardResult;

    public bool IsYes;
    public List<TouhouSha.Core.Player> SelectedPlayers = new List<TouhouSha.Core.Player>();
    public List<Card> SelectedCards = new List<Card>();
    public Skill UsedCardConverter;
    public Card UsedCardConverterFromCard;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext(IsYes);
        com.SendNext(s, SelectedPlayers);
        com.SendNext(s, SelectedCards);
        com.SendNext(s, UsedCardConverter);
        com.SendNext(s, UsedCardConverterFromCard);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        IsYes = (bool)s.ReceiveNext();
        SelectedPlayers = new List<TouhouSha.Core.Player>();
        foreach (TouhouSha.Core.Player player in ((IEnumerable)(com.ReceiveNext(s))))
            SelectedPlayers.Add(player);
        SelectedCards = new List<Card>();
        foreach (Card card in ((IEnumerable)(com.ReceiveNext(s))))
            SelectedCards.Add(card);
        UsedCardConverter = com.ReceiveNext(s) as Skill;
        UsedCardConverterFromCard = com.ReceiveNext(s) as Card;
    }
}

public class ComPackageSelectDesktop : ComPackage
{
    public enum ActionCode
    {
        Open,
        Control,
        Close,
    }

    public override StartCode Code => StartCode.SelectDesktop;

    public DesktopCardBoardCore Core;
    public ActionCode Action;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext((int)Action);
        com.SendNext(s, Core);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        Action = (ActionCode)(int)s.ReceiveNext();
        Core = com.ReceiveNext(s) as DesktopCardBoardCore;
    }
}

public class ComPackageSelectDesktopResult : ComPackage
{
    public override StartCode Code => StartCode.SelectDesktopResult;

    public DesktopCardBoardCore Core;
    
    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        com.SendNext(s, Core);
        s.SendNext(Core.IsYes);
        com.SendNext(s, Core.SelectedCards);
        s.SendNext(Core.Zones.Count());
        foreach (DesktopCardBoardZone zone in Core.Zones)
        {
            com.SendNext(s, zone);
            com.SendNext(s, zone.Cards);
        }
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        Core = com.ReceiveNext(s) as DesktopCardBoardCore;
        Core.IsYes = (bool)com.ReceiveNext(s);
        Core.SelectedCards.Clear();
        foreach (Card card in ((IEnumerable)(com.ReceiveNext(s))))
            Core.SelectedCards.Add(card);
        int n = (int)(s.ReceiveNext());
        while (n-- > 0)
        {
            DesktopCardBoardZone zone = com.ReceiveNext(s) as DesktopCardBoardZone;
            if (zone != null) zone.Cards.Clear();
            foreach (Card card in ((IEnumerable)(com.ReceiveNext(s))))
                if (zone != null) zone.Cards.Add(card);
        }
    }

}

public class ComPackageSelectList : ComPackage
{
    public override StartCode Code => StartCode.SelectList;

    public ListBoardCore Core;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext(Core.KeyName);
        s.SendNext(Core.Message);
        com.SendNext(s, Core.Controller);
        com.SendNext(s, Core.Items);
        s.SendNext(Core.SelectMax);
        s.SendNext(Core.CanCancel);
        s.SendNext(Core.Timeout);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        Core = new ListBoardCore();
        Core.KeyName = (string)s.ReceiveNext();
        Core.Message = (string)s.ReceiveNext();
        Core.Controller = com.ReceiveNext(s) as TouhouSha.Core.Player;
        Core.Items.Clear();
        foreach (object item in ((IEnumerable)(com.ReceiveNext(s))))
            Core.Items.Add(item);
        Core.SelectMax = (int)s.ReceiveNext();
        Core.CanCancel = (bool)s.ReceiveNext();
        Core.Timeout = (int)s.ReceiveNext();
    }
}

public class ComPackageSelectListResult : ComPackage
{
    public override StartCode Code => StartCode.SelectListResult;

    public bool IsYes;
    public List<object> SelectedItems = new List<object>();


    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext(IsYes);
        com.SendNext(s, SelectedItems);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        IsYes = (bool)s.ReceiveNext();
        SelectedItems = ((IEnumerable)(com.ReceiveNext(s))).Cast<object>().ToList();
    }
}

public class ComPackageBreakLastAction : ComPackage
{
    public override StartCode Code => StartCode.BreakLastAction;

    public TouhouSha.Core.Player Controller;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        com.SendNext(s, Controller);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        Controller = com.ReceiveNext(s) as TouhouSha.Core.Player;
    }
}

public class ComPackageQuestInUseCardState : ComPackage
{
    public override StartCode Code => StartCode.QuestInUseCardState;

    public TouhouSha.Core.Player Controller;
    public Event Event;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        com.SendNext(s, Controller);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        Controller = com.ReceiveNext(s) as TouhouSha.Core.Player;
    }
}

public class ComPackageEventInUseCardState : ComPackage
{
    public override StartCode Code => StartCode.EventInUseCardState;

    public Event Event;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        com.SendNext(s, Event);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        Event = com.ReceiveNext(s) as Event;
    }
}

public class ComPackageHandMatch : ComPackage
{
    public override StartCode Code => StartCode.HandMatch;

    public TouhouSha.Core.Player Controller;
    public HandMatchGesture Result;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        com.SendNext(s, Controller);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        Controller = com.ReceiveNext(s) as TouhouSha.Core.Player;
    }
}

public class ComPackageHandMatchResult : ComPackage
{
    public override StartCode Code => StartCode.HandMatchResult;

    public HandMatchGesture Result;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext((int)Result);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        Result = (HandMatchGesture)(int)s.ReceiveNext();
    }
}

public class ComPackageKOF : ComPackage
{
    public enum ActionCode
    {
        Open,
        Control,
        Close,
    }

    public override StartCode Code => StartCode.KOF;

    public ActionCode Action;
    public TouhouSha.Core.Player Controller;
    public KOFSelectCore Core;
    public List<Charactor> SelectedCharactors = new List<Charactor>();

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext((int)Action);
        com.SendNext(s, Controller);
        com.SendNext(s, Core);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        Action = (ActionCode)(int)s.ReceiveNext();
        Controller = com.ReceiveNext(s) as TouhouSha.Core.Player;
        Core = com.ReceiveNext(s) as KOFSelectCore;
    }
}

public class ComPackageKOFResult : ComPackage
{
    public override StartCode Code => StartCode.KOFResult;

    public Charactor SelectedCharactor;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        com.SendNext(s, SelectedCharactor);   
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        SelectedCharactor = com.ReceiveNext(s) as Charactor;
    }
}