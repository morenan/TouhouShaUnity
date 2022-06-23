using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Photon.Pun;
using Photon.Realtime;
using TouhouSha.Core;
using TouhouSha.Core.UIs;
using TouhouSha.Core.UIs.Events;
using UnityEngine;

public class GameCom : MonoBehaviourPunCallbacks, IPunObservable
{
    public enum StartCode
    {
        ComPackage,
        UIEvent,
        PropertySync,
        ShaPropertySync,
        StaticFieldSync,
        CollectionSync,
    }

    public DataPool DataPool = App.DefaultDataPool;
    public GameBoard GameBoard;
    public WorldServer WorldServer;
    public WorldClient WorldClient;

    private Queue<ComPackage> sendpacks = new Queue<ComPackage>();
    private Queue<UIEvent> senduievs = new Queue<UIEvent>();
    private Queue<ShaPropertySync> sendspsyncs = new Queue<ShaPropertySync>();
    private Queue<PropertySync> sendpcsyncs = new Queue<PropertySync>();
    private Queue<StaticFieldSync> sendsfsyncs = new Queue<StaticFieldSync>();
    private Queue<CollectionSync> sendclsyncs = new Queue<CollectionSync>();

    void Awake()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (photonView.IsMine)
            {
                WorldServer = new WorldServer();
                WorldServer.World = App.World;
                WorldServer.Writer = this;
            }
        }
        else
        {
            if (photonView.IsMine)
            {
                WorldClient = new WorldClient();
                WorldClient.World = App.World;
                foreach (GameCom com in gameObject.transform.parent.gameObject.GetComponentsInChildren<GameCom>())
                {
                    if (!com.photonView.Owner.IsMasterClient) continue;
                    WorldClient.Reader = com;
                    break;
                }
            }
            if (photonView.Owner.IsMasterClient)
            {
                foreach (GameCom com in gameObject.transform.parent.gameObject.GetComponentsInChildren<GameCom>())
                {
                    if (!com.photonView.IsMine) continue;
                    WorldClient.Reader = this;
                    break;
                }
            }
        }
        GameBoard = GameObject.Find("GameBoard")?.GetComponent<GameBoard>();
        if (GameBoard != null)
        {
            GameBoard.GameComs.Add(this);
            GameBoard.ConnectRemoteConsoles();
        }
    }

    public void SendNext(PhotonStream s, object obj)
    {
        if (obj is Type)
        {
            Type type = (Type)obj;
            int index = 0;
            if (DataPool.T2I.TryGetValue(type, out index))
            {
                s.SendNext((byte)(GameComCode.CSharpTypeIndiced));
                s.SendNext(index);
            }
            else
            {
                index = DataPool.T2I.Count();
                DataPool.I2T.Add(type);
                DataPool.T2I.Add(type, index);

                s.SendNext((byte)(GameComCode.CSharpTypeCreate));
                s.SendNext(index);
                s.SendNext(type.Name);
            }
        }
        else if (obj is TouhouSha.Core.Player)
        {
            TouhouSha.Core.Player player = (TouhouSha.Core.Player)obj;
            int index = 0;
            if (DataPool.P2I.TryGetValue(player, out index))
            {
                s.SendNext((byte)(GameComCode.PlayerIndiced));
                s.SendNext(index);
            }
            else
            {
                uint assbin = 0;
                if (player.Ass != null)
                {
                    assbin |= 1;
                    assbin |= ((uint)(player.Ass.E)) << 2;                    
                }
                if (player.IsAssVisibled)
                    assbin |= 2;

                index = DataPool.I2P.Count();
                DataPool.I2P.Add(player);
                DataPool.P2I.Add(player, index);
                WorldServer?.BeginListen(player);

                s.SendNext((byte)(GameComCode.PlayerCreate));
                s.SendNext(index);
                SendProperties(s, player);
                s.SendNext(player.KeyName);
                s.SendNext(player.Name);
                s.SendNext(player.Country);
                s.SendNext(assbin);
                s.SendNext(player.IsTrusteeship);

                SendNext(s, player.Charactors);
                SendNext(s, player.Skills);
                SendNext(s, player.Zones);
                SendNext(s, player.Symbols);
            }
        }
        else if (obj is Card)
        {
            Card card = (Card)obj;
            int index = 0;
            if (DataPool.C2I.TryGetValue(card, out index))
            {
                s.SendNext((byte)(GameComCode.CardIndiced));
                s.SendNext(index);
            }
            else
            {
                uint typebin = 0;
                if (card.CardType != null)
                {
                    typebin |= 1;
                    typebin |= ((uint)(card.CardType.E)) << 1;
                    if (card.CardType.SubType != null)
                    {
                        typebin |= 16;
                        typebin |= ((uint)(card.CardType.SubType.E)) << 5;
                    }
                }

                index = DataPool.I2C.Count();
                DataPool.I2C.Add(card);
                DataPool.C2I.Add(card, index);
                WorldServer?.BeginListen(card);

                s.SendNext((byte)(GameComCode.CardCreate));
                s.SendNext(index);
                SendNext(s, card.GetType());
                SendProperties(s, card);
                s.SendNext(typebin);
                s.SendNext((int)(card.CardColor?.E ?? Enum_CardColor.None));
                s.SendNext(card.CardPoint);

                SendNext(s, card.Zone);
                SendNext(s, card.GetExternZones());
            }
        }
        else if (obj is DesktopCardBoardCore)
        {
            DesktopCardBoardCore core = (DesktopCardBoardCore)obj;
            int index = 0;

            if (DataPool.DC2I.TryGetValue(core, out index))
            {
                s.SendNext((byte)(GameComCode.DesktopCoreIndiced));
                s.SendNext(index);
            }
            else
            {
                index = DataPool.DC2I.Count();
                DataPool.I2DC.Add(core);
                DataPool.DC2I.Add(core, index);
                WorldServer?.BeginListen(core);

                s.SendNext((byte)(GameComCode.DesktopCoreCreate));
                s.SendNext(index);
                s.SendNext(core.KeyName);
                s.SendNext(core.Message);
                s.SendNext(core.IsAsync);
                s.SendNext(core.Timeout);
                s.SendNext((int)(core.Flag));

                SendNext(s, core.Controller);
                SendNext(s, core.CardFilter);
                SendNext(s, core.Zones);
            }
        }
        else if (obj is DesktopCardBoardZone)
        {
            DesktopCardBoardZone zone = (DesktopCardBoardZone)obj;
            int index = 0;

            if (DataPool.DZ2I.TryGetValue(zone, out index))
            {
                s.SendNext((byte)(GameComCode.DesktopZoneIndiced));
                s.SendNext(index);
            }
            else
            {
                index = DataPool.DZ2I.Count();
                DataPool.I2DZ.Add(zone);
                DataPool.DZ2I.Add(zone, index);
                WorldServer?.BeginListen(zone);

                s.SendNext((byte)(GameComCode.DesktopZoneCreate));
                s.SendNext(index);
                SendNext(s, zone.Parent);
                SendProperties(s, zone);
                s.SendNext(zone.KeyName);
                s.SendNext(zone.Message);
                s.SendNext((int)(zone.Flag));

                SendNext(s, zone.ExternZones);
                SendNext(s, zone.Cards);
            }
        }
        else if (obj is Zone)
        {
            Zone zone = (Zone)obj;
            int index = 0;
            if (DataPool.Z2I.TryGetValue(zone, out index))
            {
                s.SendNext((byte)(GameComCode.ZoneIndiced));
                s.SendNext(index);
            }
            else
            {
                uint bin = 0;
                if (zone.AllowConverted) bin |= 1;
                if (zone.UseCardSkill) bin |= 2;

                index = DataPool.Z2I.Count();
                DataPool.I2Z.Add(zone);
                DataPool.Z2I.Add(zone, index);
                WorldServer?.BeginListen(zone);

                s.SendNext((byte)(GameComCode.ZoneCreate));
                s.SendNext(index);
                SendNext(s, zone.GetType());
                SendProperties(s, zone);
                s.SendNext(zone.KeyName);
                s.SendNext(bin);
                s.SendNext((int)(zone.Flag));

                SendNext(s, zone.Owner);
                SendNext(s, zone.ExternZones);
                SendNext(s, zone.Cards);
            }
        }
        else if (obj is ExternZone)
        {
            ExternZone zone = (ExternZone)obj;
            int index = 0;

            if (DataPool.EZ2I.TryGetValue(zone, out index))
            {
                s.SendNext((byte)(GameComCode.ExternZoneIndiced));
                s.SendNext(index);
            }
            else
            {
                index = DataPool.EZ2I.Count();
                DataPool.I2EZ.Add(zone);
                DataPool.EZ2I.Add(zone, index);
                WorldServer?.BeginListen(zone);

                s.SendNext((byte)(GameComCode.ExternZoneCreate));
                s.SendNext(index);
                SendProperties(s, zone);
                s.SendNext(zone.KeyName);
                s.SendNext((int)(zone.Flag));

                s.SendNext(zone.Owner);
                s.SendNext(zone.SourceZone);
                s.SendNext(zone.Cards);
            }
        }
        else if (obj is Symbol)
        {
            Symbol symbol = (Symbol)obj;
            int index = 0;

            if (DataPool.SY2I.TryGetValue(symbol, out index))
            {
                s.SendNext((byte)(GameComCode.SymbolIndiced));
                s.SendNext(index);
            }
            else
            {
                index = DataPool.SY2I.Count();
                DataPool.I2SY.Add(symbol);
                DataPool.SY2I.Add(symbol, index);
                WorldServer?.BeginListen(symbol);

                s.SendNext((byte)(GameComCode.SymbolCreate));
                s.SendNext(index);
                SendProperties(s, symbol);
                s.SendNext(symbol.KeyName);
                s.SendNext(symbol.Count);
            }
        }
        else if (obj is Charactor)
        {
            Charactor char0 = (Charactor)obj;
            int index = 0;

            if (DataPool.CA2I.TryGetValue(char0, out index))
            {
                s.SendNext((byte)(GameComCode.CharactorIndiced));
                s.SendNext(index);
            }
            else
            {
                index = DataPool.CA2I.Count();
                DataPool.I2CA.Add(char0);
                DataPool.CA2I.Add(char0, index);
                WorldServer?.BeginListen(char0);

                s.SendNext((byte)(GameComCode.CharactorCreate));
                s.SendNext(index);
                SendNext(s, char0.GetType());
            }
        }
        else if (obj is Skill)
        {
            Skill skill = (Skill)obj;
            int index = 0;

            if (DataPool.SK2I.TryGetValue(skill, out index))
            {
                s.SendNext((byte)(GameComCode.SkillIndiced));
                s.SendNext(index);
            }
            else
            {
                index = DataPool.SK2I.Count();
                DataPool.I2SK.Add(skill);
                DataPool.SK2I.Add(skill, index);
                WorldServer?.BeginListen(skill);

                s.SendNext((byte)(GameComCode.SkillCreate));
                s.SendNext(index);
                SendNext(s, skill.GetType());
                SendProperties(s, skill);
                SendNext(s, skill.Owner);
            }
        }
        else if (obj is Event)
        {
            Event ev = (Event)obj;
            s.SendNext((byte)(GameComCode.EventCreate));
            SendNext(s, ev.GetType());
            SendProperties(s, ev);
            SendShaSerialize(s, ev);
        }
        else if (obj is State)
        {
            State st = (State)obj;
            s.SendNext((byte)(GameComCode.StateCreate));
            SendProperties(s, st);
            s.SendNext(st.KeyName);
            s.SendNext(st.Step);
            SendNext(s, st.Owner);
            SendNext(s, st.Ev);
        }
        else if (obj is UIEvent)
        {
            UIEvent ev = (UIEvent)obj;
            s.SendNext((byte)(GameComCode.UIEventCreate));
            SendNext(s, ev.GetType());
            if (ev is UIEventFromLogical)
                SendNext(s, ((UIEventFromLogical)ev).LogicalEvent);
            if (ev is IShaSerialize)
                SendShaSerialize(s, (IShaSerialize)ev);
        }
        else if (obj is Filter)
        {
            Filter filter = (Filter)obj;
            int index = 0;

            if (DataPool.FT2I.TryGetValue(filter, out index))
            {
                s.SendNext((byte)(GameComCode.FilterIndiced));
                s.SendNext(index);
            }
            else
            {
                index = DataPool.FT2I.Count();
                DataPool.I2FT.Add(filter);
                DataPool.FT2I.Add(filter, index);
                WorldServer?.BeginListen(filter);

                s.SendNext((byte)(GameComCode.FilterCreate));
                s.SendNext(index);
                SendNext(s, filter.GetType());
                if (filter is IFilterFromSkill)
                    SendNext(s, ((IFilterFromSkill)filter).Skill);
                if (filter is IFilterFromCard)
                    SendNext(s, ((IFilterFromCard)filter).Card);
                SendProperties(s, filter);
                if (filter is IShaSerialize)
                    SendShaSerialize(s, (IShaSerialize)filter);
            }
        }
        else if (obj is Calculator)
        {
            Calculator calc = (Calculator)obj;
            int index = 0;

            if (DataPool.CC2I.TryGetValue(calc, out index))
            {
                s.SendNext((byte)(GameComCode.CalculatorIndiced));
                s.SendNext(index);
            }
            else
            {
                index = DataPool.CC2I.Count();
                DataPool.I2CC.Add(calc);
                DataPool.CC2I.Add(calc, index);
                WorldServer?.BeginListen(calc);

                s.SendNext((byte)(GameComCode.CalculatorCreate));
                s.SendNext(index);
                SendNext(s, calc.GetType());
                if (calc is ICalculatorFromSkill)
                    SendNext(s, ((ICalculatorFromSkill)calc).Skill);
                if (calc is ICalculatorFromCard)
                    SendNext(s, ((ICalculatorFromCard)calc).Card);
                SendProperties(s, calc);
                if (calc is IShaSerialize) 
                    SendShaSerialize(s, (IShaSerialize)calc);
            }
        }
        else if (obj is CardCalculator)
        {
            CardCalculator calc = (CardCalculator)obj;
            int index = 0;

            if (DataPool.CCC2I.TryGetValue(calc, out index))
            {
                s.SendNext((byte)(GameComCode.CardCalculatorIndiced));
                s.SendNext(index);
            }
            else
            {
                index = DataPool.CCC2I.Count();
                DataPool.I2CCC.Add(calc);
                DataPool.CCC2I.Add(calc, index);
                WorldServer?.BeginListen(calc);

                s.SendNext((byte)(GameComCode.CardCalculatorCreate));
                s.SendNext(index);
                SendNext(s, calc.GetType());
                if (calc is ICalculatorFromSkill)
                    SendNext(s, ((ICalculatorFromSkill)calc).Skill);
                if (calc is ICalculatorFromCard)
                    SendNext(s, ((ICalculatorFromCard)calc).Card);
                SendProperties(s, calc);
                if (calc is IShaSerialize) 
                    SendShaSerialize(s, (IShaSerialize)calc);
            }
        }
        else if (obj is Trigger)
        {
            Trigger trigger = (Trigger)obj;
            int index = 0;

            if (DataPool.TG2I.TryGetValue(trigger, out index))
            {
                s.SendNext((byte)(GameComCode.TriggerIndiced));
                s.SendNext(index);
            }
            else
            {
                index = DataPool.TG2I.Count();
                DataPool.I2TG.Add(trigger);
                DataPool.TG2I.Add(trigger, index);
                WorldServer?.BeginListen(trigger);

                s.SendNext((byte)(GameComCode.TriggerCreate));
                s.SendNext(index);
                SendNext(s, trigger.GetType());
                if (trigger is ISkillTrigger)
                    SendNext(s, ((ISkillTrigger)trigger).Skill);
                if (trigger is ICardTrigger)
                    SendNext(s, ((ICardTrigger)trigger).Card);
                SendProperties(s, trigger);
                if (trigger is IShaSerialize) 
                    SendShaSerialize(s, (IShaSerialize)trigger);
            }
        }
        else if (obj is KOFSelectCore)
        {
            KOFSelectCore core = (KOFSelectCore)obj;
            int index = 0;

            if (DataPool.KOF2I.TryGetValue(core, out index))
            {
                s.SendNext((byte)(GameComCode.KOFIndiced));
                s.SendNext(index);
            }
            else
            {
                index = DataPool.KOF2I.Count();
                DataPool.I2KOF.Add(core);
                DataPool.KOF2I.Add(core, index);
                WorldServer?.BeginListen(core);

                s.SendNext((byte)(GameComCode.KOFCreate));
                s.SendNext(index);
                s.SendNext(core.KeyName);
                s.SendNext(core.Message);
                s.SendNext(core.Timeout);
                s.SendNext(core.Hiddens);
                SendNext(s, core.Charactors);
                s.SendNext(core.PlayerSelecteds.Count());
                foreach (KeyValuePair<TouhouSha.Core.Player, List<Charactor>> kvp in core.PlayerSelecteds)
                {
                    SendNext(s, kvp.Key);
                    SendNext(s, kvp.Value);   
                }
            }
        }
        else if (obj is IEnumerable)
        {
            IEnumerable e = (IEnumerable)obj;
            s.SendNext((byte)(GameComCode.Enumerable));
            s.SendNext(e.Cast<object>().Count());
            foreach (object sub in e) SendNext(s, sub);
        }
        else if (obj is byte)
        {
            s.SendNext((byte)(GameComCode.Byte));
            s.SendNext((byte)(obj));
        }
        else
        {
            s.SendNext(obj);
        }
    }

    public void SendProperties(PhotonStream s, ShaObject obj)
    {
        IEnumerable<KeyValuePair<string, int>> values = obj.GetAllValues().Where(_kvp => _kvp.Value != 0).ToList();
        s.SendNext(values.Count());
        foreach (KeyValuePair<string, int> kvp in values)
        {
            s.SendNext(kvp.Key);
            s.SendNext(kvp.Value);
        }
    }

    public void SendShaSerialize(PhotonStream s, IShaSerialize serialize)
    {
        List<bool> bools = serialize.GetBools().ToList();
        List<int> ints = serialize.GetInts().ToList();
        List<uint> uints = serialize.GetUInts().ToList();
        List<ShaSerializeObject> subs = serialize.GetObjects().ToList();
        s.SendNext(bools.Count());
        foreach (bool b in bools) s.SendNext(b);
        s.SendNext(uints.Count());
        foreach (uint u in uints) s.SendNext(u);
        s.SendNext(ints.Count());
        foreach (int i in ints) s.SendNext(i);
        s.SendNext(subs.Count());
        foreach (ShaSerializeObject sub in subs) SendNext(s, sub.Value);
    }

    public object ReceiveNext(PhotonStream s)
    {
        object first = s.ReceiveNext();
        if (!(first is byte)) return first;
        GameComCode code = (GameComCode)first;
        switch (code)
        {
            case GameComCode.CSharpTypeIndiced:
            case GameComCode.CSharpTypeCreate:
                {
                    int index = (int)(s.ReceiveNext());
                    if (code == GameComCode.CSharpTypeIndiced)
                    {
                        if (index < 0) break;
                        if (index >= DataPool.I2T.Count()) break;
                        return DataPool.I2T[index];
                    }
                    else
                    {
                        Type type = null;
                        string typename = (string)s.ReceiveNext();
                        while (index >= DataPool.I2T.Count()) DataPool.I2T.Add(null);

                        if (Config.GameConfig.UsedPackages.Count() == 0)
                        {
                            Config.GameConfig.UsedPackages.Add(new TouhouSha.Koishi.Package());
                            Config.GameConfig.UsedPackages.Add(new TouhouSha.Koishi.Package2());
                            Config.GameConfig.UsedPackages.Add(new TouhouSha.Reimu.Package());
                        }
                        foreach (IPackage package in Config.GameConfig.UsedPackages)
                        {
                            type = package.GetTypeByFullName(typename);
                            if (type != null) break;
                        }
                        if (index >= 0)
                        {
                            DataPool.I2T[index] = type;
                            DataPool.T2I.Add(type, index);
                        }

                        return type;
                    }
                }
            case GameComCode.PlayerIndiced:
            case GameComCode.PlayerCreate:
                {
                    int index = (int)(s.ReceiveNext());
                    if (code == GameComCode.CSharpTypeIndiced)
                    {
                        if (index < 0) break;
                        if (index >= DataPool.I2P.Count()) break;
                        return DataPool.I2P[index];
                    }
                    else
                    {
                        TouhouSha.Core.Player player = new TouhouSha.Core.Player();
                        while (index >= DataPool.I2P.Count()) DataPool.I2P.Add(null);
                        if (index >= 0)
                        {
                            DataPool.I2P[index] = player;
                            DataPool.P2I.Add(player, index);
                        }

                        ReceiveProperties(s, player);
                        player.KeyName = (string)s.ReceiveNext();
                        player.Name = (string)s.ReceiveNext();
                        player.Country = (string)s.ReceiveNext();
                        uint assbin = (uint)s.ReceiveNext();
                        if ((assbin & 1) != 0) player.Ass = new PlayerAss((Enum_PlayerAss)(assbin >> 2));
                        player.IsAssVisibled = (assbin & 2) != 0;
                        player.IsTrusteeship = (bool)s.ReceiveNext();

                        List<Charactor> charactors = ((IEnumerable)(ReceiveNext(s))).Cast<Charactor>().ToList();
                        player.Charactors.Clear();
                        player.Charactors.AddRange(charactors);

                        List<Skill> skills = ((IEnumerable)(ReceiveNext(s))).Cast<Skill>().ToList();
                        player.Skills.Clear();
                        foreach (Skill skill in skills)
                            player.Skills.Add(skill);

                        List<Zone> zones = ((IEnumerable)(ReceiveNext(s))).Cast<Zone>().ToList();
                        player.Zones.Clear();
                        foreach (Zone zone in zones)
                            player.Zones.Add(zone);

                        List<Symbol> symbols = ((IEnumerable)(ReceiveNext(s))).Cast<Symbol>().ToList();
                        player.Symbols.Clear();
                        foreach (Symbol symbol in symbols)
                            player.Symbols.Add(symbol);

                        return player;
                    }
                }
            case GameComCode.CardIndiced:
            case GameComCode.CardCreate:
                {
                    int index = (int)(s.ReceiveNext());
                    if (code == GameComCode.CardIndiced)
                    {
                        if (index < 0) break;
                        if (index >= DataPool.I2C.Count()) break;
                        return DataPool.I2C[index];
                    }
                    else
                    {
                        Type type = (Type)ReceiveNext(s);
                        Card card = Create<Card>(type);
                        if (card == null) break;

                        while (index >= DataPool.I2C.Count()) DataPool.I2C.Add(null);
                        if (index >= 0)
                        {
                            DataPool.I2C[index] = card;
                            DataPool.C2I.Add(card, index);
                        }

                        ReceiveProperties(s, card);
                        uint typebin = (uint)s.ReceiveNext();
                        if ((typebin & 1) != 0)
                        {
                            Enum_CardType enumtype = (Enum_CardType)((typebin >> 1) & 7);
                            CardSubType subtype = null;
                            if ((typebin & 16) != 0)
                            {
                                Enum_CardSubType enumsub = (Enum_CardSubType)((typebin >> 5) & 7);
                                subtype = new CardSubType(enumsub);
                            }
                            card.CardType = new CardType(enumtype, subtype);
                        }
                        card.CardColor = new CardColor((Enum_CardColor)(int)s.ReceiveNext());
                        card.CardPoint = (int)s.ReceiveNext();

                        card.Zone = ReceiveNext(s) as Zone;
                        ReceiveNext(s);

                        return card;
                    }
                }
            case GameComCode.ZoneCreate:
            case GameComCode.ZoneIndiced:
                {
                    int index = (int)(s.ReceiveNext());
                    if (code == GameComCode.ZoneIndiced)
                    {
                        if (index < 0) break;
                        if (index >= DataPool.I2Z.Count()) break;
                        return DataPool.I2Z[index];
                    }
                    else
                    {
                        Type type = (Type)ReceiveNext(s);
                        Zone zone = Create<Zone>(type);
                        if (zone == null) zone = new Zone();
                        while (index >= DataPool.I2Z.Count()) DataPool.I2Z.Add(null);
                        if (index >= 0)
                        {
                            DataPool.I2Z[index] = zone;
                            DataPool.Z2I.Add(zone, index);
                        }

                        ReceiveProperties(s, zone);
                        zone.KeyName = (string)s.ReceiveNext();
                        uint bin = (uint)s.ReceiveNext();
                        zone.AllowConverted = (bin & 1) != 0;
                        zone.UseCardSkill = (bin & 2) != 0;
                        zone.Flag = (Enum_ZoneFlag)(int)s.ReceiveNext();
                        
                        zone.Owner = ReceiveNext(s) as TouhouSha.Core.Player;
                        
                        List<ExternZone> externs = ((IEnumerable)(ReceiveNext(s))).Cast<ExternZone>().ToList();
                        zone.ExternZones.Clear();
                        foreach (ExternZone ez in externs)
                            zone.ExternZones.Add(ez);
                        
                        List<Card> cards = ((IEnumerable)(ReceiveNext(s))).Cast<Card>().ToList();
                        foreach (Card card in cards)
                            zone.Add(card);

                        return zone;
                    }
                }
            case GameComCode.ExternZoneCreate:
            case GameComCode.ExternZoneIndiced:
                {
                    int index = (int)(s.ReceiveNext());
                    if (code == GameComCode.ExternZoneIndiced)
                    {
                        if (index < 0) break;
                        if (index >= DataPool.I2EZ.Count()) break;
                        return DataPool.I2EZ[index];
                    }
                    else
                    {
                        ExternZone zone = new ExternZone();
                        while (index >= DataPool.I2EZ.Count()) DataPool.I2EZ.Add(null);
                        if (index >= 0)
                        {
                            DataPool.I2EZ[index] = zone;
                            DataPool.EZ2I.Add(zone, index);
                        }
                        ReceiveProperties(s, zone);
                        zone.KeyName = (string)s.ReceiveNext();
                        zone.Flag = (Enum_ZoneFlag)(int)s.ReceiveNext();

                        zone.Owner = s.ReceiveNext() as TouhouSha.Core.Player;
                        zone.SourceZone = s.ReceiveNext() as Zone;

                        List<Card> cards = ((IEnumerable)(ReceiveNext(s))).Cast<Card>().ToList();
                        zone.Cards.Clear();
                        foreach (Card card in cards)
                            zone.Cards.Add(card);
                        return zone;
                    }
                }
            case GameComCode.DesktopCoreCreate:
            case GameComCode.DesktopCoreIndiced:
                {
                    int index = (int)(s.ReceiveNext());
                    if (code == GameComCode.DesktopCoreIndiced)
                    {
                        if (index < 0) break;
                        if (index >= DataPool.I2DC.Count()) break;
                        return DataPool.I2DC[index];
                    }
                    else
                    {
                        DesktopCardBoardCore core = new DesktopCardBoardCore();

                        while (index >= DataPool.I2DC.Count()) DataPool.I2DC.Add(null);
                        if (index >= 0)
                        {
                            DataPool.I2DC[index] = core;
                            DataPool.DC2I.Add(core, index);
                        }

                        core.KeyName = (string)s.ReceiveNext();
                        core.Message = (string)s.ReceiveNext();
                        core.IsAsync = (bool)s.ReceiveNext();
                        core.Timeout = (int)s.ReceiveNext();
                        core.Flag = (Enum_DesktopCardBoardFlag)(int)s.ReceiveNext();

                        core.Controller = ReceiveNext(s) as TouhouSha.Core.Player;
                        core.CardFilter = ReceiveNext(s) as CardFilter;
                        core.Zones.Clear();
                        foreach (DesktopCardBoardZone zone in ((IEnumerable)(ReceiveNext(s))))
                            core.Zones.Add(zone);
                    }

                    break;
                }
            case GameComCode.DesktopZoneCreate:
            case GameComCode.DesktopZoneIndiced:
                {
                    int index = (int)(s.ReceiveNext());
                    if (code == GameComCode.DesktopZoneIndiced)
                    {
                        if (index < 0) break;
                        if (index >= DataPool.I2DZ.Count()) break;
                        return DataPool.I2DZ[index];
                    }
                    else
                    {
                        DesktopCardBoardCore core = ReceiveNext(s) as DesktopCardBoardCore;
                        DesktopCardBoardZone zone = new DesktopCardBoardZone(core);
                        while (index >= DataPool.I2DZ.Count()) DataPool.I2DZ.Add(null);
                        if (index >= 0)
                        {
                            DataPool.I2DZ[index] = zone;
                            DataPool.DZ2I.Add(zone, index);
                        }
                        ReceiveProperties(s, zone);
                        zone.KeyName = (string)s.ReceiveNext();
                        zone.Message = (string)s.ReceiveNext();
                        zone.Flag = (Enum_DesktopZoneFlag)(int)s.ReceiveNext();
                        
                        List<ExternZone> externs = ((IEnumerable)(ReceiveNext(s))).Cast<ExternZone>().ToList();
                        zone.ExternZones.Clear();
                        foreach (ExternZone ez in externs)
                            zone.ExternZones.Add(ez);

                        List<Card> cards = ((IEnumerable)(ReceiveNext(s))).Cast<Card>().ToList();
                        foreach (Card card in cards)
                        {
                            if ((core.Flag & Enum_DesktopCardBoardFlag.CardInBoard) != Enum_DesktopCardBoardFlag.None)
                                zone.Add(card);
                            else
                                zone.Cards.Add(card);
                        }
                        return zone;
                    }
                }
            case GameComCode.SymbolCreate:
            case GameComCode.SymbolIndiced:
                {
                    int index = (int)(s.ReceiveNext());
                    if (code == GameComCode.SymbolIndiced)
                    {
                        if (index < 0) break;
                        if (index >= DataPool.I2SY.Count()) break;
                        return DataPool.I2SY[index];
                    }
                    else
                    {
                        Symbol symbol = new Symbol();
                        while (index >= DataPool.I2SY.Count()) DataPool.I2SY.Add(null);
                        if (index >= 0)
                        {
                            DataPool.I2SY[index] = symbol;
                            DataPool.SY2I.Add(symbol, index);
                        }
                        ReceiveProperties(s, symbol);
                        symbol.KeyName = (string)s.ReceiveNext();
                        symbol.Count = (int)s.ReceiveNext();

                        return symbol;
                    }
                }
            case GameComCode.CharactorCreate:
            case GameComCode.CharactorIndiced:
                {
                    int index = (int)s.ReceiveNext();
                    if (code == GameComCode.CharactorIndiced)
                    {
                        if (index < 0) break;
                        if (index >= DataPool.I2CA.Count()) break;
                        return DataPool.I2CA[index];
                    }
                    else
                    {
                        Type type = (Type)ReceiveNext(s);
                        Charactor char0 = Create<Charactor>(type);
                        if (char0 == null) break;

                        while (index >= DataPool.I2CA.Count()) DataPool.I2CA.Add(null);
                        if (index >= 0)
                        {
                            DataPool.I2CA[index] = char0;
                            DataPool.CA2I.Add(char0, index);
                        }
                        ReceiveProperties(s, char0);

                        return char0;
                    }
                }
            case GameComCode.SkillCreate:
            case GameComCode.SkillIndiced:
                {
                    int index = (int)s.ReceiveNext();
                    if (code == GameComCode.SkillIndiced)
                    {
                        if (index < 0) break;
                        if (index >= DataPool.I2SK.Count()) break;
                        return DataPool.I2SK[index];
                    }
                    else
                    {
                        Type type = (Type)ReceiveNext(s);
                        Skill skill = Create<Skill>(type);
                        if (skill == null) break;

                        while (index >= DataPool.I2SK.Count()) DataPool.I2SK.Add(null);
                        if (index >= 0)
                        {
                            DataPool.I2SK[index] = skill;
                            DataPool.SK2I.Add(skill, index);
                        }

                        ReceiveProperties(s, skill);
                        skill.Owner = ReceiveNext(s) as TouhouSha.Core.Player;

                        return skill;
                    }
                }
            case GameComCode.EventCreate:
                {
                    Type type = (Type)ReceiveNext(s);
                    Event ev = Create<Event>(type);
                    if (ev == null) break;
                    ReceiveProperties(s, ev);
                    ReceiveShaSerialize(s, ev);
                    return ev;
                }
            case GameComCode.StateCreate:
                {
                    State st = new State();
                    ReceiveProperties(s, st);
                    st.KeyName = (string)s.ReceiveNext();
                    st.Step = (int)s.ReceiveNext();
                    st.Owner = ReceiveNext(s) as TouhouSha.Core.Player;
                    st.Ev = ReceiveNext(s) as Event;
                    return st;
                }
            case GameComCode.UIEventCreate:
                {
                    Type type = (Type)ReceiveNext(s);
                    UIEvent ev = Create<UIEvent>(type);
                    if (ev == null) break;
                    if (ev is UIEventFromLogical)
                        ((UIEventFromLogical)ev).LogicalEvent = ReceiveNext(s) as Event;
                    if (ev is IShaSerialize)
                        ReceiveShaSerialize(s, (IShaSerialize)ev);
                    return ev;
                }
            case GameComCode.FilterCreate:
            case GameComCode.FilterIndiced:
                {
                    int index = (int)s.ReceiveNext();
                    if (code == GameComCode.FilterIndiced)
                    {
                        if (index < 0) break;
                        if (index >= DataPool.I2FT.Count()) break;
                        return DataPool.I2FT[index];
                    }
                    else
                    {
                        Type type = (Type)ReceiveNext(s);
                        Skill skill = null;
                        Card card = null;
                        Filter filter = null;
                        if (typeof(IFilterFromSkill).IsAssignableFrom(type))
                            skill = ReceiveNext(s) as Skill;
                        if (typeof(IFilterFromCard).IsAssignableFrom(type))
                            card = ReceiveNext(s) as Card;
                        if (skill != null && card != null)
                            filter = CreateFilterFromSkillAndCard(type, skill, card);
                        else if (skill != null)
                            filter = CreateFilterFromSkill(type, skill);
                        else if (card != null)
                            filter = CreateFilterFromCard(type, card);
                        else
                            filter = Create<Filter>(type);
                        if (filter == null) filter = new Filter();

                        while (index >= DataPool.I2FT.Count()) DataPool.I2FT.Add(null);
                        if (index >= 0)
                        {
                            DataPool.I2FT[index] = filter;
                            DataPool.FT2I.Add(filter, index);
                        }

                        ReceiveProperties(s, filter);
                        if (filter is IShaSerialize)
                            ReceiveShaSerialize(s, (IShaSerialize)filter);
                        return filter;
                    }
                }
            case GameComCode.CalculatorCreate:
            case GameComCode.CalculatorIndiced:
                {
                    int index = (int)s.ReceiveNext();
                    if (code == GameComCode.CalculatorIndiced)
                    {
                        if (index < 0) break;
                        if (index >= DataPool.I2CC.Count()) break;
                        return DataPool.I2CC[index];
                    }
                    else
                    {
                        Type type = (Type)ReceiveNext(s);
                        Skill skill = null;
                        Card card = null;
                        Calculator calc = null;
                        if (typeof(IFilterFromSkill).IsAssignableFrom(type))
                            skill = ReceiveNext(s) as Skill;
                        if (typeof(IFilterFromCard).IsAssignableFrom(type))
                            card = ReceiveNext(s) as Card;
                        if (skill != null && card != null)
                            calc = CreateCalculatorFromSkillAndCard(type, skill, card);
                        else if (skill != null)
                            calc = CreateCalculatorFromSkill(type, skill);
                        else if (card != null)
                            calc = CreateCalculatorFromCard(type, card);
                        else
                            calc = Create<Calculator>(type);
                        if (calc == null) calc = new Calculator();

                        while (index >= DataPool.I2CC.Count()) DataPool.I2CC.Add(null);
                        if (index >= 0)
                        {
                            DataPool.I2CC[index] = calc;
                            DataPool.CC2I.Add(calc, index);
                        }

                        ReceiveProperties(s, calc);
                        if (calc is IShaSerialize)
                            ReceiveShaSerialize(s, (IShaSerialize)calc);
                        return calc;
                    }
                }
            case GameComCode.CardCalculatorCreate:
            case GameComCode.CardCalculatorIndiced:
                {
                    int index = (int)s.ReceiveNext();
                    if (code == GameComCode.CardCalculatorIndiced)
                    {
                        if (index < 0) break;
                        if (index >= DataPool.I2CCC.Count()) break;
                        return DataPool.I2CCC[index];
                    }
                    else
                    {
                        Type type = (Type)ReceiveNext(s);
                        Skill skill = null;
                        Card card = null;
                        CardCalculator calc = null;
                        if (typeof(IFilterFromSkill).IsAssignableFrom(type))
                            skill = ReceiveNext(s) as Skill;
                        if (typeof(IFilterFromCard).IsAssignableFrom(type))
                            card = ReceiveNext(s) as Card;
                        if (skill != null && card != null)
                            calc = CreateCardCalculatorFromSkillAndCard(type, skill, card);
                        else if (skill != null)
                            calc = CreateCardCalculatorFromSkill(type, skill);
                        else if (card != null)
                            calc = CreateCardCalculatorFromCard(type, card);
                        else
                            calc = Create<CardCalculator>(type);
                        if (calc == null) calc = new CardCalculator();

                        while (index >= DataPool.I2CCC.Count()) DataPool.I2CCC.Add(null);
                        if (index >= 0)
                        {
                            DataPool.I2CCC[index] = calc;
                            DataPool.CCC2I.Add(calc, index);
                        }

                        ReceiveProperties(s, calc);
                        if (calc is IShaSerialize)
                            ReceiveShaSerialize(s, (IShaSerialize)calc);
                        return calc;
                    }
                }
            case GameComCode.TriggerCreate:
            case GameComCode.TriggerIndiced:
                {
                    int index = (int)s.ReceiveNext();
                    if (code == GameComCode.TriggerIndiced)
                    {
                        if (index < 0) break;
                        if (index >= DataPool.I2TG.Count()) break;
                        return DataPool.I2TG[index];
                    }
                    else
                    {
                        Type type = (Type)ReceiveNext(s);
                        Skill skill = null;
                        Card card = null;
                        Trigger trigger = null;
                        if (typeof(ISkillTrigger).IsAssignableFrom(type))
                            skill = ReceiveNext(s) as Skill;
                        if (typeof(ICardTrigger).IsAssignableFrom(type))
                            card = ReceiveNext(s) as Card;
                        if (skill != null && card != null)
                            trigger = CreateTriggerFromSkillAndCard(type, skill, card);
                        else if (skill != null)
                            trigger = CreateTriggerFromSkill(type, skill);
                        else if (card != null)
                            trigger = CreateTriggerFromCard(type, card);
                        else
                            trigger = Create<Trigger>(type);
                        if (trigger == null) trigger = new Trigger();

                        while (index >= DataPool.I2TG.Count()) DataPool.I2TG.Add(null);
                        if (index >= 0)
                        {
                            DataPool.I2TG[index] = trigger;
                            DataPool.TG2I.Add(trigger, index);
                        }

                        ReceiveProperties(s, trigger);
                        if (trigger is IShaSerialize)
                            ReceiveShaSerialize(s, (IShaSerialize)trigger);
                        return trigger;
                    }
                }
            case GameComCode.KOFCreate:
            case GameComCode.KOFIndiced:
                {
                    int index = (int)(s.ReceiveNext());
                    if (code == GameComCode.KOFIndiced)
                    {
                        if (index < 0) break;
                        if (index >= DataPool.I2KOF.Count()) break;
                        return DataPool.I2KOF[index];
                    }
                    else
                    {
                        KOFSelectCore core =new KOFSelectCore();
                        while (index >= DataPool.I2KOF.Count()) DataPool.I2KOF.Add(null);
                        if (index >= 0)
                        {
                            DataPool.I2KOF[index] = core;
                            DataPool.KOF2I.Add(core, index);
                        }
                        core.KeyName = (string)s.ReceiveNext();
                        core.Message = (string)s.ReceiveNext();
                        core.Timeout = (int)s.ReceiveNext();
                        core.Hiddens = (int)s.ReceiveNext();

                        List<Charactor> chars = ((IEnumerable)(ReceiveNext(s))).Cast<Charactor>().ToList();
                        core.Charactors.Clear();
                        core.Charactors.AddRange(chars);

                        int n = (int)s.ReceiveNext();
                        core.PlayerSelecteds.Clear();
                        while (n-- > 0)
                        {
                            TouhouSha.Core.Player player = ReceiveNext(s) as TouhouSha.Core.Player;
                            List<Charactor> charlist = ((IEnumerable)(ReceiveNext(s))).Cast<Charactor>().ToList();
                            core.PlayerSelecteds.Add(player, charlist);
                        }

                        return core;
                    }
                }
            case GameComCode.Enumerable:
                {
                    int n = (int)s.ReceiveNext();
                    List<object> list = new List<object>();
                    while (n-- > 0) list.Add(ReceiveNext(s));
                    return list;
                }
            case GameComCode.Byte:
                return (byte)s.ReceiveNext();
        }
        return null;
    }

    public void ReceiveProperties(PhotonStream s, ShaObject obj)
    {
        int n = (int)s.ReceiveNext();
        obj.ClearAllValues(null);
        while (n-- > 0)
        {
            string key = (string)s.ReceiveNext();
            int value = (int)s.ReceiveNext();
            obj.SetValue(key, value);
        }
    }

    public void ReceiveShaSerialize(PhotonStream s, IShaSerialize serialize)
    {
        List<bool> bools = new List<bool>();
        List<int> ints = new List<int>();
        List<uint> uints = new List<uint>();
        List<ShaSerializeObject> subs = new List<ShaSerializeObject>();
        int n = (int)s.ReceiveNext();
        while (n-- > 0) bools.Add((bool)(s.ReceiveNext()));
        n = (int)s.ReceiveNext();
        while (n-- > 0) uints.Add((uint)(s.ReceiveNext()));
        n = (int)s.ReceiveNext();
        while (n-- > 0) ints.Add((int)(s.ReceiveNext()));
        n = (int)s.ReceiveNext();
        while (n-- > 0) subs.Add(ReceiveShaSerializeObject(s));
        serialize.SetBools(bools);
        serialize.SetInts(ints);
        serialize.SetUInts(uints);
        serialize.SetObjects(subs);
    }

    public ShaSerializeObject ReceiveShaSerializeObject(PhotonStream s)
    {
        object value = ReceiveNext(s);
        bool islist = false;
        Type shatype = null;
        if (value is TouhouSha.Core.Player)
            shatype = typeof(TouhouSha.Core.Player);
        else if (value is Card)
            shatype = typeof(Card);
        else if (value is Zone)
            shatype = typeof(Zone);
        else if (value is ExternZone)
            shatype = typeof(ExternZone);
        else if (value is Symbol)
            shatype = typeof(Symbol);
        else if (value is Charactor)
            shatype = typeof(Charactor);
        else if (value is Skill)
            shatype = typeof(Skill);
        else if (value is Filter)
            shatype = typeof(Filter);
        else if (value is Calculator)
            shatype = typeof(Calculator);
        else if (value is CardCalculator)
            shatype = typeof(CardCalculator);
        else if (value is IEnumerable)
        {
            islist = true;
            shatype = typeof(IList);
        }
        else
            shatype = value?.GetType();
        ShaSerializeObject shaobj = null;
        if (islist)
            shaobj = new ShaSerializeObject(shatype, ((IEnumerable)value).Cast<object>().ToList());
        else
            shaobj = new ShaSerializeObject(shatype, shaobj);
        return shaobj;
    }

    public T Create<T>(Type type)
    {
        if (!typeof(T).IsAssignableFrom(type)) return default(T);
        foreach (ConstructorInfo ci in type.GetConstructors())
        {
            if (ci.GetParameters().Length > 0) continue;
            return (T)ci.Invoke(new object[] { });
        }
        return default(T);
    }

    public Filter CreateFilterFromSkillAndCard(Type type, Skill skill, Card card)
    {
        return Create<Filter>(type);
    }

    public Filter CreateFilterFromSkill(Type type, Skill skill)
    {
        if (!typeof(Filter).IsAssignableFrom(type)) return null;
        foreach (ConstructorInfo ci in type.GetConstructors())
        {
            if (ci.GetParameters().Length != 1) continue;
            if (!ci.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(Skill))) continue;  
            return (Filter)ci.Invoke(new object[] { skill });
        }
        foreach (ConstructorInfo ci in type.GetConstructors())
        {
            if (ci.GetParameters().Length > 0) continue;
            return (Filter)ci.Invoke(new object[] { });
        }
        return null;
    }

    public Filter CreateFilterFromCard(Type type, Card card)
    {
        if (!typeof(Filter).IsAssignableFrom(type)) return null;
        foreach (ConstructorInfo ci in type.GetConstructors())
        {
            if (ci.GetParameters().Length != 1) continue;
            if (!ci.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(Card))) continue;
            return (Filter)ci.Invoke(new object[] { card });
        }
        foreach (ConstructorInfo ci in type.GetConstructors())
        {
            if (ci.GetParameters().Length > 0) continue;
            return (Filter)ci.Invoke(new object[] { });
        }
        return null;
    }

    public Calculator CreateCalculatorFromSkillAndCard(Type type, Skill skill, Card card)
    {
        return Create<Calculator>(type);
    }

    public Calculator CreateCalculatorFromSkill(Type type, Skill skill)
    {
        if (!typeof(Calculator).IsAssignableFrom(type)) return null;
        foreach (ConstructorInfo ci in type.GetConstructors())
        {
            if (ci.GetParameters().Length != 1) continue;
            if (!ci.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(Skill))) continue;
            return (Calculator)ci.Invoke(new object[] { skill });
        }
        foreach (ConstructorInfo ci in type.GetConstructors())
        {
            if (ci.GetParameters().Length > 0) continue;
            return (Calculator)ci.Invoke(new object[] { });
        }
        return null;
    }

    public Calculator CreateCalculatorFromCard(Type type, Card card)
    {
        if (!typeof(Calculator).IsAssignableFrom(type)) return null;
        foreach (ConstructorInfo ci in type.GetConstructors())
        {
            if (ci.GetParameters().Length != 1) continue;
            if (!ci.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(Card))) continue;
            return (Calculator)ci.Invoke(new object[] { card });
        }
        foreach (ConstructorInfo ci in type.GetConstructors())
        {
            if (ci.GetParameters().Length > 0) continue;
            return (Calculator)ci.Invoke(new object[] { });
        }
        return null;
    }

    public CardCalculator CreateCardCalculatorFromSkillAndCard(Type type, Skill skill, Card card)
    {
        return Create<CardCalculator>(type);
    }

    public CardCalculator CreateCardCalculatorFromSkill(Type type, Skill skill)
    {
        if (!typeof(CardCalculator).IsAssignableFrom(type)) return null;
        foreach (ConstructorInfo ci in type.GetConstructors())
        {
            if (ci.GetParameters().Length != 1) continue;
            if (!ci.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(Skill))) continue;
            return (CardCalculator)ci.Invoke(new object[] { skill });
        }
        foreach (ConstructorInfo ci in type.GetConstructors())
        {
            if (ci.GetParameters().Length > 0) continue;
            return (CardCalculator)ci.Invoke(new object[] { });
        }
        return null;
    }

    public CardCalculator CreateCardCalculatorFromCard(Type type, Card card)
    {
        if (!typeof(CardCalculator).IsAssignableFrom(type)) return null;
        foreach (ConstructorInfo ci in type.GetConstructors())
        {
            if (ci.GetParameters().Length != 1) continue;
            if (!ci.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(Card))) continue;
            return (CardCalculator)ci.Invoke(new object[] { card });
        }
        foreach (ConstructorInfo ci in type.GetConstructors())
        {
            if (ci.GetParameters().Length > 0) continue;
            return (CardCalculator)ci.Invoke(new object[] { });
        }
        return null;
    }

    public Trigger CreateTriggerFromSkillAndCard(Type type, Skill skill, Card card)
    {
        return Create<Trigger>(type);
    }

    public Trigger CreateTriggerFromSkill(Type type, Skill skill)
    {
        if (!typeof(Trigger).IsAssignableFrom(type)) return null;
        foreach (ConstructorInfo ci in type.GetConstructors())
        {
            if (ci.GetParameters().Length != 1) continue;
            if (!ci.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(Skill))) continue;
            return (Trigger)ci.Invoke(new object[] { skill });
        }
        foreach (ConstructorInfo ci in type.GetConstructors())
        {
            if (ci.GetParameters().Length > 0) continue;
            return (Trigger)ci.Invoke(new object[] { });
        }
        return null;
    }

    public Trigger CreateTriggerFromCard(Type type, Card card)
    {
        if (!typeof(Trigger).IsAssignableFrom(type)) return null;
        foreach (ConstructorInfo ci in type.GetConstructors())
        {
            if (ci.GetParameters().Length != 1) continue;
            if (!ci.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(Card))) continue;
            return (Trigger)ci.Invoke(new object[] { card });
        }
        foreach (ConstructorInfo ci in type.GetConstructors())
        {
            if (ci.GetParameters().Length > 0) continue;
            return (Trigger)ci.Invoke(new object[] { });
        }
        return null;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            ShaPropertySync sync0 = default(ShaPropertySync);
            lock (sendspsyncs)
                if (sendspsyncs.Count() > 0)
                    sync0 = sendspsyncs.Dequeue();
            if (sync0.Owner != null)
            {
                stream.SendNext((byte)(StartCode.ShaPropertySync));
                SendNext(stream, sync0.Owner);
                stream.SendNext(sync0.PropertyName);
                SendNext(stream, sync0.NewValue);
                return;
            }

            PropertySync sync1 = default(PropertySync);
            lock (sendpcsyncs)
                if (sendpcsyncs.Count() > 0)
                    sync1 = sendpcsyncs.Dequeue();
            if (sync1.Owner != null)
            {
                stream.SendNext((byte)(StartCode.PropertySync));
                SendNext(stream, sync1.Owner);
                stream.SendNext(sync1.PropertyName);
                SendNext(stream, sync1.NewValue);
                return;
            }

            StaticFieldSync sync2 = default(StaticFieldSync);
            lock (sendsfsyncs)
                if (sendsfsyncs.Count() > 0)
                    sync2 = sendsfsyncs.Dequeue();
            if (sync2.Type != null)
            {
                stream.SendNext((byte)(StartCode.StaticFieldSync));
                SendNext(stream, sync2.Type);
                stream.SendNext(sync2.FieldName);
                SendNext(stream, sync2.NewValue);
                return;
            }

            CollectionSync sync3 = default(CollectionSync);
            lock (sendclsyncs)
                if (sendclsyncs.Count() > 0)
                    sync3 = sendclsyncs.Dequeue();
            if (sync3.Owner != null)
            {
                stream.SendNext((byte)(StartCode.CollectionSync));
                SendNext(stream, sync3.Owner);
                stream.SendNext(sync3.CollectionName);
                stream.SendNext((int)(sync3.Event.Action));
                SendNext(stream, sync3.Event.OldItems ?? new object[] { });
                stream.SendNext(sync3.Event.OldStartingIndex);
                SendNext(stream, sync3.Event.NewItems ?? new object[] { });
                stream.SendNext(sync3.Event.NewStartingIndex);
            }

            UIEvent ev = null;
            lock (senduievs)
                if (senduievs.Count() > 0)
                    ev = senduievs.Dequeue();
            if (ev != null)
            {
                stream.SendNext((byte)(StartCode.UIEvent));
                SendNext(stream, ev);
                return;
            }

            ComPackage pack = null;
            lock (sendpacks)
                if (sendpacks.Count() > 0)
                    pack = sendpacks.Dequeue();
            if (pack != null)
            {
                stream.SendNext((byte)(StartCode.ComPackage));
                ComPackage.Write(stream, this, pack);
                return;
            }
        }
        else
        {
            StartCode code = (StartCode)(byte)stream.ReceiveNext();
            switch (code)
            {
                case StartCode.PropertySync:
                    {
                        object owner = ReceiveNext(stream);
                        string propname = (string)(stream.ReceiveNext());
                        object value = ReceiveNext(stream);
                        if (owner == null) break;
                        PropertyInfo pi = owner.GetType().GetProperty(propname);
                        if (pi == null) break;
                        pi.SetValue(owner, value);
                        break;
                    }
                case StartCode.ShaPropertySync:
                    {
                        ShaObject owner = ReceiveNext(stream) as ShaObject;
                        string propname = (string)(stream.ReceiveNext());
                        int value = (int)(stream.ReceiveNext());
                        if (owner == null) break;
                        owner.SetValue(propname, value);
                        break;
                    }
                case StartCode.CollectionSync:
                    {
                        object owner = ReceiveNext(stream);
                        string collname = (string)(stream.ReceiveNext());
                        NotifyCollectionChangedAction action = (NotifyCollectionChangedAction)(int)(stream.ReceiveNext());
                        IList olditems = (IList)(ReceiveNext(stream));
                        int oldindex = (int)stream.ReceiveNext();
                        IList newitems = (IList)(ReceiveNext(stream));
                        int newindex = (int)stream.ReceiveNext();
                        PropertyInfo pi = owner.GetType().GetProperty(collname);
                        if (pi == null) break;
                        IList list = pi.GetValue(owner) as IList;
                        if (list == null) break;
                        switch (action)
                        {
                            case NotifyCollectionChangedAction.Reset: 
                                list.Clear();
                                break;
                            default:
                                foreach (object item in olditems)
                                    list.Remove(item);
                                foreach (object item in newitems.Cast<object>().Reverse())
                                    list.Insert(newindex, item);
                                break;
                        }
                        break;
                    }
                case StartCode.StaticFieldSync:
                    {
                        Type type = ReceiveNext(stream) as Type;
                        string fieldname = (string)(stream.ReceiveNext());
                        object value = ReceiveNext(stream);
                        if (type == null) break;
                        PropertyInfo pi = type.GetProperty(fieldname);
                        if (pi != null)
                        {
                            pi.SetValue(null, value);
                            break;
                        }
                        FieldInfo fi = type.GetField(fieldname);
                        if (fi != null)
                        {
                            fi.SetValue(null, value);
                            break;
                        }
                        break;
                    }
                case StartCode.UIEvent:
                    {
                        UIEvent ev = ReceiveNext(stream) as UIEvent;
                        if (ev == null) break;
                        UIEventReceivedEventArgs evrecv = new UIEventReceivedEventArgs(ev);
                        UIEventReceived?.Invoke(this, evrecv);
                        break;
                    }
                case StartCode.ComPackage:
                    {
                        ComPackage pack = ComPackage.Read(stream, this);
                        if (pack == null) break;
                        ComPackageReceivedEventArgs evpack = new ComPackageReceivedEventArgs(pack);
                        PackageReceived?.Invoke(this, evpack);
                        break;
                    }
            }
        }
    }

    public void SendPackage(ComPackage pack)
    {
        lock (sendpacks) sendpacks.Enqueue(pack);
    }

    public void SendUIEvent(UIEvent ev)
    {
        lock (senduievs) senduievs.Enqueue(ev);
    }

    public void SyncShaProperty(ShaObject obj, string propname)
    {
        SyncShaProperty(obj, propname, obj.GetValue(propname));
    }
    public void SyncShaProperty(ShaObject obj, string propname, int newvalue)
    {
        ShaPropertySync sync = new ShaPropertySync();
        sync.Owner = obj;
        sync.PropertyName = propname;
        sync.NewValue = newvalue;
        lock (sendspsyncs) sendspsyncs.Enqueue(sync);
    }

    public void SyncProperty(object obj, string propname)
    {
        PropertySync sync = new PropertySync();
        PropertyInfo pi = obj.GetType().GetProperty(propname);
        sync.Owner = obj;
        sync.PropertyName = propname;
        sync.NewValue = pi.GetValue(obj);
        lock (sendpcsyncs) sendpcsyncs.Enqueue(sync);
    }

    public void SyncStaticField(Type type, string fieldname)
    {
        StaticFieldSync sync = new StaticFieldSync();
        PropertyInfo pi = type.GetProperty(fieldname);
        FieldInfo fi = type.GetField(fieldname);
        sync.Type = type;
        sync.FieldName = fieldname;
        if (pi != null)
            sync.NewValue = pi.GetValue(null);
        else if (fi != null)
            sync.NewValue = fi.GetValue(null);
        lock (sendsfsyncs) sendsfsyncs.Enqueue(sync);
    }

    public void SyncCollection(object obj, string collname, NotifyCollectionChangedEventArgs ev)
    {
        CollectionSync sync = new CollectionSync();
        sync.Owner = obj;
        sync.CollectionName = collname;
        sync.Event = ev;
        lock (sendclsyncs) sendclsyncs.Enqueue(sync);
    }

    public event UIEventReceivedEventHandler UIEventReceived;
    
    public event ComPackageReceivedEventHandler PackageReceived;

}

