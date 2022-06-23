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

public class DataPool
{
    readonly public List<Type> I2T = new List<Type>();
    readonly public Dictionary<Type, int> T2I = new Dictionary<Type, int>();

    readonly public List<TouhouSha.Core.Player> I2P = new List<TouhouSha.Core.Player>();
    readonly public Dictionary<TouhouSha.Core.Player, int> P2I = new Dictionary<TouhouSha.Core.Player, int>();

    readonly public List<Card> I2C = new List<Card>();
    readonly public Dictionary<Card, int> C2I = new Dictionary<Card, int>();

    readonly public List<Zone> I2Z = new List<Zone>();
    readonly public Dictionary<Zone, int> Z2I = new Dictionary<Zone, int>();

    readonly public List<DesktopCardBoardCore> I2DC = new List<DesktopCardBoardCore>();
    readonly public Dictionary<DesktopCardBoardCore, int> DC2I = new Dictionary<DesktopCardBoardCore, int>();

    readonly public List<DesktopCardBoardZone> I2DZ = new List<DesktopCardBoardZone>();
    readonly public Dictionary<DesktopCardBoardZone, int> DZ2I = new Dictionary<DesktopCardBoardZone, int>();

    readonly public List<ExternZone> I2EZ = new List<ExternZone>();
    readonly public Dictionary<ExternZone, int> EZ2I = new Dictionary<ExternZone, int>();

    readonly public List<Symbol> I2SY = new List<Symbol>();
    readonly public Dictionary<Symbol, int> SY2I = new Dictionary<Symbol, int>();

    readonly public List<Charactor> I2CA = new List<Charactor>();
    readonly public Dictionary<Charactor, int> CA2I = new Dictionary<Charactor, int>();

    readonly public List<Skill> I2SK = new List<Skill>();
    readonly public Dictionary<Skill, int> SK2I = new Dictionary<Skill, int>();

    readonly public List<Filter> I2FT = new List<Filter>();
    readonly public Dictionary<Filter, int> FT2I = new Dictionary<Filter, int>();

    readonly public List<Calculator> I2CC = new List<Calculator>();
    readonly public Dictionary<Calculator, int> CC2I = new Dictionary<Calculator, int>();

    readonly public List<CardCalculator> I2CCC = new List<CardCalculator>();
    readonly public Dictionary<CardCalculator, int> CCC2I = new Dictionary<CardCalculator, int>();

    readonly public List<Trigger> I2TG = new List<Trigger>();
    readonly public Dictionary<Trigger, int> TG2I = new Dictionary<Trigger, int>();

    readonly public List<KOFSelectCore> I2KOF = new List<KOFSelectCore>();
    readonly public Dictionary<KOFSelectCore, int> KOF2I = new Dictionary<KOFSelectCore, int>();

}
