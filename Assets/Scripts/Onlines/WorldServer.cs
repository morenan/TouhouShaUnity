using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TouhouSha.Core;
using TouhouSha.Core.UIs;
using TouhouSha.Core.UIs.Events;

public class WorldServer
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

    private Dictionary<object, Player> object2players = new Dictionary<object, Player>();
    private Dictionary<object, Zone> object2zones = new Dictionary<object, Zone>();
    private Dictionary<object, ExternZone> object2externzones = new Dictionary<object, ExternZone>();

    #endregion

    #region Method

    public void BeginListen(object obj)
    {
        if (obj is ShaObject)
        {
            ShaObject shaobj = (ShaObject)obj;
            shaobj.ShaPropertyChanged += OnShaPropertyChanged;
        }
        if (obj is INotifyPropertyChanged)
        {
            INotifyPropertyChanged notify = (INotifyPropertyChanged)obj;
            notify.PropertyChanged += OnNotifyPropertyChanged;
        }
        if (obj is Player)
        {
            Player player = (Player)obj;
            object2players.Add(player.Skills, player);
            object2players.Add(player.Zones, player);
            object2players.Add(player.Symbols, player);
            player.Skills.CollectionChanged += Player_Skills_CollectionChanged;
            player.Zones.CollectionChanged += Player_Zones_CollectionChanged;
            player.Symbols.CollectionChanged += Player_Symbols_CollectionChanged;
        }
        if (obj is Zone)
        {
            Zone zone = (Zone)obj;
            object2zones.Add(zone.ExternZones, zone);
            zone.ExternZones.CollectionChanged += Zone_ExternZones_CollectionChanged;
        }
        if (obj is ExternZone)
        {
            ExternZone zone = (ExternZone)obj;
            object2externzones.Add(zone.Cards, zone);
            zone.Cards.CollectionChanged += ExternZone_Cards_CollectionChanged;
        }
    }

    public void BoardcastUIEvent(UIEvent ev)
    {
        Writer.SendUIEvent(ev);
    }

    #endregion

    #region Event Handler

    private void OnShaPropertyChanged(object sender, ShaPropertyChangedEventArgs e)
    {
        Writer.SyncShaProperty(e.Source, e.PropertyName);
    }

    private void OnNotifyPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            // Player
            case "Ass":
            case "Console":
            case "TrusteeshipConsole":
            // Card
            case "TargetFilter":
            case "UseCondition":
            case "CardColor": // 固有属性不变，使用转化卡外套来更改。
            case "CardType":
            case "CardPoint":
            case "Zone": // 从属区不同步，以移动事件作处理。
            // DesktopCardBoardCore
            case "World":
            case "IsYes": // 以下返回结果无需同步，单独以结果包接受。
            case "SelectedCards":
                return;
            
        }
        Writer.SyncProperty(sender, e.PropertyName);
    }

    private void Player_Skills_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        Writer.SyncCollection(object2players[sender], "Skills", e);
    }

    private void Player_Zones_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        Writer.SyncCollection(object2players[sender], "Zones", e);
    }

    private void Player_Symbols_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        Writer.SyncCollection(object2players[sender], "Symbols", e);
    }

    private void Zone_ExternZones_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        Writer.SyncCollection(object2zones[sender], "ExternZones", e);
    }

    private void ExternZone_Cards_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        Writer.SyncCollection(object2externzones[sender], "Cards", e);
    }

    #endregion

}