public enum GameComCode
{
    Byte,
    Enumerable,

    CSharpTypeIndiced,
    CSharpTypeCreate,
    PlayerIndiced,
    PlayerCreate,
    CardIndiced,
    CardCreate,
    ZoneIndiced,
    ZoneCreate,
    ExternZoneIndiced,
    ExternZoneCreate,
    SymbolIndiced,
    SymbolCreate,
    CharactorIndiced,
    CharactorCreate,
    SkillIndiced,
    SkillCreate,
    EventIndiced,
    EventCreate,
    StateIndiced,
    StateCreate,
    UIEventIndiced,
    UIEventCreate,
    FilterIndiced,
    FilterCreate,
    CalculatorIndiced,
    CalculatorCreate,
    CardCalculatorIndiced,
    CardCalculatorCreate,
    TriggerIndiced,
    TriggerCreate,
    DesktopCoreIndiced,
    DesktopCoreCreate,
    DesktopZoneIndiced,
    DesktopZoneCreate,
    KOFIndiced,
    KOFCreate,
}


public class UIEventReceivedEventArgs : EventArgs
{
    public UIEventReceivedEventArgs(UIEvent _ev)
    {
        this.ev = _ev;
    }

    private UIEvent ev;
    public UIEvent Ev => ev;
}

public delegate void UIEventReceivedEventHandler(object sender, UIEventReceivedEventArgs e);

