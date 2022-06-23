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

public class ComPackageDeterminePlayerCount : ComPackage
{
    public override StartCode Code => StartCode.DeterminePlayerCount;

    public int PlayerCount;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext(PlayerCount);
    }
    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        PlayerCount = (int)s.ReceiveNext();
    }
}

public class ComPackageDetermineChairIndex : ComPackage
{
    public override StartCode Code => StartCode.DetermineChairIndex;

    public readonly List<string> PunIds = new List<string>();

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext(PunIds.Count());
        foreach (string punid in PunIds)
            s.SendNext(punid);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        int n = (int)s.ReceiveNext();
        PunIds.Clear();
        while (n-- > 0)
            PunIds.Add((string)s.ReceiveNext());
    }
}

public class ComPackageBuildPlayers : ComPackage
{
    public override StartCode Code => StartCode.BuildPlayers;

    public readonly List<TouhouSha.Core.Player> Players = new List<TouhouSha.Core.Player>();

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext(Players.Count());
        foreach (TouhouSha.Core.Player player in Players)
            com.SendNext(s, player);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        int n = (int)s.ReceiveNext();
        Players.Clear();
        while (n-- > 0)
            Players.Add(com.ReceiveNext(s) as TouhouSha.Core.Player);
    }
}

public class ComPackageBuildCommonZones : ComPackage
{
    public override StartCode Code => StartCode.BuildCommonZones;

    public readonly List<Zone> Zones = new List<Zone>();

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext(Zones.Count());
        foreach (Zone zone in Zones)
            com.SendNext(s, zone);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        int n = (int)s.ReceiveNext();
        Zones.Clear();
        while (n-- > 0)
            Zones.Add(com.ReceiveNext(s) as Zone);
    }
}

public class ComPackageAllocAsses : ComPackage
{
    public override StartCode Code => StartCode.AllocAsses;

    public readonly List<TouhouSha.Core.Player> Players = new List<TouhouSha.Core.Player>();

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        s.SendNext(Players.Count());
        foreach (TouhouSha.Core.Player player in Players)
        {
            com.SendNext(s, player);
            s.SendNext((int)(player.Ass.E));
        }
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        int n = (int)s.ReceiveNext();
        Players.Clear();
        while (n-- > 0)
        {
            TouhouSha.Core.Player player = com.ReceiveNext(s) as TouhouSha.Core.Player;
            Players.Add(player);
            Enum_PlayerAss enumass = (Enum_PlayerAss)(int)s.ReceiveNext();
            PlayerAss ass = new PlayerAss(enumass);
            player.Ass = ass;
        }
    }

}

public class ComPackageLeaderSelectCharactor : ComPackage
{
    public override StartCode Code => StartCode.LeaderSelectCharactor;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {

    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {

    }
}

public class ComPackageAllSelectCharactor : ComPackage
{
    public override StartCode Code => StartCode.AllSelectCharactor;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {

    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {

    }
}

public class ComPackageBuildCardInstances : ComPackage
{
    public override StartCode Code => StartCode.BuildCardInstances;

    public readonly List<Card> Privates = new List<Card>();
    public readonly List<Card> Instances = new List<Card>();

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        com.SendNext(s, Privates);
        com.SendNext(s, Instances);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        Privates.Clear();
        Privates.AddRange(((IEnumerable)(com.ReceiveNext(s))).Cast<Card>());
        Instances.Clear();
        Instances.AddRange(((IEnumerable)(com.ReceiveNext(s))).Cast<Card>());
    }
}

public class ComPackageInitializeDrawZone : ComPackage
{
    public override StartCode Code => StartCode.InitializeDrawZone;

    public readonly List<Card> Cards = new List<Card>();

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        com.SendNext(s, Cards);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        Cards.Clear();
        Cards.AddRange(((IEnumerable)(com.ReceiveNext(s))).Cast<Card>());
    }
}

public class ComPackageBuildGlobalTrigger : ComPackage
{
    public override StartCode Code => StartCode.BuildGlobalTrigger;

    public readonly List<Trigger> TriggerBefores = new List<Trigger>();
    public readonly List<Trigger> TriggerAfters = new List<Trigger>();
    public readonly List<Calculator> CalculatorBefores = new List<Calculator>();
    public readonly List<Calculator> CalculatorAfters = new List<Calculator>();
    public readonly List<CardCalculator> CardCalculatorBefores = new List<CardCalculator>();
    public readonly List<CardCalculator> CardCalculatorAfters = new List<CardCalculator>();

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        com.SendNext(s, TriggerBefores);
        com.SendNext(s, TriggerAfters);
        com.SendNext(s, CalculatorBefores);
        com.SendNext(s, CalculatorAfters);
        com.SendNext(s, CardCalculatorBefores);
        com.SendNext(s, CardCalculatorAfters);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        TriggerBefores.Clear();
        TriggerBefores.AddRange(((IEnumerable)(com.ReceiveNext(s))).Cast<Trigger>());
        TriggerAfters.Clear();
        TriggerAfters.AddRange(((IEnumerable)(com.ReceiveNext(s))).Cast<Trigger>());
        CalculatorBefores.Clear();
        CalculatorBefores.AddRange(((IEnumerable)(com.ReceiveNext(s))).Cast<Calculator>());
        CalculatorAfters.Clear();
        CalculatorAfters.AddRange(((IEnumerable)(com.ReceiveNext(s))).Cast<Calculator>());
        CardCalculatorBefores.Clear();
        CardCalculatorBefores.AddRange(((IEnumerable)(com.ReceiveNext(s))).Cast<CardCalculator>());
        CardCalculatorAfters.Clear();
        CardCalculatorAfters.AddRange(((IEnumerable)(com.ReceiveNext(s))).Cast<CardCalculator>());
    }
}

public class ComPackageEnterState : ComPackage
{
    public override StartCode Code => StartCode.EnterState;

    public State State;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {
        com.SendNext(s, State);
    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {
        State = com.ReceiveNext(s) as State;
    }
}

public class ComPackageEnterEvent : ComPackage
{
    public override StartCode Code => StartCode.EnterEvent;

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

public class ComPackageLeaveStack : ComPackage
{
    public override StartCode Code => StartCode.LeaveStack;

    protected override void WriteOverride(PhotonStream s, GameCom com)
    {

    }

    protected override void ReadOverride(PhotonStream s, GameCom com)
    {

    }
}


}