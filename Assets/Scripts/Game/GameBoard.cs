using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TouhouSha.Core;
using TouhouSha.Core.Events;
using TouhouSha.Core.Filters;
using TouhouSha.Core.UIs;
using TouhouSha.Core.UIs.Events;
using TouhouSha.Core.UIs.Texts;
using TouhouSha.Koishi.Cards;
using TouhouSha.AI;
using System.Collections.Specialized;
using System.Threading;
using Photon.Pun;
using Photon.Realtime;
using Player = TouhouSha.Core.Player;
using PunPlayer = Photon.Realtime.Player;

public class GameBoard : MonoBehaviour
{
    #region Resources
   
    public class HandMatchAnimation
    {
        public float TotalTime;
        public float MaxTime;
        public Button Button0;
        public Vector3 StartPoint0;
        public Vector3 EndPoint0;
        public Button Button1;
        public Vector3 StartPoint1;
        public Vector3 EndPoint1;
    }

    #endregion 

    #region Number

    private Player currentplayer;
    public Player CurrentPlayer
    {
        get
        {
            return this.currentplayer;
        }
        set
        {
            this.currentplayer = value;
        }
    }

    private Enum_GameBoardAction status;
    public Enum_GameBoardAction Status
    {
        get
        {
            return this.status;
        }
        set
        {
            if (status == value) return;
            switch (status)
            {
                case Enum_GameBoardAction.CardSelecting:
                case Enum_GameBoardAction.PlayerAndCardSelecting:
                    UI_Skills.LeaveCardSelect();
                    break;
                case Enum_GameBoardAction.CardUsing:
                    UI_Skills.LeaveUseCardState();
                    break;
            }
            status = value;
            switch (status)
            {
                case Enum_GameBoardAction.CardSelecting:
                case Enum_GameBoardAction.PlayerAndCardSelecting:
                    UI_Skills.EnterCardSelect(App.World.GetContext(), GetSelectCardFilter());
                    break;
                case Enum_GameBoardAction.CardUsing:
                    UI_Skills.EnterUseCardState(App.World.GetContext());
                    break;
            }
            if (Status == Enum_GameBoardAction.CharactorSelecting)
                App.Show(UI_CharactorSelectPanel);
            else
                App.Hide(UI_CharactorSelectPanel);
            if (Status == Enum_GameBoardAction.KOFCharactorSelecting)
                App.Show(UI_KOF);
            else
                App.Hide(UI_KOF);
            if (Status == Enum_GameBoardAction.ListSelecting)
                App.Show(UI_ListPanel);
            else
                App.Hide(UI_ListPanel);
            AskTimeoutEnd();
            UpdateAllControlUIs();
            switch (status)
            {
                case Enum_GameBoardAction.Asking:
                case Enum_GameBoardAction.CardSelecting:
                case Enum_GameBoardAction.PlayerSelecting:
                case Enum_GameBoardAction.PlayerAndCardSelecting:
                case Enum_GameBoardAction.CardUsing:
                    AskTimeoutStart();
                    App.Show(UI_Ask);
                    break;
                default:
                    App.Hide(UI_Ask);
                    break;
            }
            usecardconverts.Clear();
            usecardtargetfilters.Clear();
        }
    }

    public bool NoWaitAnimation = false;

    public Image UI_Background;
    public PlayerBe UI_CurrentPlayer;
    public MessageAskBox UI_Ask;
    public SkillList UI_Skills;
    public EquipList UI_Equips;
    public HandPlacer UI_Hands;
    public DesktopPlacer UI_DesktopPlacer;
    public DrawPlacer UI_DrawHeapPlacer;
    public DiscardPlacer UI_DiscardHeapPlacer;
    public KOFCharactorList UI_KOFList0;
    public KOFCharactorList UI_KOFList1;
    public CharactorSelectPanel UI_CharactorSelectPanel;
    public ListPanel UI_ListPanel;
    public DesktopPanel UI_DesktopBoard;
    public KOFPanel UI_KOF;
    public ExtraZonePanel SP_ExtraZones;
    public YujiPanel UI_Yuji;
    public OutputBox UI_OutputBox;
    public CoolSkill UI_CoolSkill;
    public WinnerPanel UI_Winner;
    public PlayerTooltip UI_PlayerTooltip;
    public CardTooltip UI_CardTooltip;
    public ZoneTooltip UI_ZoneTooltip;

    public GameObject CV_Players;
    public GameObject CV_Cards;
    public GameObject CV_Lines;
    public GameObject CV_Anims;

    public Button BN_MyFist;
    public Button BN_MyCut;
    public Button BN_MyCloth;
    public Button BN_YourHand;

    public AudioSource BGM;
    public AudioSource SE;
    public AudioSource Voice;
    public AudioClip[] BgmList;
    public string[] BgmNames;
    private Dictionary<string, AudioClip> name2bgms = new Dictionary<string, AudioClip>();

    readonly public List<GameCom> GameComs = new List<GameCom>();
    readonly public List<RemoteConsole> RemoteConsoles = new List<RemoteConsole>();

    /// <summary>
    /// 本地的游戏通信对象。
    /// </summary>
    public GameCom LocalGameCom;

    /// <summary>
    /// 是否确认角色，这时可以更新血条。
    /// </summary>
    public bool CharactorDetermined = false;

    /// <summary>
    /// 当前玩家的控制端。
    /// </summary>
    private GameBoardConsole console;
    public GameBoardConsole Console
    {
        get { return this.console; }
        set { this.console = value; }
    }

    /// <summary> 世界运行停顿。</summary>
    private AutoResetEvent worldwait = new AutoResetEvent(false);
    /// <summary> 调度给UI线程处理的任务队列 </summary>
    private Queue<DispatcherTask> dispatcher_queue = new Queue<DispatcherTask>();
    /// <summary> 调度任务队列的线程锁 </summary>
    private Futex dispatcher_futex = new Futex();

    /// <summary> 武将选择场景核 </summary>
    private SelectCharactorBoardCore sc_chars;
    /// <summary> 自己的卡牌选择场景核 </summary>
    private SelectCardBoardCore sc_cards;
    /// <summary> 场上角色选择场景核 </summary>
    private SelectPlayerBoardCore sc_players;
    /// <summary> 同时选择自己卡牌和场上角色的场景核 </summary>
    private SelectPlayerAndCardBoardCore sc_pncs;
    /// <summary> 列表选择场景核 </summary>
    private ListBoardCore sc_list;
    /// <summary> 出牌阶段的使用卡的选择器。 </summary>
    private CardFilter usecardfilter;
    /// <summary> 出牌阶段将要选择的卡。 </summary>
    private Card cardwillused;
    /// <summary> 当前正在使用的卡牌转换技能。 </summary>
    private ISkillCardConverter skillconvert;
    /// <summary> 当前将要发动的初动技能。 </summary>
    private ISkillInitative skillinitative;
    /// <summary> 当前正在使用的卡牌转换技能的来源装备。 </summary>
    private Card skillconvertfrom;
    /// <summary> 当前正在使用的初动技能的来源装备。 </summary>
    private Card skillinitativefrom;

    /// <summary> 应答环节是由应答【是】 </summary>
    private bool asked_yes;
    /// <summary> 卡牌选择是由应答【是】 </summary>
    //private bool selcard_yes;
    /// <summary> 角色选择是由应答【是】 </summary>
    //private bool selplayer_yes;
    /// <summary> 卡牌和角色选择是由应答【是】 </summary>
    //private bool selpnc_yes;
    /// <summary> 列表选择是由应答【是】 </summary>
    //private bool sellist_yes;
    /// <summary> 已决定选择的武将 </summary>
    private Charactor selectedcharactor;
    /// <summary> 已决定选择的最终卡牌列表 </summary>
    private List<Card> selectedcards = new List<Card>();
    /// <summary> 已选择的初始卡牌列表，请和最终卡牌作区分（神关羽，小乔那种锁定转换造成的不同） </summary>
    private Dictionary<Card, Card> selectedinitcards = new Dictionary<Card, Card>();
    /// <summary> 已决定选择的角色列表 </summary>
    private List<Player> selectedplayers = new List<Player>();
    /// <summary> 已决定选择的列表选择项的列表 </summary>
    private List<object> selecteditems = new List<object>();
    /// <summary> 用于优化UI，快速判定一张卡视作的另一张卡 </summary>
    private Dictionary<Card, Card> usecardconverts = new Dictionary<Card, Card>();
    /// <summary> 用于优化UI，快速判定一张卡的目标过滤器 </summary>
    private Dictionary<Card, PlayerFilter> usecardtargetfilters = new Dictionary<Card, PlayerFilter>();
    /// <summary> 是否正在拖动，要命中目标才算拖动。 </summary>
    private bool isdragging = false;
    /// <summary> 已决定（出牌阶段发动）的事件 </summary>
    private TouhouSha.Core.Event selectedevent;

    /// <summary> 猜拳决定 </summary>
    private HandMatchGesture handgesture = HandMatchGesture.Fist;
    /// <summary> 猜拳结果演示动画 </summary>
    private HandMatchAnimation handanim = null;

    private bool _toconnectremoteconsoles = false;

    #endregion

    #region MonoBehavior

    void Awake()
    {
        // 由于在主机客户端上跑逻辑，所以主机走小道大家都完蛋。
        // 主机只能当主公，走了就算反贼/内奸获胜。
        // 随机挑选一名幸运玩家担任主机/主公。
        if (App.IsOnlineGame 
         && PhotonNetwork.IsMasterClient
         && PhotonNetwork.CurrentRoom != null)
        {
            System.Random random = new System.Random();
            List<PunPlayer> players = PhotonNetwork.CurrentRoom.Players.Values.ToList();
            PunPlayer newmaster = players[random.Next() % players.Count()];
            if (newmaster != PhotonNetwork.MasterClient)
                PhotonNetwork.SetMasterClient(newmaster);
        }

        // 直球场景调试时，这里进行初始化。
        if (Config.GameConfig.UsedPackages.Count() == 0)
        {
            Config.GameConfig.UsedPackages.Add(new TouhouSha.Core.Package());
            Config.GameConfig.UsedPackages.Add(new TouhouSha.Koishi.Package());
            Config.GameConfig.UsedPackages.Add(new TouhouSha.Koishi.Package2());
            Config.GameConfig.UsedPackages.Add(new TouhouSha.Reimu.Package());
        }
        if (App.World == null)
        {
            App.World = new World();
            App.World.GameMode = Enum_GameMode.StandardPlayers8;
        }

        // 初始化UI事件。
        BN_MyFist.onClick.AddListener(BN_MyFist_MouseDown);
        BN_MyCloth.onClick.AddListener(BN_MyCloth_MouseDown);
        BN_MyCut.onClick.AddListener(BN_MyCut_MouseDown);

        // 绑定世界事件。
        App.World.UIEvent += World_UIEvent;
        App.World.GameStartup += World_GameStartup;
        App.World.PostComment += World_PostComment;

        // 确定可用Bgm。
        for (int i = 0; i < BgmList.Length; i++)
        {
            if (i >= BgmNames.Length) break;
            string name = BgmNames[i];
            AudioClip bgm = BgmList[i];
            if (name2bgms.ContainsKey(name)) continue;
            name2bgms.Add(name, bgm);
        }

        // 创建通信对象。
        PhotonNetwork.Instantiate("GameCom", new Vector3(0, 0, 0), Quaternion.identity);
    }

    void Start()
    {
        App.Hide(UI_Ask);
        App.Hide(UI_KOFList0);
        App.Hide(UI_KOFList1);
        App.Hide(UI_CharactorSelectPanel);
        App.Hide(UI_ListPanel);
        App.Hide(UI_DesktopBoard);
        App.Hide(UI_KOF);
        App.Hide(SP_ExtraZones);
        App.Hide(UI_Yuji);
        App.Hide(UI_CoolSkill);
        App.Hide(UI_Winner);
        App.Hide(BN_MyFist);
        App.Hide(BN_MyCut);
        App.Hide(BN_MyCloth);
        App.Hide(BN_YourHand);
        if (!App.IsOnlineGame)
            App.World.GameStart();
        else if (PhotonNetwork.IsMasterClient)
            Invoke("GameStartLater", 3.0f);
        else
            App.World.StartRemote();
    }

    void Update()
    {
        if (_toconnectremoteconsoles)
            ConnectRemoteConsoles();
        DispatcherTask task = null;
        dispatcher_futex.Invoke(() =>
        {
            if (dispatcher_queue.Count() > 0)
                task = dispatcher_queue.Dequeue();
        });
        if (task != null)
        {
            //print(String.Format("DispatcherTask:{0}", task));
            if (task is DispatcherUIEvent)
                World_UIEvent_Dispatcher(((DispatcherUIEvent)task).Ev);
            else if (task is DispatcherGameStartup)
                World_GameStartup_Dispatcher(((DispatcherGameStartup)task).Ev);
            else if (task is DispatcherPostComment)
                World_PostComment_Dispatcher(((DispatcherPostComment)task).Ev);
            else if (task is DispatcherAsk)
                Dispatcher_BeginAsk(((DispatcherAsk)task).Message);
            else if (task is DispatcherSelectCharactor)
                Dispatcher_BeginCharactorSelect(((DispatcherSelectCharactor)task).Core);
            else if (task is DispatcherSelectCard)
                Dispatcher_BeginCardSelect(((DispatcherSelectCard)task).Core);
            else if (task is DispatcherSelectPlayer)
                Dispatcher_BeginPlayerSelect(((DispatcherSelectPlayer)task).Core);
            else if (task is DispatcherSelectPlayerAndCard)
                Dispatcher_BeginPlayerAndCardSelect(((DispatcherSelectPlayerAndCard)task).Core);
            else if (task is DispatcherSelectList)
                Dispatcher_BeginListSelect(((DispatcherSelectList)task).Core);
            else if (task is DispatcherQuestUseCard)
                Dispatcher_BeginUseCardState(App.World.GetContext());
            else if (task is DispatcherBreakAction)
                Dispatcher_BreakLastAction();
            else if (task is DispatcherSelectDesktop)
            {
                DispatcherSelectDesktop disp = (DispatcherSelectDesktop)task;
                switch (disp.Action)
                {
                    case DispatcherSelectDesktop.Enum_Action.Open:
                        Dispatcher_ShowDesktopBoard(disp.Core);
                        break;
                    case DispatcherSelectDesktop.Enum_Action.Control:
                        Dispatcher_ControlDesktopBoard(disp.Core);
                        break;
                    case DispatcherSelectDesktop.Enum_Action.Close:
                        Dispatcher_CloseDesktopBoard(disp.Core);
                        break;
                }
            }
            else if (task is DispatcherSelectKOF)
            {
                DispatcherSelectKOF disp = (DispatcherSelectKOF)task;
                switch (disp.Action)
                {
                    case DispatcherSelectKOF.Enum_Action.Open:
                        Dispatcher_ShowKOF(disp.Core);
                        break;
                    case DispatcherSelectKOF.Enum_Action.Control:
                        Dispatcher_ControlKOF(disp.Core);
                        break;
                    case DispatcherSelectKOF.Enum_Action.Close:
                        Dispatcher_CloseKOF(disp.Core);
                        break;
                }
            }
        }
        if (!BGM.isPlaying)
        {
            System.Random random = new System.Random();
            BGM.clip = BgmList[random.Next() % BgmList.Length];
            BGM.Play();
        }
    }

    #endregion

    #region Method

    #region Players

    /// <summary> 玩家：逻辑对象 => 视觉对象 </summary>
    private Dictionary<Player, PlayerBe> player2views = new Dictionary<Player, PlayerBe>();

    /// <summary> 重置所有玩家的视觉对象。 </summary>
    protected void ResetPlayers()
    {
        // 释放之前的玩家视觉对象 
        foreach (PlayerBe playercard in player2views.Values)
            playercard.Core = null;
        player2views.Clear();
        CV_Players.transform.DetachChildren();
        // 世界（内核）不存在。
        if (App.World == null) return;
        // 当前控制台控制玩家的视觉对象。
        if (CurrentPlayer != null)
        {
            UI_CurrentPlayer.Core = CurrentPlayer;
            UI_CurrentPlayer.UpdateSelectedAss();
            player2views.Add(CurrentPlayer, UI_CurrentPlayer);
        }
        // 其他玩家的视觉对象。
        List<Player> others = App.World.GetAlivePlayersStartHere(CurrentPlayer).ToList();
        foreach (Player other in others)
        {
            if (other == CurrentPlayer) continue;
            GameObject go0 = Resources.Load<GameObject>("Player");
            GameObject go1 = GameObject.Instantiate(go0, CV_Players.transform);
            PlayerBe playercard = go1.GetComponent<PlayerBe>();
            playercard.name = other.KeyName;
            playercard.Core = other;
            player2views.Add(other, playercard);
        }
        // 布局所有玩家。
        LayoutPlayers();
    }

    /// <summary> 布局所有玩家视觉对象。 </summary>
    protected void LayoutPlayers()
    {
        if (App.World == null) return;
        if (CurrentPlayer == null) return;
        // 当前玩家是第几号（从0开始）。
        int i0 = App.World.Players.IndexOf(CurrentPlayer);
        // 其他玩家的依结算顺序的视觉对象列表。
        List<PlayerBe> playerviewlist = new List<PlayerBe>();
        // 生成这个列表。
        for (int i = i0 + 1; i != i0; i++)
        {
            i = i % App.World.Players.Count();
            if (i == i0) break;
            Player player = App.World.Players[i];
            PlayerBe playerview = player2views[player];
            playerviewlist.Add(playerview);
        }
        // 实际空间的矩形转换
        RectTransform cv_players_rt = CV_Players.GetComponent<RectTransform>();
        // 可容纳的实际空间宽度。
        double aw = cv_players_rt.rect.width;
        // 可容纳的实际空间高度。
        double ah = cv_players_rt.rect.height;
        // 一个视觉对象的矩形转换
        RectTransform ui_player_rt = UI_CurrentPlayer.gameObject.GetComponent<RectTransform>();
        // 一个视觉对象的实际占用宽度。
        double cw = ui_player_rt.rect.width;
        // 一个视觉对象的实际占用高度。
        double ch = ui_player_rt.rect.height;
        // 布局当前玩家的卡片UI
        if (UI_CurrentPlayer != null)
        {
            RectTransform current_player_rt = UI_CurrentPlayer.gameObject.GetComponent<RectTransform>();
            current_player_rt.localPosition = new Vector3(
                (float)(aw / 2 - current_player_rt.rect.width / 2),
                (float)(-ah / 2));
            UI_CurrentPlayer.Position = UI_CurrentPlayer.transform.position;
        }
        // 如果输出窗显示，空间宽度减去其宽度。
        if (App.IsVisible(UI_OutputBox))
        {
            RectTransform output_rt = UI_OutputBox.gameObject.GetComponent<RectTransform>();
            aw -= output_rt.rect.width;
        }
        // 两侧纵向的单位容纳容量。
        int n0 = (int)((ah - 16) / ch);
        // 顶侧横向的单位容纳容量。
        int n1 = (int)((aw - cw * 2) / cw);
        // 纵向容量必须是一个不小于0的合法值。
        n0 = Math.Max(n0, 0);
        // 纵向容量×2也不能超过其他玩家的数量。
        while (playerviewlist.Count() - n0 * 2 <= 0) n0--;
        // 总数量-两侧数量，决定是顶部数量。
        n1 = playerviewlist.Count() - n0 * 2;
        // 两侧纵向的排列间隔。
        double span0 = ch + 36;
        // 顶部横向的排列间隔。
        double span1 = (n1 > 0) ? ((aw - cw * 2) / n1) : cw;
        // 两侧纵向的最顶部y坐标。
        double sidetop = ah - span0 * Math.Max(n0, 1);
        // 排列每个其他玩家。
        for (int i = 0; i < playerviewlist.Count(); i++)
        {
            PlayerBe playercard = playerviewlist[i];
            RectTransform playercard_rt = playercard.gameObject.GetComponent<RectTransform>();
            double x = 0;
            double y = 0;
            // 排列到右侧。
            if (i < n0)
            {
                x = aw - cw;
                y = ah - (i + 1) * span0;
            }
            // 排列到左侧。
            else if (i >= playerviewlist.Count - n0)
            {
                x = 0;
                y = ah - (playerviewlist.Count() - i) * span0;
            }
            // 排列到顶部，这里作了一个阶梯过渡到两侧。
            else
            {
                x = cw + (playerviewlist.Count() - n0 - i - 1) * span1 + (span1 - cw) / 2;
                if (i == n0 || i == playerviewlist.Count() - n0 - 1)
                {
                    if (n0 == 0)
                        y = 16;
                    else
                        y = 16 + (sidetop - 16) / 2;
                }
                else
                {
                    y = 16;
                }
            }
            playercard_rt.localPosition = new Vector3(
                (float)(x + playercard_rt.rect.width / 2), 
                (float)(-y - playercard_rt.rect.height / 2));
            playercard.Position = playercard.transform.position;
            //print(String.Format("playerviewlist[{0}]=={1} localPosition={2} Position={3}", 
            //    i, playercard.name, playercard_rt.localPosition, playercard.Position));
            /*
            DamageShakeObject shake = null;
            if (player2damageshakes.TryGetValue(playercard.Player, out shake))
            {
                shake.StartPoint = new Point(Canvas.GetLeft(playercard), Canvas.GetTop(playercard));
                shake.Box = new Rect(shake.StartPoint.X - 40, shake.StartPoint.Y - 40, playercard.ActualWidth + 80, playercard.ActualHeight + 80);
            }
            */
        }
    }


    protected void UpdatePlayerStates()
    {
        foreach (PlayerBe playerview in gameObject.GetComponentsInChildren<PlayerBe>())
            playerview.UpdateState();
    }

    #endregion

    #region Cards

    /// <summary> 卡牌视觉对象的垃圾箱 = </summary>
    private List<CardBe> cardviewhiddens = new List<CardBe>();
    /// <summary> 卡牌：逻辑对象 => 视觉对象 </summary>
    private Dictionary<Card, CardBe> card2views = new Dictionary<Card, CardBe>();
    /// <summary> 正在处理（移动）动画的卡牌，以及其视觉对象。 </summary>
    // 用计时代替，取消不必要的交互。
    //private Dictionary<Card, CardBe> card2anims = new Dictionary<Card, CardBe>();
    /// <summary> 正在处理勾八动画的卡牌，以及勾八进度对象。 </summary>
    //private Dictionary<Card, CardAcceptObject> card2accepts = new Dictionary<Card, CardAcceptObject>();
    /// <summary> 正在处理叉叉动画的卡牌，以及叉叉进度对象。 </summary>
    //private Dictionary<Card, CardFailedObject> card2faileds = new Dictionary<Card, CardFailedObject>();
    /// <summary> 非停留的卡牌区，也要体现多卡堆叠的效果。这里作统计来累加坐标来实现堆叠。 </summary>
    private Dictionary<IGameBoardArea, int> notkeptareatotals = new Dictionary<IGameBoardArea, int>();
    /// <summary> 搜集到的UI卡牌移动事件 </summary>
    private List<UIMoveCardEvent> moveevents = new List<UIMoveCardEvent>();
    /// <summary> 搜集到的UI卡牌使用事件 </summary>
    private List<UICardEvent> cardevents = new List<UICardEvent>();
    /// <summary> 搜集到的UI卡牌勾八事件 </summary>
    private List<UICardAcceptEvent> cardacceptevents = new List<UICardAcceptEvent>();
    /// <summary> 搜集到的UI卡牌叉叉事件 </summary>
    private List<UICardFailedEvent> cardfailedevents = new List<UICardFailedEvent>();
    /// <summary> 搜集到的UI技能发动事件 </summary>
    private List<UISkillActive> skillactiveevents = new List<UISkillActive>();
    /// <summary> 搜集到的UI限定技/觉醒技动画事件 </summary>
    private List<UISkillCoolAnimEvent> skillcoolevents = new List<UISkillCoolAnimEvent>();
    /// <summary> 搜集到的UI阶段更变事件 </summary>
    private List<UIStateChangeEvent> stateevents = new List<UIStateChangeEvent>();
    /// <summary> 搜集到的UI伤害事件 </summary>
    private List<UIDamageEvent> damageevents = new List<UIDamageEvent>();
    /// <summary> 搜集到的UI恢复事件 </summary>
    private List<UIHealEvent> healevents = new List<UIHealEvent>();
    /// <summary> 收集到的UI座次交换事件 </summary>
    private List<UISwitchChairEvent> switchchairevents = new List<UISwitchChairEvent>();
    /// <summary> 搜集到的UI卡牌展示事件 </summary>
    private List<UICardShowEvent> cardshowevents = new List<UICardShowEvent>();
    /// <summary> 搜集到的UI卡牌取消展示事件 </summary>
    private List<UICardHideEvent> cardhideevents = new List<UICardHideEvent>();
    /// <summary> 搜集到的UI卡牌（拼点）赢事件 </summary>
    private List<UICardWinEvent> cardwinevents = new List<UICardWinEvent>();
    /// <summary> 搜集到的UI卡牌（拼点）输事件 </summary>
    private List<UICardLoseEvent> cardloseevents = new List<UICardLoseEvent>();
    /// <summary> 卡片高度统计，被激活的卡牌放到顶部，和Windows窗口一样。 </summary>
    private int cardzindex = 0;

    /// <summary> 获取卡片逻辑对象对应的视觉对象 </summary>
    /// <param name="card"></param>
    /// <returns></returns>
    public CardBe GetView(Card card)
    {
        CardBe cardview = null;
        if (card2views.TryGetValue(card, out cardview))
            return cardview;
        return null;
    }

    /// <summary> 创建卡片逻辑对象对应的视觉对象 </summary>
    /// <param name="card"></param>
    /// <returns></returns>
    public CardBe Show(Card card)
    {
        CardBe cardview = null;
        if (card2views.TryGetValue(card, out cardview))
            return cardview;
        if (cardviewhiddens.Count() > 0)
        {
            cardview = cardviewhiddens.LastOrDefault();
            cardviewhiddens.RemoveAt(cardviewhiddens.Count() - 1);
            App.Show(cardview);
        }
        if (cardview == null)
        {
            GameObject go0 = Resources.Load<GameObject>("Card");
            GameObject go1 = GameObject.Instantiate(go0, CV_Cards.transform);
            cardview = go1.GetComponent<CardBe>();
        }
        if (cardview != null)
        {
            cardview.Core = card;
            card2views.Add(card, cardview);
        }
        return cardview;
    }

    /// <summary> 注销卡牌视觉对象，放进垃圾堆等待二次利用 </summary>
    /// <param name="card"></param>
    /// <returns></returns>
    public void Hide(CardBe cardview)
    {
        if (cardview.Core != null)
        {
            if (card2views.ContainsKey(cardview.Core))
                card2views.Remove(cardview.Core);
            cardview.Core = null;
        }
        App.Hide(cardview);
        cardviewhiddens.Add(cardview);
    }

    /// <summary> 获取卡牌逻辑区对应的必定是非保持的卡牌视觉区 </summary>
    /// <param name="zone">卡牌逻辑区</param>
    /// <returns></returns>
    public IGameBoardArea GetAreaNotKept(Zone zone)
    {
        // 选卡区。
        if (zone is DesktopCardBoardZone) return null;
        // 左侧装备栏。
        if (zone.Owner == CurrentPlayer && zone.KeyName?.Equals(Zone.Equips) == true) return UI_Equips;
        // 角色卡牌也当作卡牌视觉区。
        if (zone.Owner != null)
        {
            PlayerBe playerview = null;
            player2views.TryGetValue(zone.Owner, out playerview);
            return playerview;
        }
        // 公用卡牌视觉区。
        switch (zone.KeyName)
        {
            // 桌面区和弃牌区，同映射到桌面视觉区。
            case Zone.Desktop:
            case Zone.Discard:
            case Zone.Yuji:
                return UI_DiscardHeapPlacer;
            // 桌面摸牌区
            case Zone.Draw:
                return UI_DrawHeapPlacer;
        }
        // 其他区同映射到桌面视觉区。
        return UI_DiscardHeapPlacer;
    }

    /// <summary> 获取卡牌逻辑区对应的卡牌视觉区 </summary>
    /// <param name="zone">卡牌逻辑区</param>
    /// <returns></returns>
    public IGameBoardArea GetArea(Zone zone)
    {
        // 选卡区。
        if (zone is DesktopCardBoardZone) return null;
        // 当前玩家的手牌列表。
        if (zone.Owner == CurrentPlayer && zone.KeyName?.Equals(Zone.Hand) == true) return UI_Hands;
        // 左侧装备栏。
        if (zone.Owner == CurrentPlayer && zone.KeyName?.Equals(Zone.Equips) == true) return UI_Equips;
        // 角色卡牌也当作卡牌视觉区。
        if (zone.Owner != null)
        {
            PlayerBe playerview = null;
            player2views.TryGetValue(zone.Owner, out playerview);
            return playerview;
        }
        // 公用卡牌视觉区。
        switch (zone.KeyName)
        {
            // 桌面区，弃牌区和猜测区，同映射到桌面视觉区。
            case Zone.Desktop:
            case Zone.Discard:
            case Zone.Yuji:
                return UI_DesktopPlacer;
            // 桌面摸牌区
            case Zone.Draw:
                return UI_DrawHeapPlacer;
        }
        // 其他区同映射到桌面视觉区。
        return UI_DiscardHeapPlacer;
    }

    /// <summary> 动画处理卡牌跨视觉区移动 </summary>
    /// <param name="card">移动卡牌</param>
    /// <param name="from">原逻辑区</param>
    /// <param name="to">新逻辑区</param>
    /// <returns> 动画完成需等待时间 </returns>
    protected float AnimMoveCrossArea(Card card, Zone from, Zone to)
    {
        // 移动到选卡面板没有主界面的显示，也没有动画。
        if (to is DesktopCardBoardZone) return 0.0f;
        // 选卡面板向公共区不显示动画移动
        if (from is DesktopCardBoardZone && to?.Owner == null) return 0.0f;
        // 卡牌视觉对象。
        CardBe cardview = GetView(card);
        IGameBoardArea areafrom = null;
        IGameBoardArea areato = GetArea(to);
        bool isshowedbefore = cardview != null;
        bool isshowedafter = areato != null && areato.KeptCards;
        if (isshowedbefore)
            areafrom = GetArea(from);
        else
            areafrom = GetAreaNotKept(from);
        if (areato == null)
            areato = GetAreaNotKept(to);
        // 同区域无需移动，等后续AnimArrange方法重排版。
        if (areafrom != null && areato != null && areafrom == areato) return 0.0f;
        // 桌面向弃牌堆无需移动（同属UI放置区），但是如果经过桌面清理优化，误认为拿回到桌面，但是桌面的集合又不包含之。
        // 桌面找不到该卡，会放到零点，也就是左上角，就造成了Bug。
        if (!String.IsNullOrEmpty(from.KeyName) && !String.IsNullOrEmpty(to.KeyName)
         && from.KeyName.Equals(Zone.Desktop) && to.KeyName.Equals(Zone.Discard)) return 0.0f;
        if (areafrom == null) areafrom = UI_DrawHeapPlacer;
        Vector3 startpoint = cardview?.Position
            ?? areafrom?.GetExpectedPosition(card)
            ?? default(Vector3);
        Vector3 endpoint = areato?.GetExpectedPosition(card)
            ?? default(Vector3);
        if (!isshowedbefore)
        {
            cardview = Show(card);
            cardview.Opacity = 0;
            startpoint = areafrom?.GetExpectedPosition(card)
                      ?? default(Vector3);
        }
        if (areafrom != null && !areafrom.KeptCards)
        {
            int total = 0;
            if (!notkeptareatotals.TryGetValue(areafrom, out total))
                notkeptareatotals.Add(areafrom, total = 0);
            startpoint.x += (total++) * 8;
            notkeptareatotals[areafrom] = total;
        }
        if (areato != null && !areato.KeptCards)
        {
            int total = 0;
            if (!notkeptareatotals.TryGetValue(areato, out total))
                notkeptareatotals.Add(areato, total = 0);
            endpoint.x += (total++) * 8;
            notkeptareatotals[areato] = total;
        }
        cardview.Position = startpoint;
        cardview.Move(endpoint);
        if (!isshowedbefore && !isshowedafter)
            cardview.ShowWaitHide();
        if (isshowedbefore && !isshowedafter)
            cardview.WaitHide();
        if (!isshowedbefore && isshowedafter)
            cardview.ShowWait();
        return cardview.GetEllapsedTime();
    }

    /// <summary> 动画处理卡牌从桌面上消失 </summary>
    /// <param name="card">消失卡牌</param>
    /// <returns> 动画完成需等待时间 </returns>
    protected float AnimHideDesktop(Card card)
    {
        CardBe cardview = GetView(card);
        if (cardview == null) return 0.0f;
        cardview.ImmediatelyHide();
        return cardview.GetEllapsedTime();
    }

    /// <summary> 动画处理这个区对这张卡的排布 </summary>
    /// <param name="card">处理卡</param>
    /// <param name="area">所在区</param>
    /// <returns> 动画完成需等待时间 </returns>
    protected float AnimArrange(Card card, IGameBoardArea area)
    {
        CardBe cardview = GetView(card);
        Vector3 endpoint = area?.GetExpectedPosition(card)
            ?? default(Vector3);
        cardview.Move(endpoint);
        return cardview.GetEllapsedTime();
    }

    #endregion

    #region Targets

    /// <summary> 线段视觉对象的垃圾箱 = </summary>
    private List<LineBe> lineviewhiddens = new List<LineBe>();

    public void LineTarget(Player source, Player target)
    {
        PlayerBe view0 = null;
        PlayerBe view1 = null;

        if (source == null) return;
        if (target == null) return;
        if (!player2views.TryGetValue(source, out view0)) return;
        if (!player2views.TryGetValue(target, out view1)) return;

        Vector3 p0 = view0.gameObject.transform.position;
        Vector3 p1 = view1.gameObject.transform.position;

        LineBe line = lineviewhiddens.LastOrDefault();
        if (line == null)
        {
            GameObject go0 = Resources.Load<GameObject>("Line");
            GameObject go1 = GameObject.Instantiate(go0, CV_Lines.transform);
            line = go1.GetComponent<LineBe>();
        }
        else
        {
            lineviewhiddens.RemoveAt(lineviewhiddens.Count() - 1);
            App.Show(line);
        }
        line.Show(p0, p1);
    }

    public void Hide(LineBe line)
    {
        App.Hide(line);
        lineviewhiddens.Add(line);
    }

    #endregion

    #region Skill Actives

    /// <summary> 技能发动文本的垃圾箱 = </summary>
    private List<SkillTextBe> skilltexttrash = new List<SkillTextBe>();
   
    public void AnimSkillActive(Player player, Skill skill)
    {
        PlayerBe playercard = null;
        if (player == null) return;
        if (!player2views.TryGetValue(player, out playercard)) return;
        SkillTextBe text = skilltexttrash.LastOrDefault();
        if (text == null)
        {
            GameObject go0 = Resources.Load<GameObject>("SkillText");
            GameObject go1 = GameObject.Instantiate(go0, CV_Lines.transform);
            text = go1.GetComponent<SkillTextBe>();
        }
        else
        {
            skilltexttrash.RemoveAt(skilltexttrash.Count() - 1);
            App.Show(text);
        }
        text.Show(skill.GetInfo().Name, playercard.Position);
    }

    public void Hide(SkillTextBe text)
    {
        App.Hide(text);
        skilltexttrash.Add(text);
    }

    #endregion

    #region Damages & Heals

    private Dictionary<string, List<AnimationBe>> animtrash = new Dictionary<string, List<AnimationBe>>();
    
    protected bool ExistAnimation(string name)
    {
        switch (name)
        {
            case DamageEvent.Fire:
            case DamageEvent.Thunder:
            case KillCard.Normal:
            case KillCard.Fire:
            case KillCard.Thunder:
            case MissCard.Normal:
            case LiqureCard.Normal:
            case PeachCard.Normal:
            case DuelCard.Normal:
            case "座次移动":
            case "Win":
            case "Lose":
            case "JudgeGood":
            case "JudgeBad":
            case "Chain":
            case "Skill_Nullify":
                return true;
        }
        return false;
    }

    protected AnimationBe CreateAnimation(string name)
    {
        string assetname = null;
        switch (name)
        {
            case DamageEvent.Fire:
                assetname = "Fire";
                break;
            case DamageEvent.Thunder:
                assetname = "Thunder";
                break;
            case KillCard.Normal:
                assetname = "Kill";
                break;
            case KillCard.Fire:
                assetname = "FireKill";
                break;
            case KillCard.Thunder:
                assetname = "ThunderKill";
                break;
            case MissCard.Normal:
                assetname = "Miss";
                break;
            case LiqureCard.Normal:
                assetname = "Liqure";
                break;
            case PeachCard.Normal:
                assetname = "Peach";
                break;
            case DuelCard.Normal:
                assetname = "Duel";
                break;
            case "座次移动":
                assetname = "Move";
                break;
            default:
                assetname = name;
                break;
        }
        if (assetname == null) return null;
        GameObject go0 = Resources.Load<GameObject>(assetname);
        GameObject go1 = GameObject.Instantiate(go0, CV_Anims.transform);
        AnimationBe anim = go1.GetComponent<AnimationBe>();
        anim.Name = name;
        return anim;
    }

    public void AnimationDamage(Player player, UIDamageEvent ev)
    {
        PlayerBe playercard = null;
        if (player2views.TryGetValue(player, out playercard))
        {
            // 伤害摇动
            playercard.DamageShake();
            // 伤害闪烁
            playercard.DamageBlink();
        }
        // 属性伤害动画
        AnimationByName(player, ev.DamageType);
    }

    public void AnimationHeal(Player player, UIHealEvent ev)
    {
        AnimationByName(player, ev.HealType);
    }

    public void AnimationSwitchChair(Player player)
    {
        AnimationByName(player, "座次移动");
    }

    public void AnimationByName(Player player, string name)
    {
        List<AnimationBe> trashlist = null;
        AnimationBe anim = null;
        if (ExistAnimation(name))
        {
            if (!String.IsNullOrEmpty(name)
             && animtrash.TryGetValue(name, out trashlist))
                anim = trashlist.LastOrDefault();
            if (anim != null)
            {
                trashlist.RemoveAt(trashlist.Count() - 1);
                LocateAnimation(anim, player);
                anim.Play();
                App.Show(anim);
            }
            else
            {
                anim = CreateAnimation(name);
                LocateAnimation(anim, player);
            }
        }
    }

    public void AnimationByName(Card card, string name)
    {
        CardBe cardview = GetView(card);
        if (cardview == null) return;
        List<AnimationBe> trashlist = null;
        AnimationBe anim = null;
        if (ExistAnimation(name))
        {
            if (!String.IsNullOrEmpty(name)
             && animtrash.TryGetValue(name, out trashlist))
                anim = trashlist.LastOrDefault();
            if (anim != null)
            {
                trashlist.RemoveAt(trashlist.Count() - 1);
                LocateAnimation(anim, card);
                anim.Play();
                App.Show(anim);
            }
            else
            {
                anim = CreateAnimation(name);
                LocateAnimation(anim, card);
            }
        }
    }

    protected void LocateAnimation(AnimationBe anim, Player player)
    {
        PlayerBe playercard = null;
        if (!player2views.TryGetValue(player, out playercard)) return;
        anim.transform.position = playercard.transform.position;
    }

    protected void LocateAnimation(AnimationBe anim, Card card)
    {
        CardBe cardview = null;
        if (!card2views.TryGetValue(card, out cardview)) return;
        anim.transform.position = cardview.transform.position;
    }

    public void Hide(AnimationBe anim)
    {
        List<AnimationBe> trash = null;
        if (!animtrash.TryGetValue(anim.Name, out trash))
        {
            trash = new List<AnimationBe>();
            animtrash.Add(anim.Name, trash);
        }
        trash.Add(anim);
        App.Hide(anim);
    }

    #endregion

    #region Screen Label

    public void AnimScreenLabel(Enum_ScreenCoolLabel e)
    {
        /*
        while ((int)e >= screenlabellist.Count())
            screenlabellist.Add(new ScreenCoolLabel((Enum_ScreenCoolLabel)(screenlabellist.Count())));
        ScreenCoolLabel label = screenlabellist[(int)e];
        BD_DarkMask.Opacity = 0;
        BD_DarkMask.Visibility = Visibility.Visible;
        DoubleAnimation anim = new DoubleAnimation();
        anim.Duration = new Duration(TimeSpan.FromMilliseconds(300));
        anim.From = 0;
        anim.To = 1;
        BD_DarkMask.BeginAnimation(OpacityProperty, anim);
        UI_ChampainshipLabel.ImageSource = label.ImageSource;
        UI_ChampainshipLabel.LightSource = label.LightSource;
        UI_ChampainshipLabel.EnglishText = label.BottomText;
        UI_ChampainshipLabel.AnimationStart();
        screenlabelwait.Reset();
        */
    }

    #endregion

    #region UI <==> World

    public enum ControlFlags
    {
        None = 0x0000,
        All = 0xFFFF,
        Ask = 0x0001,
        CardWillUsed = 0x0002,
        ExtraAreaList = 0x0004,
        CardSelections = 0x0008,
        PlayerSelections = 0x0010,
    }

    public void UpdateAllControlUIs(ControlFlags flag = ControlFlags.All)
    {
        if ((flag & ControlFlags.CardWillUsed) != ControlFlags.None)
            UpdateCardWillUsed();
        if ((flag & ControlFlags.ExtraAreaList) != ControlFlags.None)
            ShowOrHideCardSelectExtraArea();
        if ((flag & ControlFlags.CardSelections) != ControlFlags.None)
            UpdateCardAboutSelection();
        if ((flag & ControlFlags.PlayerSelections) != ControlFlags.None)
            UpdatePlayerAboutSelection();
        if ((flag & ControlFlags.Ask) != ControlFlags.None)
        {
            UpdateAskVisible();
            UpdateAskMessage();
            UpdateAskButtons();
        }
    }

    #region Ask

    public void BeginAsk(string message)
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherAsk(message)); });
    }

    protected void Dispatcher_BeginAsk(string message)
    {
        UI_Ask.Message = message;
        Status = Enum_GameBoardAction.Asking;
    }

    public bool GetAskedResult()
    {
        return asked_yes;
    }

    protected void UpdateAskVisible()
    {
        switch (Status)
        {
            case Enum_GameBoardAction.None:
            case Enum_GameBoardAction.ListSelecting:
                App.Hide(UI_Ask);
                break;
            default:
                App.Show(UI_Ask);
                break;
        }
    }

    protected void UpdateAskMessage()
    {
        switch (Status)
        {
            case Enum_GameBoardAction.Asking:
                break;
            case Enum_GameBoardAction.CardSelecting:
                UI_Ask.Message = sc_cards?.Message;
                break;
            case Enum_GameBoardAction.PlayerSelecting:
                UI_Ask.Message = sc_players?.Message;
                break;
            case Enum_GameBoardAction.PlayerAndCardSelecting:
                UI_Ask.Message = sc_pncs?.Message;
                break;
            case Enum_GameBoardAction.CardUsing:
                UI_Ask.Message = skillinitative?.GetMessage(App.World.GetContext()) ?? "出牌阶段，请选择你要使用的卡。";
                break;
            default:
                UI_Ask.Message = String.Empty;
                break;
        }
    }

    protected void UpdateAskButtons()
    {
        Button yes = UI_Ask.ButtonYes;
        Button no = UI_Ask.ButtonNo;
        switch (Status)
        {
            case Enum_GameBoardAction.Asking:
                App.Show(yes);
                App.Show(no);
                yes.interactable = true;
                no.interactable = true;
                break;
            case Enum_GameBoardAction.CardSelecting:
                App.Show(yes);
                App.Show(no);
                yes.interactable = CanEndCardSelectWithYes();
                no.interactable = sc_cards?.CanCancel == true;
                break;
            case Enum_GameBoardAction.PlayerSelecting:
                App.Show(yes);
                App.Show(no);
                yes.interactable = sc_players?.PlayerFilter?.Fulfill(App.World.GetContext(), selectedplayers) == true;
                no.interactable = sc_players?.CanCancel == true;
                break;
            case Enum_GameBoardAction.PlayerAndCardSelecting:
                App.Show(yes);
                App.Show(no);
                yes.interactable = CanEndPlayerAndCardSelectWithYes();
                no.interactable = sc_pncs?.CanCancel == true;
                break;
            case Enum_GameBoardAction.CardUsing:
                App.Show(yes);
                App.Show(no);
                yes.interactable = CanPostEventInUseCardState();
                no.interactable = true;
                break;
            default:
                App.Hide(yes);
                App.Hide(no);
                break;
        }
    }

    protected void AskTimeoutStart()
    {
        switch (Status)
        {
            case Enum_GameBoardAction.None:
            case Enum_GameBoardAction.ListSelecting:
                return;
        }
        int timeout = Config.GameConfig.Timeout_Handle;
        if (Status == Enum_GameBoardAction.Asking)
            timeout = Config.GameConfig.Timeout_Ask;
        else if (Status == Enum_GameBoardAction.CardUsing)
            timeout = Config.GameConfig.Timeout_UseCard;
        else if (sc_cards != null)
            timeout = sc_cards.Timeout;
        else if (sc_players != null)
            timeout = sc_players.Timeout;
        else if (sc_pncs != null)
            timeout = sc_pncs.Timeout;
        UI_Ask.StartTimeout(timeout);
    }

    protected void AskTimeoutEnd()
    {
        UI_Ask.StopTimeout();
    }

    #endregion

    #region Charactor Select

    public void BeginCharactorSelect(SelectCharactorBoardCore core)
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherSelectCharactor(core)); });
    }

    protected void Dispatcher_BeginCharactorSelect(SelectCharactorBoardCore core)
    {
        sc_chars = core;
        Status = Enum_GameBoardAction.CharactorSelecting;
        UI_CharactorSelectPanel.Core = core;
    }

    public void SelectCharactor(Charactor char0)
    {
        if (sc_chars == null) return;
        sc_chars.SelectedCharactor = char0;
        sc_chars = null;
        UI_CharactorSelectPanel.Core = null;
        Status = Enum_GameBoardAction.None;
        console.WorldContiune();
    }

    #endregion

    #region Card Select

    public void BeginCardSelect(SelectCardBoardCore core)
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherSelectCard(core)); });
    }

    public void BreakLastAction()
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherBreakAction()); });
    }

    /// <summary> 开始自己的卡牌选择。 </summary>
    /// <param name="core"></param>
    protected void Dispatcher_BeginCardSelect(SelectCardBoardCore core)
    {
        this.sc_cards = core;
        this.skillinitative = null;
        this.skillconvert = null;
        selectedcards.Clear();
        selectedinitcards.Clear();
        Status = Enum_GameBoardAction.CardSelecting;
    }

    protected void Dispatcher_BreakLastAction()
    {
        while (Status != Enum_GameBoardAction.None) UI_Ask_No();
    }

    /// <summary> 获取是由决定选择卡，以及选择的卡。  </summary>
    /// <param name="isyes">是否决定</param>
    /// <returns></returns>
    public List<Card> GetSelectedCards()
    {
        // 如果使用了转换卡的技能，则使用转换判定方式。
        if (skillconvert != null)
        {
            bool convert_isvalid;
            List<Card> remainscards;
            return GetConvertedCards(out convert_isvalid, out remainscards);
        }
        // 返回通常手选的卡。
        return selectedcards;
    }

    /// <summary> 启用了转换卡的技能后，获取转换过程是否有效，以及选择卡进行转换后的卡。 </summary>
    /// <param name="isvalid">是否有效</param>
    /// <param name="convertedcards">已转换后的卡</param>
    /// <param name="remainscards">待转换的卡</param>
    /// <returns></returns>
    protected List<Card> GetConvertedCards(out bool isvalid, out List<Card> remainscards)
    {
        // 将选择卡依顺序进行转换，放到这个转换后卡列表。
        List<Card> convertedcards = new List<Card>();
        // 当前等待后续选择卡来组包进行转换的队列。
        // 例如丈八的第一张卡，放在这里等待第二张，并拿出来转换放到convertedcards队列。
        remainscards = new List<Card>();
        // 没有启用转换卡。
        if (skillconvert == null)
        {
            isvalid = false;
            return convertedcards;
        }
        // 上下文。
        Context ctx = App.World.GetContext();
        // 最终卡牌的选择器，将转换后的卡带入进行检验。
        CardFilter cardfilter = GetSelectCardFilter();
        // 依次处理每张选择卡。
        foreach (Card card in selectedcards)
        {
            // 先放到等待队列。
            remainscards.Add(card);
            // 等待队列满足转换条件。
            if (skillconvert.CardFilter.Fulfill(ctx, remainscards))
            {
                // 单卡转单卡调用方法。
                if (remainscards.Count() == 1)
                    convertedcards.Add(skillconvert.CardConverter.GetValue(ctx, remainscards[0]));
                // 多卡转单卡调用方法。
                else
                    convertedcards.Add(skillconvert.CardConverter.GetCombine(ctx, remainscards));
                // 等待队列以及转换完成，清空。
                remainscards.Clear();
            }
        }
        // 以下场景失败。
        // 1. 等待队列还有卡没有转换。
        // 2. 转换后的卡的集合，不通过最终筛选器的校验。
        if (remainscards.Count() > 0
         || !cardfilter.Fulfill(ctx, convertedcards))
        {
            isvalid = false;
            return convertedcards;
        }
        // 否则成功，并返回转换后的队列。
        isvalid = true;
        return convertedcards;
    }

    /// <summary> 获取未被替换的卡片筛选器。 </summary>
    /// <returns></returns>
    /// <remarks>
    /// 替换是什么概念呢？例如吕布的无双，其中一种实现方式就是将【响应闪的卡片筛选器】替换为【响应两张闪的卡片筛选器】。
    /// </remarks>
    public CardFilter GetSelectCardFilterWithoutReplaced()
    {
        switch (Status)
        {
            case Enum_GameBoardAction.CardSelecting:
                return sc_cards?.CardFilter;
            case Enum_GameBoardAction.PlayerAndCardSelecting:
                return sc_pncs?.CardFilter;
            case Enum_GameBoardAction.CardUsing:
                return skillinitative?.CostFilter ?? usecardfilter;
        }
        return null;
    }

    /// <summary> 获取卡片筛选器。</summary>
    /// <returns></returns>
    public CardFilter GetSelectCardFilter()
    {
        CardFilter result = GetSelectCardFilterWithoutReplaced();
        if (result == null) return result;
        return App.World.TryReplaceNewCardFilter(result, null);
    }

    /// <summary> 检验是否能按下【确定】按钮，完成对卡的选择。 </summary>
    /// <returns></returns>
    protected bool CanEndCardSelectWithYes()
    {
        // 启用转换技能后，进行检验。
        if (skillconvert != null)
        {
            bool isvalid;
            List<Card> remainscards;
            GetConvertedCards(out isvalid, out remainscards);
            return isvalid;
        }
        // 直接用筛选器进行检验。
        return sc_cards?.CardFilter?.Fulfill(App.World.GetContext(), selectedcards) == true;
    }

    /// <summary> 显示或者隐藏自身额外卡牌区 </summary>
    protected void ShowOrHideCardSelectExtraArea()
    {
        CardFilter cardfilter = skillinitative?.CostFilter ?? skillconvert?.CardFilter;
        if (cardfilter != null) cardfilter = App.World.TryReplaceNewCardFilter(cardfilter, null);
        if (cardfilter == null)
        {
            cardfilter = GetSelectCardFilter();
            if (cardfilter != null && (cardfilter.GetFlag(App.World.GetContext()) & Enum_CardFilterFlag.UseExtraZones) == Enum_CardFilterFlag.None)
                cardfilter = null;
        }
        if (cardfilter == null)
        {
            SP_ExtraZones.Zones.Clear();
            App.Hide(SP_ExtraZones);
        }
        else
        {
            Context ctx = App.World.GetContext();
            List<Zone> extrazones = new List<Zone>();
            foreach (Zone zone in CurrentPlayer.Zones)
            {
                Card least = null;
                if (zone.KeyName?.Equals(Zone.Hand) == true) continue;
                if (zone.KeyName?.Equals(Zone.Equips) == true) continue;
                foreach (Card card in zone.Cards)
                {
                    Card newcard = App.World.CalculateCard(ctx, card);
                    if (newcard == null) continue;
                    if (!cardfilter.CanSelect(ctx, new Card[] { }, newcard)) continue;
                    least = newcard;
                    break;
                }
                if (least != null) extrazones.Add(zone);
            }
            if (extrazones.Count() > 0)
                App.Show(SP_ExtraZones);
            else
                App.Hide(SP_ExtraZones);
            while (SP_ExtraZones.Zones.Count < extrazones.Count())
                SP_ExtraZones.Zones.Add(extrazones[SP_ExtraZones.Zones.Count]);
            while (SP_ExtraZones.Zones.Count > extrazones.Count())
                SP_ExtraZones.Zones.RemoveAt(SP_ExtraZones.Zones.Count - 1);
            for (int i = 0; i < extrazones.Count(); i++)
                if (SP_ExtraZones.Zones[i] != extrazones[i])
                    SP_ExtraZones.Zones[i] = extrazones[i];

        }
    }

    /// <summary> 更新所有自身卡牌区的已选/可选状态。 </summary>
    protected void UpdateCardAboutSelection()
    {
        World world = App.World;
        Context ctx = world.GetContext();
        IGameBoardArea handarea = UI_Hands;
        CardFilter cardfilter = GetSelectCardFilter();
        #region 告知已选择情况
        if (cardfilter is IKnowPlayerSelection)
        {
            IKnowPlayerSelection needknow = (IKnowPlayerSelection)cardfilter;
            needknow.LetKnow(ctx, selectedplayers);
        }
        #endregion
        #region 无卡牌筛选器
        if (cardfilter == null)
        {
            selectedcards.Clear();
            selectedinitcards.Clear();
            foreach (Card card in handarea.Cards)
            {
                CardBe cardview = GetView(card);
                if (cardview == null) continue;
                cardview.IsEnterSelecting = false;
                cardview.IsSelected = false;
                cardview.CanSelect = false;
                cardview.CanDrag = false;
            }
            UI_Equips.UpdateAboutSelections(ctx, cardfilter,
                selectedcards, selectedinitcards,
                skillinitative, skillconvert);
            foreach (ExtraZoneList zonelist in SP_ExtraZones.Lists)
            {
                if (zonelist.Zone == null) continue;
                zonelist.UpdateAboutSelections(ctx, cardfilter,
                    selectedcards, selectedinitcards);
            }
        }
        #endregion
        #region 卡牌筛选器强制通过所有可行卡
        else if ((cardfilter.GetFlag(ctx) & Enum_CardFilterFlag.ForceAll) != Enum_CardFilterFlag.None)
        {
            selectedcards.Clear();
            selectedinitcards.Clear();
            foreach (Card card in handarea.Cards)
            {
                if (selectedinitcards.ContainsKey(card)) continue;
                CardBe cardview = GetView(card);
                if (cardview == null) continue;
                Card newcard = world.CalculateCard(ctx, card);
                cardview.IsEnterSelecting = true;
                cardview.CanDrag = false;
                if (cardfilter.CanSelect(ctx, selectedcards, newcard))
                {
                    selectedcards.Add(newcard);
                    selectedinitcards.Add(card, newcard);
                    cardview.IsSelected = true;
                    cardview.CanSelect = false;
                }
                else
                {
                    cardview.IsSelected = false;
                    cardview.CanSelect = false;
                }
            }
            UI_Equips.UpdateAboutSelections(ctx, cardfilter,
                selectedcards, selectedinitcards,
                skillinitative, skillconvert);
            foreach (ExtraZoneList zonelist in SP_ExtraZones.Lists)
            {
                if (zonelist.Zone == null) continue;
                zonelist.UpdateAboutSelections(ctx, cardfilter,
                    selectedcards, selectedinitcards);
            }
        }
        #endregion
        #region 启用了转换技能
        else if (skillconvert?.CardFilter != null && skillconvert?.CardConverter != null)
        {
            // 当前转换选择是否有效。
            bool isvalid;
            // 等待组合转换的卡。
            List<Card> remainscards;
            // 已经转换后的卡。
            List<Card> convertedcards = GetConvertedCards(out isvalid, out remainscards);
            // 枚举每个手牌。
            foreach (Card card in handarea.Cards)
            {
                CardBe cardview = GetView(card);
                if (cardview == null) continue;
                Card newcard = world.CalculateCard(ctx, card);
                cardview.IsEnterSelecting = true;
                cardview.CanDrag = false;
                // 已经被选过了。
                if (selectedinitcards.ContainsKey(card))
                {
                    cardview.IsSelected = true;
                    cardview.CanSelect = false;
                }
                // 不能通过转换技能的筛选器。
                else if (!skillconvert.CardFilter.CanSelect(ctx, remainscards, newcard))
                {
                    cardview.IsSelected = false;
                    cardview.CanSelect = false;
                }
                // 通过了转换技能的筛选器。
                else
                {
                    // 暂时加入等待队列。
                    remainscards.Add(newcard);
                    // 如果满足转换立即转换，检验转换卡和之前的选择是否通过最终筛选器。
                    if (skillconvert.CardFilter.Fulfill(ctx, remainscards))
                    {
                        Card newconv = remainscards.Count() == 1
                            ? skillconvert.CardConverter.GetValue(ctx, remainscards[0])
                            : skillconvert.CardConverter.GetCombine(ctx, remainscards);
                        cardview.IsSelected = false;
                        cardview.CanSelect = cardfilter.CanSelect(ctx, convertedcards, newconv);
                    }
                    // 转换不满足，继续加卡转换。
                    else
                    {
                        cardview.IsSelected = false;
                        cardview.CanSelect = true;
                    }
                    // 离开暂时加入的等待队列。
                    remainscards.Remove(newcard);
                }
            }
            // 其他区域也同样处理转换技能。
            UI_Equips.UpdateWithConvertSkill(ctx, cardfilter, skillconvert,
                convertedcards, remainscards, selectedinitcards,
                skillinitative, skillconvert);
            foreach (ExtraZoneList zonelist in SP_ExtraZones.Lists)
            {
                if (zonelist.Zone == null) continue;
                zonelist.UpdateWithConvertSkill(ctx, cardfilter, skillconvert,
                    convertedcards, remainscards, selectedinitcards);
            }
        }
        #endregion
        #region 使用卡牌筛选器进行判定
        else
        {
            foreach (Card card in handarea.Cards)
            {
                CardBe cardview = GetView(card);
                if (cardview == null) continue;
                Card newcard = world.CalculateCard(ctx, card);
                cardview.IsEnterSelecting = true;
                if (selectedinitcards.ContainsKey(card))
                {
                    cardview.IsSelected = true;
                    cardview.CanSelect = false;
                }
                else
                {
                    cardview.IsSelected = false;
                    cardview.CanSelect = cardfilter.CanSelect(ctx, selectedcards, newcard);
                }
                cardview.CanDrag = cardview.CanSelect && cardfilter is UseCardStateCardFilter;
            }
            UI_Equips.UpdateAboutSelections(ctx, cardfilter,
                selectedcards, selectedinitcards,
                skillinitative, skillconvert);
            foreach (ExtraZoneList zonelist in SP_ExtraZones.Lists)
            {
                if (zonelist.Zone == null) continue;
                zonelist.UpdateAboutSelections(ctx, cardfilter,
                    selectedcards, selectedinitcards);
            }
        }
        #endregion
    }

    /// <summary> 设置转换技能 </summary>
    /// <param name="conv"></param>
    protected void SetSkillConvert(ISkillCardConverter conv, Card from)
    {
        skillconvert = conv;
        skillconvertfrom = from;
        selectedcards.Clear();
        selectedinitcards.Clear();
        selectedplayers.Clear();
        UpdateAllControlUIs();
    }

    /// <summary> 取消转换技能 </summary>
    protected void CancelSkillConvert()
    {
        skillconvert = null;
        skillconvertfrom = null;
        selectedcards.Clear();
        selectedinitcards.Clear();
        selectedplayers.Clear();
        UpdateAllControlUIs();

    }

    /// <summary> 设置初动技能 </summary>
    /// <param name="conv"></param>
    protected void SetSkillInitative(ISkillInitative init, Card from)
    {
        skillinitative = init;
        skillinitativefrom = from;
        selectedcards.Clear();
        selectedinitcards.Clear();
        selectedplayers.Clear();
        UpdateAllControlUIs();
    }

    /// <summary> 取消初动技能 </summary>
    protected void CancelSkillInitative()
    {
        skillinitative = null;
        skillinitativefrom = null;
        selectedcards.Clear();
        selectedinitcards.Clear();
        selectedplayers.Clear();
        UpdateAllControlUIs();
    }

    /// <summary>
    /// 鼠标点击卡牌，进行选择/取消选择的处理。
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void CardView_MouseDown(CardBe cardview)
    {
        if (isdragging) return;
        #region 限手牌
        if (cardview.Core == null) return;
        if (!((IGameBoardArea)(UI_Hands)).Cards.Contains(cardview.Core)) return;
        #endregion
        #region 取消已经选择的卡
        if (cardview.IsSelected)
        {
            //cardview.IsSelected = false;
            if (selectedinitcards.ContainsKey(cardview.Core))
            {
                selectedcards.Remove(selectedinitcards[cardview.Core]);
                selectedinitcards.Remove(cardview.Core);
            }
            UpdateAllControlUIs(ControlFlags.All & ~ControlFlags.ExtraAreaList);
        }
        #endregion
        #region 选择可选的卡
        else if (cardview.CanSelect)
        {
            //cardview.IsSelected = true;
            if (!selectedinitcards.ContainsKey(cardview.Core))
            {
                Card newcard = App.World.CalculateCard(App.World.GetContext(), cardview.Core);
                selectedcards.Add(newcard);
                selectedinitcards.Add(cardview.Core, newcard);
            }
            UpdateAllControlUIs(ControlFlags.All & ~ControlFlags.ExtraAreaList);
        }
        #endregion
    }
    
    /// <summary>
    /// 选择了一个装备。
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void EquipSelect(Card card)
    {
        Context ctx = App.World.GetContext();
        CardFilter cardfilter = GetSelectCardFilter();
        ISkillCardConverter conv = card?.Skills?.FirstOrDefault(_skill => _skill is ISkillCardConverter) as ISkillCardConverter;
        ISkillInitative init = card?.Skills?.FirstOrDefault(_skill => _skill is ISkillInitative) as ISkillInitative;
        #region 转换技能
        if ((cardfilter is UseCardStateCardFilter || cardfilter is ICardFilterRequiredCardTypes) && conv != null)
        {
            #region 多选转换技能
            if (conv is ISkillCardMultiConverter)
            {
                ISkillCardMultiConverter multi = (ISkillCardMultiConverter)(conv);
                // 手牌区域，要视作用手牌使用。
                Zone hand = CurrentPlayer.Zones.FirstOrDefault(_zone => _zone.KeyName?.Equals(Zone.Hand) == true);
                // 可选的卡的列表。
                List<Card> cardlist = new List<Card>();
                // 出牌阶段，仅能选择从可以使用的牌类。
                #region 出牌阶段
                if (Status == Enum_GameBoardAction.CardUsing)
                {
                    // 枚举每种卡。
                    foreach (string cardtype in multi.GetCardTypes(ctx))
                    {
                        // 创建这个种类的虚拟卡，放置到手牌建立虚拟场景，检验其是否可以使用。
                        Card cardinst = App.World.GetCardInstance(cardtype);
                        if (cardinst == null) continue;
                        cardinst = cardinst.Clone();
                        bool canuse = false;
                        using (ZoneLock zoneenv = new ZoneLock(cardinst, hand))
                        {
                            if (multi is ISkillCardConverterMark)
                                ((ISkillCardConverterMark)multi).Mark(ctx, cardinst);
                            ConditionFilter condition = cardinst.UseCondition;
                            if (condition != null) condition = App.World.TryReplaceNewCondition(condition, null);
                            canuse = condition?.Accept(ctx) == true;
                        }
                        if (canuse) cardlist.Add(cardinst);
                    }
                }
                #endregion
                // 响应阶段，根据情况来定。
                #region 响应阶段
                else
                {
                    // 响应的卡牌筛选器有要求列表，求两个列表的交集。
                    if (cardfilter is ICardFilterRequiredCardTypes)
                    {
                        ICardFilterRequiredCardTypes required = (ICardFilterRequiredCardTypes)cardfilter;
                        // 枚举每种卡。
                        foreach (string cardtype in multi.GetCardTypes(ctx))
                        {
                            // 不在要求列表中。
                            if (!required.RequiredCardTypes.Contains(cardtype)) continue;
                            // 加入到可选列表中。
                            cardlist.Add(App.World.GetCardInstance(cardtype));
                        }
                    }
                    // 未知响应，假设可以使用这个技能。
                    else
                    {
                        SetSkillConvert(conv, card);
                        return;
                    }
                }
                #endregion
                #region 直接选择/使用于吉面板
                // 没有可用的卡的种类，本次选择无效。
                if (cardlist.Count() == 0)
                {
                    //UI_Skills.SetUncheck(e.Skill);
                    return;
                }
                // 仅一个，设置此卡并开始转换。
                if (cardlist.Count() == 1)
                {
                    multi.SetSelectedCardType(ctx, cardlist[0].KeyName);
                    if (skillconvert != null)
                        UI_Skills.SetUncheck(skillconvert as Skill);
                    SetSkillConvert(conv, card);
                    return;
                }
                // 多种可选，使用于吉面板来选择。
                UI_Yuji.Skill = conv as Skill;
                UI_Yuji.SkillFromCard = card;
                UI_Yuji.CardList = cardlist;
                App.Show(UI_Yuji);
                return;
                #endregion
            }
            #endregion
            #region 普通转换技能
            SetSkillConvert(conv, card);
            #endregion
        }
        #endregion
        #region 初发技能
        else if (cardfilter is UseCardStateCardFilter && init != null)
        {
            if (skillinitative != null)
                UI_Skills.SetUncheck(skillinitative as Skill);
            SetSkillInitative(init, card);
        }
        #endregion
        #region 选择该装备
        else if (!selectedinitcards.ContainsKey(card))
        {
            Card newcard = App.World.CalculateCard(App.World.GetContext(), card);
            selectedcards.Add(newcard);
            selectedinitcards.Add(card, newcard);
        }
        #endregion
        UpdateAllControlUIs(ControlFlags.All & ~ControlFlags.ExtraAreaList);
    }

    /// <summary>
    /// 取消对一个装备的选择。
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void EquipUnselect(Card card)
    {
        #region 取消转换技能
        if (skillconvert is Skill && card.Skills.Contains(skillconvert as Skill))
        {
            CancelSkillConvert();
        }
        #endregion
        #region 取消初发技能
        else if (skillinitative is Skill && card.Skills.Contains(skillinitative as Skill))
        {
            CancelSkillInitative();
        }
        #endregion
        #region 取消该装备的选择
        else if (selectedinitcards.ContainsKey(card))
        {
            selectedcards.Remove(selectedinitcards[card]);
            selectedinitcards.Remove(card);
        }
        #endregion
        UpdateAllControlUIs(ControlFlags.All & ~ControlFlags.ExtraAreaList);
    }

    /// <summary> 选择了一个额外区的卡 </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ExtraZoneCardSelected(Card card)
    {
        if (!selectedinitcards.ContainsKey(card))
        {
            Card newcard = App.World.CalculateCard(App.World.GetContext(), card);
            selectedcards.Add(newcard);
            selectedinitcards.Add(card, newcard);
        }
        UpdateAllControlUIs(ControlFlags.All & ~ControlFlags.ExtraAreaList);
    }

    /// <summary> 取消选择一个额外区的卡 </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ExtraZoneCardUnselected(Card card)
    {
        if (selectedinitcards.ContainsKey(card))
        {
            selectedcards.Remove(selectedinitcards[card]);
            selectedinitcards.Remove(card);
        }
        UpdateAllControlUIs(ControlFlags.All & ~ControlFlags.ExtraAreaList);
    }

    #endregion

    #region Player Select

    public void BeginPlayerSelect(SelectPlayerBoardCore core)
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherSelectPlayer(core)); });
    }

    protected void Dispatcher_BeginPlayerSelect(SelectPlayerBoardCore core)
    {
        this.sc_players = core;
        selectedplayers.Clear();
        Status = Enum_GameBoardAction.PlayerSelecting;
    }

    public List<Player> GetSelectedPlayers()
    {
        return selectedplayers;
    }

    public PlayerFilter GetSelectPlayerFilterWithoutReplaced()
    {
        switch (Status)
        {
            case Enum_GameBoardAction.PlayerSelecting:
                return sc_players?.PlayerFilter;
            case Enum_GameBoardAction.PlayerAndCardSelecting:
                return sc_pncs?.PlayerFilter;
            case Enum_GameBoardAction.CardUsing:
                return skillinitative?.TargetFilter ?? cardwillused?.TargetFilter;
        }
        return null;
    }

    public PlayerFilter GetSelectPlayerFilter()
    {
        PlayerFilter result = GetSelectPlayerFilterWithoutReplaced();
        if (result == null) return result;
        return App.World.TryReplaceNewPlayerFilter(result, null);
    }

    protected void UpdatePlayerAboutSelection()
    {
        World world = App.World;
        Context ctx = world.GetContext();
        PlayerFilter targetfilter = GetSelectPlayerFilter();
        #region 告知已选择情况
        if (targetfilter is IKnowCardSelection)
        {
            IKnowCardSelection needknow = (IKnowCardSelection)targetfilter;
            #region 启用了转换技能
            if (skillconvert?.CardFilter != null && skillconvert?.CardConverter != null)
            {
                // 当前转换选择是否有效。
                bool isvalid;
                // 等待组合转换的卡。
                List<Card> remainscards;
                // 已经转换后的卡。
                List<Card> convertedcards = GetConvertedCards(out isvalid, out remainscards);
                // 进行告知。
                needknow.LetKnow(ctx, convertedcards);
            }
            #endregion
            #region 裸选择
            else
            {
                // 进行告知。
                needknow.LetKnow(ctx, selectedcards);
            }
            #endregion
        }
        #endregion
        #region 无目标筛选器
        if (targetfilter == null)
        {
            foreach (Player player in world.GetAlivePlayers().ToArray())
            {
                PlayerBe playerview = null;
                if (!player2views.TryGetValue(player, out playerview)) continue;
                playerview.IsEnterSelecting = false;
                playerview.CanSelect = false;
                playerview.IsSelected = false;
            }
        }
        #endregion
        #region 强制通过所有可行目标
        else if ((targetfilter.GetFlag(ctx) & Enum_PlayerFilterFlag.ForceAll) != Enum_PlayerFilterFlag.None)
        {
            selectedplayers.Clear();
            foreach (Player player in world.GetAlivePlayers().ToArray())
            {
                PlayerBe playerview = null;
                if (!player2views.TryGetValue(player, out playerview)) continue;
                playerview.IsEnterSelecting = true;
                if (targetfilter.CanSelect(ctx, selectedplayers, player))
                {
                    selectedplayers.Add(player);
                    playerview.IsSelected = true;
                    playerview.CanSelect = false;
                }
                else
                {
                    playerview.IsSelected = false;
                    playerview.CanSelect = false;
                }
            }
        }
        #endregion
        #region 进行可选择判定
        else
        {
            foreach (Player player in world.GetAlivePlayers().ToArray())
            {
                PlayerBe playerview = null;
                if (!player2views.TryGetValue(player, out playerview)) continue;
                playerview.IsEnterSelecting = true;
                if (selectedplayers.Contains(player))
                {
                    playerview.IsSelected = true;
                    playerview.CanSelect = false;
                }
                else
                {
                    playerview.IsSelected = false;
                    playerview.CanSelect = targetfilter.CanSelect(ctx, selectedplayers, player);
                }
            }
        }
        #endregion
    }

    public void PlayerCard_MouseDown(PlayerBe playerview)
    {
        if (playerview.IsSelected)
        {
            //playerview.IsSelected = false;
            selectedplayers.Remove(playerview.Core);
            UpdateAllControlUIs(ControlFlags.All & ~ControlFlags.ExtraAreaList);
        }
        else if (playerview.CanSelect)
        {
            //playerview.IsSelected = true;
            selectedplayers.Add(playerview.Core);
            UpdateAllControlUIs(ControlFlags.All & ~ControlFlags.ExtraAreaList);
        }
    }

    #endregion

    #region Player & Card Select

    public void BeginPlayerAndCardSelect(SelectPlayerAndCardBoardCore core)
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherSelectPlayerAndCard(core)); });
    }

    protected void Dispatcher_BeginPlayerAndCardSelect(SelectPlayerAndCardBoardCore core)
    {
        this.sc_pncs = core;
        this.skillconvert = null;
        this.skillinitative = null;
        selectedcards.Clear();
        selectedinitcards.Clear();
        selectedplayers.Clear();
        Status = Enum_GameBoardAction.PlayerAndCardSelecting;
    }

    protected bool CanEndPlayerAndCardSelectWithYes()
    {
        if (sc_pncs?.PlayerFilter?.Fulfill(App.World.GetContext(), selectedplayers) != true) return false;
        if (skillconvert != null)
        {
            bool isvalid;
            List<Card> remainscards;
            GetConvertedCards(out isvalid, out remainscards);
            return isvalid;
        }
        return sc_pncs?.CardFilter?.Fulfill(App.World.GetContext(), selectedcards) == true;
    }

    #endregion

    #region List Select

    public void BeginListSelect(ListBoardCore core)
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherSelectList(core)); });
    }

    protected void Dispatcher_BeginListSelect(ListBoardCore core)
    {
        this.sc_list = core;
        selecteditems.Clear();
        UI_ListPanel.Core = core;
        Status = Enum_GameBoardAction.ListSelecting;
    }

    #endregion

    #region Desktop Board

    public void ShowDesktopBoard(DesktopCardBoardCore core)
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherSelectDesktop(core, DispatcherSelectDesktop.Enum_Action.Open)); });
    }

    public void ControlDesktopBoard(DesktopCardBoardCore core)
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherSelectDesktop(core, DispatcherSelectDesktop.Enum_Action.Control)); });
    }

    public void CloseDesktopBoard(DesktopCardBoardCore core)
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherSelectDesktop(core, DispatcherSelectDesktop.Enum_Action.Close)); });
    }

    protected void Dispatcher_ShowDesktopBoard(DesktopCardBoardCore core)
    {
        core.SelectedCards.Clear();
        UI_DesktopBoard.Core = core;
        App.Show(UI_DesktopBoard);
        UI_DesktopBoard.IsControlling = false;
        if (core.IsAsync)
            console.WorldContiune();
        else
            UI_DesktopBoard.IsControlling = true;
    }

    protected void Dispatcher_ControlDesktopBoard(DesktopCardBoardCore core)
    {
        core.SelectedCards.Clear();
        UI_DesktopBoard.IsControlling = true;
    }

    protected void Dispatcher_CloseDesktopBoard(DesktopCardBoardCore core)
    {
        UI_DesktopBoard.IsControlling = false;
        UI_DesktopBoard.Core = null;
        App.Hide(UI_DesktopBoard);
    }

    public void UI_DesktopBoard_Yes()
    {
        DesktopCardBoardCore core = UI_DesktopBoard.Core;
        core.IsYes = true;
        if (!core.IsAsync)
        {
            UI_DesktopBoard.Core = null;
            App.Hide(UI_DesktopBoard);
        }
        Status = Enum_GameBoardAction.None;
        UI_DesktopBoard.IsControlling = false;
        console.WorldContiune();
    }

    public void UI_DesktopBoard_No()
    {
        DesktopCardBoardCore core = UI_DesktopBoard.Core;
        core.IsYes = false;
        core.SelectedCards.Clear();
        if (!core.IsAsync)
        {
            UI_DesktopBoard.Core = null;
            App.Hide(UI_DesktopBoard);
        }
        Status = Enum_GameBoardAction.None;
        UI_DesktopBoard.IsControlling = false;
        console.WorldContiune();
    }

    #endregion

    #region Use Card State

    public void BeginUseCardState(Context ctx)
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherQuestUseCard()); });
    }
    
    protected void Dispatcher_BeginUseCardState(Context ctx)
    {
        this.usecardfilter = new UseCardStateCardFilter(ctx);
        this.selectedevent = new LeaveUseCardStateEvent(ctx.World.GetPlayerState());
        this.cardwillused = null;
        this.skillinitative = null;
        this.skillconvert = null;
        selectedcards.Clear();
        selectedinitcards.Clear();
        selectedplayers.Clear();
        Status = Enum_GameBoardAction.CardUsing;
    }

    public TouhouSha.Core.Event GetEventInUseCardState()
    {
        return selectedevent;
    }

    protected void UpdateCardWillUsed()
    {
        if (Status != Enum_GameBoardAction.CardUsing
         || skillinitative != null)
        {
            this.cardwillused = null;
            return;
        }
        Card _cardwillused = cardwillused;
        try
        {
            if (skillconvert != null)
            {
                bool isvalid = false;
                List<Card> remains;
                List<Card> converts = GetConvertedCards(out isvalid, out remains);
                if (!isvalid)
                    this.cardwillused = null;
                else
                    this.cardwillused = converts.FirstOrDefault();
                return;
            }
            this.cardwillused = selectedcards.FirstOrDefault();
        }
        catch (Exception exce)
        {
            //MessageBox.Show(exce.StackTrace, exce.Message);
        }
        finally
        {
            if ((cardwillused != null ^ _cardwillused != null)
             || (cardwillused != null && _cardwillused != null && cardwillused.KeyName?.Equals(_cardwillused.KeyName) != true))
            {
                selectedplayers.Clear();
            }
        }
    }

    protected bool CanPostEventInUseCardState()
    {
        Context ctx = App.World.GetContext();
        CardFilter cardfilter = GetSelectCardFilter();
        PlayerFilter playerfilter = GetSelectPlayerFilter();
        if (cardfilter == null) return false;
        if (playerfilter == null) return false;
        if (!playerfilter.Fulfill(ctx, selectedplayers)) return false;
        if (skillconvert != null)
        {
            bool isvalid = false;
            List<Card> remains;
            GetConvertedCards(out isvalid, out remains);
            if (!isvalid) return false;
        }
        else
        {
            if (!cardfilter.Fulfill(ctx, selectedcards)) return false;
        }
        return true;
    }

    protected void PostEventInUseCardState()
    {
        if (!CanPostEventInUseCardState())
        {
            State state = App.World.GetPlayerState();
            this.selectedevent = new LeaveUseCardStateEvent(state);
            return;
        }
        if (skillinitative != null)
        {
            List<Card> costs = GetSelectedCards();
            List<Player> targets = GetSelectedPlayers();
            SkillInitativeEvent ev = new SkillInitativeEvent();
            ev.Skill = skillinitative as Skill;
            ev.SkillFrom = skillinitativefrom;
            ev.User = CurrentPlayer;
            ev.Costs.Clear();
            ev.Costs.AddRange(costs);
            ev.Targets.Clear();
            ev.Targets.AddRange(targets);
            this.selectedevent = ev;
            return;
        }
        if (cardwillused != null)
        {
            bool isyes_notused = false;
            List<Player> targets = GetSelectedPlayers();
            CardEvent ev = new CardEvent();
            ev.Card = cardwillused;
            ev.Source = CurrentPlayer;
            ev.Targets.Clear();
            ev.Targets.AddRange(targets);
            ev.Skill = skillconvert as Skill;
            ev.SkillFrom = skillconvertfrom;
            this.selectedevent = ev;
            return;
        }
        {
            State state = App.World.GetPlayerState();
            this.selectedevent = new LeaveUseCardStateEvent(state);
            return;
        }
    }

    protected void PostLeaveEventInUseCardState()
    {
        State state = App.World.GetPlayerState();
        this.selectedevent = new LeaveUseCardStateEvent(state);
    }

    #endregion

    #region Display

    public float BeginDisplay(IEnumerable<Card> cards)
    {
        List<Card> common2displays = new List<Card>();
        Dictionary<Player, List<Card>> player2displays = new Dictionary<Player, List<Card>>();
        float time = 0.0f;
        foreach (Card card in cards)
        {
            if (card.Zone?.Owner == null)
            {
                common2displays.AddRange(card.GetInitialCards());
            }
            else
            {
                List<Card> displays = null;
                if (!player2displays.TryGetValue(card.Zone.Owner, out displays))
                {
                    displays = new List<Card>();
                    player2displays.Add(card.Zone.Owner, displays);
                }
                displays.AddRange(card.GetInitialCards());
            }
        }
        if (common2displays.Count() > 0)
        {
            Zone commonzone = App.World.CommonZones.FirstOrDefault(_zone => _zone.KeyName?.Equals(Zone.Draw) == true);
            Transform place = (GetAreaNotKept(commonzone) as MonoBehaviour)?.gameObject?.transform;
            time = Math.Max(time, PlaceDisplays(place, common2displays));
        }
        foreach (KeyValuePair<Player, List<Card>> kvp in player2displays.ToArray())
        {
            if (kvp.Key == CurrentPlayer) continue;
            Zone handzone = kvp.Key.Zones.FirstOrDefault(_zone => _zone.KeyName?.Equals(Zone.Hand) == true);
            Transform place = (GetAreaNotKept(handzone) as MonoBehaviour)?.gameObject?.transform;
            time = Math.Max(time, PlaceDisplays(place, kvp.Value));
        }
        return time;
    }

    public float EndDisplay(IEnumerable<Card> cards)
    {
        float time = 0.0f;
        foreach (Card card in cards)
        {
            CardBe cardview = GetView(card);
            if (cardview == null) continue;
            // 自己的手牌始终可见，不是展示后可见。
            if (card.Zone?.KeyName?.Equals(Zone.Hand) == true
             && card.Owner == CurrentPlayer) continue;
            // 桌面区根据桌面控制，不以取消展示而不可见。
            if (card.Zone?.KeyName?.Equals(Zone.Desktop) == true
             || card.Zone?.KeyName?.Equals(Zone.Discard) == true) continue;
            // 消失动画过渡。
            //Hide(cardview);
            cardview.WaitHide();
            time = Math.Max(time, cardview.GetEllapsedTime());
        }
        return time;
    }

    protected float PlaceDisplays(Transform place, IEnumerable<Card> cards)
    {
        Vector3 p = place.position;
        float time = 0.0f;
        foreach (Card card in cards)
        {
            CardBe cardview = Show(card);
            if (cardview == null) continue;
            RectTransform cardview_rt = cardview.gameObject.GetComponent<RectTransform>();
            cardview.Position = p;
            cardview.IsFaceDown = false;
            cardview.ShowWait();
            p.x += cardview_rt.rect.width;
            time = Math.Max(time, cardview.GetEllapsedTime());
        }
        return time;
    }

    #endregion

    #region Hand Match

    public void BeginHandMatch()
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherHandMatch()); });
    }

    protected void Dispatcher_BeginHandMatch()
    {
        RectTransform rt = gameObject.GetComponent<RectTransform>();
        List<Button> btns = new List<Button>();
        float aw = rt.rect.width;
        float ah = rt.rect.height;
        float cw = 0;
        float ch = 0;

        btns.Add(BN_MyFist);
        btns.Add(BN_MyCut);
        btns.Add(BN_MyCloth);
        foreach (Button btn in btns)
        {
            RectTransform btn_rt = btn.gameObject.GetComponent<RectTransform>();
            cw += btn_rt.rect.width + 24;
            ch = Math.Max(ch, btn_rt.rect.height + 24);
        }
        float x0 = (aw - cw) / 2;
        float y0 = 60;
        foreach (Button btn in btns)
        {
            RectTransform btn_rt = btn.gameObject.GetComponent<RectTransform>();
            App.Show(btn);
            btn_rt.localPosition = new Vector3(x0 + 12, y0);
            x0 += btn_rt.rect.width + 24;
        }
        App.Hide(BN_YourHand);
        Status = Enum_GameBoardAction.HandMatching;
    }

    public HandMatchGesture GetHandMatchResult()
    {
        return handgesture;
    }

    #endregion

    #region KOF

    public void ShowKOF(KOFSelectCore core)
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherSelectKOF(core, DispatcherSelectKOF.Enum_Action.Open)); });
    }

    public void ControlKOF(KOFSelectCore core)
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherSelectKOF(core, DispatcherSelectKOF.Enum_Action.Control)); });
    }

    public void CloseKOF(KOFSelectCore core)
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherSelectKOF(core, DispatcherSelectKOF.Enum_Action.Close)); });
    }

    protected void Dispatcher_ShowKOF(KOFSelectCore core)
    {
        UI_KOF.Core = core;
        Status = Enum_GameBoardAction.KOFCharactorSelecting;
    }

    protected void Dispatcher_ControlKOF(KOFSelectCore core)
    {
        UI_KOF.BeginSelect();
    }

    protected void Dispatcher_CloseKOF(KOFSelectCore core)
    {
        App.Hide(UI_KOF);
    }

    #endregion

    #region Drag & Drop

    public void DragEnter(CardBe cardview)
    {
        isdragging = true;
    }

    public void DragMove(CardBe cardview)
    {
        if (cardview.Core == null) return;
        Card newcard = null;
        RectTransform cr = cardview.gameObject.GetComponent<RectTransform>();
        Vector3 cp = cardview.gameObject.transform.position;
        PlayerBe hit = null;
        float hitdist = float.MaxValue;
        if (!usecardconverts.TryGetValue(cardview.Core, out newcard))
        {
            newcard = App.World.CalculateCard(App.World.GetContext(), cardview.Core);
            usecardconverts.Add(cardview.Core, newcard);
        }
        foreach (PlayerBe playerview in player2views.Values)
        {
            RectTransform pr = playerview.gameObject.GetComponent<RectTransform>();
            Vector3 pp = playerview.gameObject.transform.position;
            if (Math.Abs(pp.x - cp.x) > (cr.rect.width + pr.rect.width) / 2) continue;
            if (Math.Abs(pp.y - cp.y) > (cr.rect.height + pr.rect.height) / 2) continue;
            float dist = (pp - cp).magnitude;
            if (hit == null || hitdist > dist)
            {
                hit = playerview;
                hitdist = dist;
            }
        }
        if (hit == null && cp.y > 300)
        {
            hit = UI_CurrentPlayer;
            hitdist = 0;
        }
        if (hit != null)
        {
            isdragging = true;

            KeyValuePair<Card, Player> kvp = new KeyValuePair<Card, Player>(cardview.Core, hit.Core);
            if (selectedinitcards.Count() != 1 
             || !selectedinitcards.ContainsKey(cardview.Core))
            {
                selectedcards.Clear();
                selectedcards.Add(newcard);
                selectedinitcards.Clear();
                selectedinitcards.Add(cardview.Core, newcard);
                selectedplayers.Clear();
                UpdateAllControlUIs(ControlFlags.All & ~ControlFlags.ExtraAreaList);
            }

            PlayerFilter targetfilter = null;
            Context ctx = App.World.GetContext();
            if (!usecardtargetfilters.TryGetValue(cardview.Core, out targetfilter))
            {
                targetfilter = GetSelectPlayerFilter();
                usecardtargetfilters.Add(cardview.Core, targetfilter);
            }

            if ((targetfilter.GetFlag(ctx) & Enum_PlayerFilterFlag.ForceAll) == Enum_PlayerFilterFlag.None
             && targetfilter.CanSelect(ctx, new Player[] { }, hit.Core))
                if (selectedplayers.Count() != 1
                 || selectedplayers[0] != hit.Core)
                {
                    selectedplayers.Clear();
                    selectedplayers.Add(hit.Core);
                    UpdateAllControlUIs(ControlFlags.All & ~ControlFlags.ExtraAreaList);
                }
        }
        else
        {
            isdragging = false;

            if (selectedcards.Count() > 0
             || selectedplayers.Count() > 0)
            {
                selectedcards.Clear();
                selectedplayers.Clear();
                UpdateAllControlUIs(ControlFlags.All & ~ControlFlags.ExtraAreaList);
            }
        }
    }

    public void Drop(CardBe cardview)
    {
        if (!isdragging) return;
        isdragging = false;
        if (!CanPostEventInUseCardState()) return; 
        UI_Ask_Yes();
    }

    #endregion

    #endregion

    #region Onlines

    private void GameStartLater()
    {
        // 检查网络连接
        Room room = PhotonNetwork.CurrentRoom;
        if (room == null || room.IsOffline) throw new Exception("已断开连接。");
        // 主机运行逻辑
        if (PhotonNetwork.IsMasterClient)
            App.World.GameStart();
    }

    public void ConnectRemoteConsolesAsync()
    {
        _toconnectremoteconsoles = true;
    }

    private void ConnectRemoteConsoles()
    {
        _toconnectremoteconsoles = false;
        if (!PhotonNetwork.IsMasterClient) return;
        GameCom[] gcs = GameComs.ToArray();
        RemoteConsole[] rcs = RemoteConsoles.ToArray();
        GameCom gc_master = gcs.FirstOrDefault(_com => _com.photonView.IsMine);
        foreach (RemoteConsole console in rcs)
        {
            GameCom gc_reader = gcs.FirstOrDefault(_com => _com.photonView.Owner != null && _com.photonView.Owner.UserId?.Equals(console.PunId) == true);
            if (console.Writer == null)
                console.Writer = gc_master;
            if (console.Reader == null)
                console.Reader = gc_reader;
        }
    }

    public void PlayerRegisterInClient(List<string> punids)
    {
        World world = App.World;
        AIPlayerRegister airegister = new AIPlayerRegister();
        world.AIRegister = airegister;
        world.PlayerRegisters.Clear();
        for (int i = 0; i < world.PlayerCount; i++)
        {
            if (i >= punids.Count()
             || String.IsNullOrEmpty(punids[i])
             || !PhotonNetwork.LocalPlayer.UserId.Equals(punids[i]))
                world.PlayerRegisters.Add(null);
            else
                world.PlayerRegisters.Add(new CurrentPlayerRegister(this));
        }
    }

    #endregion 

    #endregion

    #region Event Handler

    private void World_UIEvent(object sender, UIEvent e)
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherUIEvent(e)); });
        worldwait.WaitOne();
    }

    private void World_UIEvent_Dispatcher(UIEvent e)
    {
        //Dispatcher.BeginInvoke((ThreadStart)delegate ()
        {
            // 世界继续运行前的等待时间。
            float worldwaittime = 0.0f;
            try
            {
                #region 卡片Phase迭代（用于桌面卡半显示和隐藏）
                UIEvent2CardViewPhase(e);
                #endregion
                #region 虚假等待开始
                if (e is UIFakeWaitBeginEvent)
                {
                    UIFakeWaitBeginEvent fw = (UIFakeWaitBeginEvent)e;
                    if (fw.Player != CurrentPlayer)
                    {
                        PlayerBe playercard = null;
                        if (player2views.TryGetValue(fw.Player, out playercard))
                            playercard.BeginFakeWait(fw.Timeout);
                    }
                }
                #endregion
                #region 虚假等待结束
                if (e is UIFakeWaitEndEvent)
                {
                    UIFakeWaitEndEvent fw = (UIFakeWaitEndEvent)e;
                    if (fw.Player != CurrentPlayer)
                    {
                        PlayerBe playercard = null;
                        if (player2views.TryGetValue(fw.Player, out playercard))
                            playercard.EndFakeWait();
                    }
                }
                #endregion
                #region 输出报告
                if (e is UIEventFromLogical)
                {
                    ShaText shatext = ((UIEventFromLogical)e).GetText();
                    if (shatext != null)
                    {
                        List<Player> relateds = new List<Player>();
                        UI_OutputBox.AppendLine(shatext);
                        /*
                        foreach (Player related in ((UIEventFromLogical)e).GetRelatedPlayers())
                        {
                            if (relateds.Contains(related)) continue;
                            relateds.Add(related);
                            UI_OutputBox.AppendLine(related, shatext);
                        }
                        */
                    }
                }
                #endregion
                #region 搜集动画事件
                moveevents.Clear();
                cardevents.Clear();
                cardacceptevents.Clear();
                cardfailedevents.Clear();
                skillactiveevents.Clear();
                skillcoolevents.Clear();
                stateevents.Clear();
                damageevents.Clear();
                healevents.Clear();
                switchchairevents.Clear();
                notkeptareatotals.Clear();
                cardshowevents.Clear();
                cardhideevents.Clear();
                cardwinevents.Clear();
                cardloseevents.Clear();
                UIEventFind(e);
                #endregion
                #region 卡片展示动画
                foreach (UICardShowEvent ev in cardshowevents)
                    worldwaittime = Math.Max(worldwaittime, BeginDisplay(ev.Cards));
                #endregion
                #region 卡牌取消展示动画
                foreach (UICardHideEvent ev in cardhideevents)
                    worldwaittime = Math.Max(worldwaittime, EndDisplay(ev.Cards));
                #endregion
                #region 卡片移动动画
                // 更改过的卡片区，需要有动画来重整理布局。
                HashSet<IGameBoardArea> changedareas = new HashSet<IGameBoardArea>();
                // 桌面区的UI容器。
                IGameBoardArea desktop_area = UI_DesktopPlacer;
                // 桌面区的矩形转换
                RectTransform desktop_rt = UI_DesktopPlacer.gameObject.GetComponent<RectTransform>();
                // 桌面区的理想容量，建议尽量非折叠显示所有卡片。
                int desktop_capacity = (int)(desktop_rt.rect.width / CardBe.DefaultWidth);
                // 事先检查桌面牌数是否超过了理想容量，先进先出去掉卡牌。
                while (desktop_area.Cards.Count() > desktop_capacity)
                {
                    List<Card> removedlist = new List<Card>();
                    int removeds = desktop_area.Cards.Count() - desktop_capacity;
                    while (desktop_area.Cards.Count() > 0 && removeds-- > 0)
                    {
                        removedlist.Add(desktop_area.Cards[0]);
                        desktop_area.Cards.RemoveAt(0);
                    }
                    foreach (Card card in removedlist)
                        worldwaittime = Math.Max(worldwaittime, AnimHideDesktop(card) + 0.5f);
                }
                // UI卡片容器区的添加和删除。
                // 卡片逻辑区映射一个卡片容器区，逻辑区的移入和移除同样对应于容器区。
                // 注意，弃牌区和桌面区同样映射于UI桌面区。
                foreach (UIMoveCardEvent movecard in moveevents)
                    foreach (Card card0 in movecard.MovedCards)
                        foreach (Card card in card0.GetInitialCards())
                        {
                            IGameBoardArea area0 = GetArea(movecard.OldZone);
                            IGameBoardArea area1 = GetArea(movecard.NewZone);
                            if (area0 != null && area0 != area1 && area0.KeptCards)
                            {
                                area0.Cards.Remove(card);
                                if (!changedareas.Contains(area0))
                                    changedareas.Add(area0);
                            }
                            if (area1 != null && area1 != area0 && area1.KeptCards)
                            {
                                area1.Cards.Add(card);
                                if (!changedareas.Contains(area1))
                                    changedareas.Add(area1);
                            }
                            // 新进入桌面的卡，给分配最新的PhaseId。
                            if (area1 == UI_DesktopPlacer && area1 != area0)
                            {
                                CardBe cardview = GetView(card);
                                if (cardview != null)
                                    cardview.PhaseId = CardBe.PhaseNow;
                            }
                        }
                // 对桌面卡片进行阶段性半隐藏/隐藏
                {
                    IGameBoardArea area = UI_DesktopPlacer;
                    int firstindex = 0;
                    while (firstindex < area.Cards.Count())
                    {
                        Card firstcard = area.Cards[firstindex];
                        CardBe firstcardview = GetView(firstcard);
                        if (firstcardview == null)
                        {
                            firstindex++;
                            continue;
                        }
                        if (firstcardview.PhaseId < CardBe.PhaseNow - 1)
                        {
                            if (firstindex > 0) break;
                            worldwaittime = Math.Max(worldwaittime, AnimHideDesktop(firstcard));
                            area.Cards.RemoveAt(0);
                            continue;
                        }
                        else if (firstcardview.PhaseId < CardBe.PhaseNow)
                        {
                            /*
                            if (!firstcardview.IsHalfVisible)
                            {
                                CardWaitHide halfhide = new CardWaitHide();
                                halfhide.Half = CardHideHalf.Full2Half;
                                halfhide.WaitTime = 0;
                                halfhide.TimeMax = 10;
                                firstcardview.IsHalfVisible = true;
                                foreach (CardAnim cardanim in firstcardview.Anims.ToArray())
                                    if (cardanim is CardShowWait || cardanim is CardWaitHide || cardanim is CardShowWaitHide)
                                        firstcardview.Anims.Remove(cardanim);
                                //firstcardview.Anims.Clear();
                                firstcardview.Anims.Add(halfhide);
                            }
                            */
                        }
                        else
                        {
                            break;
                        }
                        firstindex++;
                    }
                }
                // 给每个移动卡片注册卡片动画，以及明牌暗牌的处理。
                // 并分配新的ZIndex，防止堆叠不整齐的现象。
                foreach (UIMoveCardEvent movecard in moveevents)
                {
                    bool isfacedown = !movecard.IsCardVisibled;
                    if (movecard.NewZone?.Owner == CurrentPlayer)
                        isfacedown = false;
                    foreach (Card card0 in movecard.MovedCards)
                        foreach (Card card in card0.GetInitialCards())
                        {
                            //if (card2anims.ContainsKey(card)) continue;
                            worldwaittime = Math.Max(worldwaittime, AnimMoveCrossArea(card, movecard.OldZone, movecard.NewZone) + 0.5f);
                            CardBe cardview = GetView(card);
                            if (cardview != null)
                            {
                                // 被移动的卡片放置到最上层。
                                cardview.transform.SetParent(null);
                                cardview.transform.SetParent(CV_Cards.transform);
                                //Panel.SetZIndex(cardview, ++cardzindex);
                                // 设置正面/背面
                                cardview.IsFaceDown = isfacedown;
                                // 新进入桌面的卡，给分配最新的PhaseId。
                                IGameBoardArea area0 = GetArea(movecard.OldZone);
                                IGameBoardArea area1 = GetArea(movecard.NewZone);
                                if (area1 == UI_DesktopPlacer && area1 != area0)
                                    cardview.PhaseId = CardBe.PhaseNow;
                            }
                        }
                }
                // 给每个更改过的卡片区注册整理动画。
                foreach (IGameBoardArea area in changedareas)
                    foreach (Card card in area.Cards)
                        worldwaittime = Math.Max(worldwaittime, AnimArrange(card, area));
                // 通知桌面面板移动事件
                if (UI_DesktopBoard.Core != null)
                    UI_DesktopBoard.IvCardMoved(moveevents);
                #endregion
                #region 卡片使用动画
                // 绘制指向目标的直线型动画。
                foreach (UICardEvent ev in cardevents)
                {
                    // 显示和目标相关的动画。
                    if (ev.ShowTargets)
                    {
                        // 显示向目标的连线。
                        foreach (Player target in ev.CardTargets)
                            LineTarget(ev.CardUser, target);
                        // 决斗的对方也显示动画。
                        if (ev.Card.KeyName?.Equals(DuelCard.Normal) == true)
                            foreach (Player target in ev.CardTargets)
                                AnimationByName(target, ev.Card.KeyName);
                    }
                    // 显示出牌动画。
                    AnimationByName(ev.CardUser, ev.Card.KeyName);
                    // 发出声音。
                    AudioClip clip = Resources.Load<AudioClip>("Voices/" + ev.Card.KeyName);
                    if (clip != null)
                    {
                        Voice.clip = clip;
                        Voice.Play();
                    }
                }
                #endregion
                #region 卡片通过动画
                foreach (UICardAcceptEvent ev in cardacceptevents)
                    foreach (Card card0 in ev.Cards)
                        foreach (Card card in card0.GetInitialCards())
                            AnimationByName(card, "JudgeGood");
                #endregion
                #region 卡片失败动画
                foreach (UICardFailedEvent ev in cardfailedevents)
                    foreach (Card card0 in ev.Cards)
                        foreach (Card card in card0.GetInitialCards())
                            AnimationByName(card, "JudgeBad");
                #endregion
                #region 拼点获胜动画
                foreach (UICardWinEvent ev in cardwinevents)
                    foreach (Card card0 in ev.Cards)
                        foreach (Card card in card0.GetInitialCards())
                            AnimationByName(card, "Win");
                #endregion
                #region 拼点失败动画
                foreach (UICardLoseEvent ev in cardloseevents)
                    foreach (Card card0 in ev.Cards)
                        foreach (Card card in card0.GetInitialCards())
                            AnimationByName(card, "Lose");
                #endregion
                #region 技能使用动画
                foreach (UISkillActive ev in skillactiveevents)
                {
                    // 发动者卡牌上方，浮动文字宣称技能。
                    if (ev.Skill != null)
                        AnimSkillActive(ev.SkillActiver, ev.Skill);
                    // 绘制指向目标的直线型动画。
                    if (ev.ShowTargets)
                        foreach (Player target in ev.SkillTargets)
                            LineTarget(ev.SkillActiver, target);
                }
                #endregion
                #region 限定技/觉醒技过场动画
                foreach (UISkillCoolAnimEvent ev in skillcoolevents)
                {
                    if (UI_CoolSkill.Event != null) break;
                    UI_CoolSkill.Event = ev;
                    App.Show(UI_CoolSkill);
                    worldwaittime = Math.Max(worldwaittime, UI_CoolSkill.MaxTime);
                }
                //screenlabelwait.Reset();
                #endregion
                #region 伤害动画技能
                // 部署伤害动画
                foreach (UIDamageEvent ev in damageevents)
                    AnimationDamage(ev.Target, ev);
                #endregion
                #region 回复动画技能
                // 部署回复动画
                foreach (UIHealEvent ev in healevents)
                    AnimationHeal(ev.Target, ev);
                #endregion
                #region 座次交换技能
                foreach (UISwitchChairEvent ev in switchchairevents)
                {
                    AnimationSwitchChair(ev.Source);
                    AnimationSwitchChair(ev.Target);
                }
                if (switchchairevents.Count() > 0)
                    LayoutPlayers();
                #endregion
                #region 状态切换
                if (stateevents.Count() > 0)
                    UpdatePlayerStates();
                #endregion
                #region 猜拳动画
                if (e is UIHandMatchEvent)
                {
                    UIHandMatchEvent hm = (UIHandMatchEvent)e;
                    HandMatchGesture ges0 = HandMatchGesture.Fist;
                    HandMatchGesture ges1 = HandMatchGesture.Fist;
                    Button btn0 = null;
                    RectTransform rt = gameObject.GetComponent<RectTransform>();
                    if (hm.Source == CurrentPlayer)
                    {
                        ges0 = hm.SourceHand;
                        ges1 = hm.TargetHand;
                    }
                    else
                    {
                        ges0 = hm.TargetHand;
                        ges1 = hm.SourceHand;
                    }
                    switch (ges0)
                    {
                        case HandMatchGesture.Fist: btn0 = BN_MyFist; break;
                        case HandMatchGesture.Cloth: btn0 = BN_MyCloth; break;
                        case HandMatchGesture.Cut: btn0 = BN_MyCut; break;
                    }
                    handanim = new HandMatchAnimation();
                    handanim.MaxTime = 1.0f;
                    handanim.TotalTime = 0.0f;
                    handanim.Button0 = btn0;
                    handanim.Button1 = BN_YourHand;
                    handanim.StartPoint0 = btn0.transform.position;
                    handanim.StartPoint1 = BN_YourHand.transform.position;
                    handanim.EndPoint0 = new Vector3(rt.rect.width / 2, -rt.rect.height / 2 + 200);
                    handanim.EndPoint1 = new Vector3(rt.rect.width / 2, -rt.rect.height / 2 - 200);
                    foreach (Button btn in new Button[] { BN_MyFist, BN_MyCloth, BN_MyCut })
                        if (btn != btn0) App.Hide(btn);
                    worldwaittime = Math.Max(worldwaittime, 1.0f);
                }
                #endregion
                #region 队列下一名登场
                if (e is UIKOFNextEvent)
                {
                    UIKOFNextEvent kof_next = (UIKOFNextEvent)e;
                    Player player = kof_next.Source;
                    PlayerBe playerview = player2views[player];
                    playerview.UpdateInfo();
                    if (player == CurrentPlayer)
                        UI_KOFList0.UpdateList();
                    else
                        UI_KOFList1.UpdateList();
                }

                #endregion
            }
            catch (Exception exce)
            {

            }
            // 无需等待动画。
            if (e is UIEventFromLogical && moveevents.Count() > 0)
            {
                UIEventFromLogical evfl = (UIEventFromLogical)e;
                print(String.Format("Ev={0} worldwaittime={1}", evfl.LogicalEvent, worldwaittime));
            }
            if (NoWaitAnimation || worldwaittime < 0.001f)
                World_UIEvent_Continue();
            // 延迟等待时间再Set信号量。
            else
                Invoke("World_UIEvent_Continue", worldwaittime);
        }
        //);
    }

    private void World_UIEvent_Continue()
    {
        App.Hide(UI_CoolSkill);
        worldwait.Set();
    }

    private void World_GameStartup(object sender, GameStartupEventArgs e)
    {
        //print(String.Format("World_GameStartup:{0}", e.E));
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherGameStartup(e)); });
        worldwait.WaitOne();
    }

    private void World_GameStartup_Dispatcher(GameStartupEventArgs e)
    {
        //print(String.Format("World_GameStartup_Dispatcher:{0}", e.E));
        switch (e.E)
        {
            case Enum_GameStartupEvent.DeterminedPlayerCount:
                //Dispatcher.Invoke(() =>
                if (!App.IsOnlineGame)
                {
                    World world = App.World;
                    AIPlayerRegister airegister = new AIPlayerRegister();
                    world.AIRegister = airegister;
                    world.PlayerRegisters.Clear();
                    world.PlayerRegisters.Add(new CurrentPlayerRegister(this));
                    while (world.PlayerRegisters.Count() < world.PlayerCount)
                        world.PlayerRegisters.Add(null);
                }//);
                else if (PhotonNetwork.IsMasterClient)
                {
                    World world = App.World;
                    Room room = PhotonNetwork.CurrentRoom;
                    if (room != null)
                    {
                        AIPlayerRegister airegister = new AIPlayerRegister();
                        List<string> chairs = new List<string>();
                        foreach (PunPlayer player in room.Players.Values)
                            chairs.Add(player.UserId);
                        while (chairs.Count() < world.PlayerCount)
                            chairs.Add(null);
                        world.Shuffle(chairs);
                        world.AIRegister = airegister;
                        world.PlayerRegisters.Clear();
                        for (int i = 0; i < chairs.Count(); i++)
                        {
                            string punid = chairs[i];
                            if (String.IsNullOrEmpty(punid))
                                world.PlayerRegisters.Add(null);
                            else if (punid.Equals(PhotonNetwork.LocalPlayer.UserId))
                                world.PlayerRegisters.Add(new CurrentPlayerRegister(this));
                            else
                                world.PlayerRegisters.Add(new RemotePlayerRegister(this, punid));
                        }
                        if (LocalGameCom != null)
                        {
                            ComPackageDetermineChairIndex compack = new ComPackageDetermineChairIndex();
                            compack.PunIds.Clear();
                            compack.PunIds.AddRange(chairs);
                            LocalGameCom.SendPackage(compack);
                        }
                    }
                }
                break;
            case Enum_GameStartupEvent.BuildPlayers:
                //Dispatcher.Invoke(() =>
                {
                    CurrentPlayer = App.World.Players.FirstOrDefault(_player => _player.Console is GameBoardConsole);
                    ResetPlayers();
                }//);
                break;
            case Enum_GameStartupEvent.AllocAsses:
                //Dispatcher.Invoke(() =>
                {
                    // 强制将主机设为主公。
                    if (App.IsOnlineGame
                     && PhotonNetwork.IsMasterClient)
                    {
                        Player player0 = CurrentPlayer;
                        Player player1 = App.World.Players.FirstOrDefault(_player => _player.Ass.E == Enum_PlayerAss.Leader);
                        if (player0 != null && player1 != null && player0 != player1)
                        {
                            PlayerAss temp0 = player0.Ass;
                            player0.Ass = player1.Ass;
                            player1.Ass = temp0;
                            bool temp1 = player0.IsAssVisibled;
                            player0.IsAssVisibled = player1.IsAssVisibled;
                            player1.IsAssVisibled = temp1;
                        }
                    }
                    foreach (Player player in App.World.Players)
                    {
                        //bool isassvisible = player.Ass?.E == Enum_PlayerAss.Leader;
                        //isassvisible |= player == CurrentPlayer;
                        //player.IsAssVisibled = isassvisible;
                        PlayerBe playercard = null;
                        if (!player2views.TryGetValue(player, out playercard)) continue;
                        playercard.UpdateSelectedAss();
                    }
                }//);
                break;
            case Enum_GameStartupEvent.LeaderSelectedCharactor:
                //Dispatcher.Invoke(() =>
                {
                    foreach (Player player in App.World.Players)
                    {
                        if (player.Ass?.E != Enum_PlayerAss.Leader) continue;
                        PlayerBe playercard = null;
                        if (!player2views.TryGetValue(player, out playercard)) continue;
                        playercard.UpdateInfo();
                    }
                }//);
                break;
            case Enum_GameStartupEvent.AllSelectedCharactor:
                //Dispatcher.Invoke(() =>
                {
                    foreach (Player player in App.World.Players)
                    {
                        PlayerBe playercard = null;
                        if (!player2views.TryGetValue(player, out playercard)) continue;
                        playercard.UpdateInfo();
                    }
                    if (App.World.GameMode == Enum_GameMode.KOF)
                    {
                        UI_KOFList0.Player = CurrentPlayer;
                        UI_KOFList1.Player = App.World.Players.FirstOrDefault(_player => _player != CurrentPlayer);
                        App.Show(UI_KOFList0);
                        App.Show(UI_KOFList1);
                    }
                    else
                    {
                        App.Hide(UI_KOFList0);
                        App.Hide(UI_KOFList1);
                    }
                    CharactorDetermined = true;
                }//);
                break;
            case Enum_GameStartupEvent.TotalCards:
                //Dispatcher.Invoke(() =>
                {
                    NoWaitAnimation = true;
                    AnimScreenLabel(Enum_ScreenCoolLabel.GameStart);
                }//);
                //screenlabelwait.WaitOne();
                break;
            case Enum_GameStartupEvent.AllocInitialHands:
                //Dispatcher.Invoke(() =>
                {
                    NoWaitAnimation = false;
                }//);
                break;
            case Enum_GameStartupEvent.GameOver:
                //Dispatcher.Invoke(() =>
                {
                    PlayerScore highest = null;
                    foreach (Player player in App.World.Players)
                    {
                        if (App.World.WinnerAss != null)
                        {
                            if (App.World.WinnerAss.E == Enum_PlayerAss.Leader)
                            {
                                if (player.Ass.E != Enum_PlayerAss.Leader
                                 && player.Ass.E != Enum_PlayerAss.Slave) continue;
                            }
                            else
                            {
                                if (player.Ass.E != App.World.WinnerAss.E) continue;
                            }
                        }
                        if (highest == null)
                            highest = player.Score;
                        else if (highest.GetScoreTotal() < player.Score.GetScoreTotal())
                            highest = player.Score;
                    }
                    App.Show(UI_Winner);
                    UI_Winner.Score = highest;
                }//);
                break;
        }
        worldwait.Set();
    }

    private void World_PostComment(object sender, PlayerCommentEventArgs e)
    {
        dispatcher_futex.Invoke(() => { dispatcher_queue.Enqueue(new DispatcherPostComment(e)); });
    }

    private void World_PostComment_Dispatcher(PlayerCommentEventArgs e)
    {
        UI_OutputBox.AppendComment(e.Player, e.Comment);
    }

    protected void UIEventFind(UIEvent e)
    {
        if (e is UIEventGroup)
        {
            UIEventGroup group = (UIEventGroup)e;
            foreach (UIEvent sub in group.Items)
                UIEventFind(sub);
        }
        else if (e is UIEventFromLogical)
        {
            UIEventFromLogical fromlogic = (UIEventFromLogical)e;
            if (fromlogic.LogicalEvent is MoveCardAnimEvent)
            {
                MoveCardAnimEvent mc = (MoveCardAnimEvent)(fromlogic.LogicalEvent);
                List<Card> initcards = new List<Card>();
                foreach (Card card in mc.MovedCards)
                    initcards.AddRange(card.GetInitialCards());
                UIMoveCardEvent uimc = new UIMoveCardEvent(initcards, mc.OldZone, mc.NewZone);
                uimc.IsCardVisibled = (mc.Flag & Enum_MoveCardFlag.FaceUp) != Enum_MoveCardFlag.None;
                moveevents.Add(uimc);
            }
            else if (fromlogic.LogicalEvent is CardAnimEvent)
            {
                CardAnimEvent cp = (CardAnimEvent)(fromlogic.LogicalEvent);
                UICardEvent uice = new UICardEvent();
                uice.Card = cp.Card;
                uice.CardUser = cp.Source;
                uice.CardTargets.Clear();
                uice.CardTargets.AddRange(cp.Targets);
                uice.ShowTargets = !(cp.Reason is CardEventBase);
                cardevents.Add(uice);
            }
            else if (fromlogic.LogicalEvent is CardAcceptEvent)
            {
                CardAcceptEvent ac = (CardAcceptEvent)(fromlogic.LogicalEvent);
                UICardAcceptEvent uiac = new UICardAcceptEvent();
                uiac.Target = ac.Target;
                uiac.Cards.Clear();
                uiac.Cards.AddRange(ac.Cards);
                cardacceptevents.Add(uiac);
            }
            else if (fromlogic.LogicalEvent is CardFailedEvent)
            {
                CardFailedEvent fe = (CardFailedEvent)(fromlogic.LogicalEvent);
                UICardFailedEvent uife = new UICardFailedEvent();
                uife.Target = fe.Target;
                uife.Cards.Clear();
                uife.Cards.AddRange(fe.Cards);
                cardfailedevents.Add(uife);
            }
            else if (fromlogic.LogicalEvent is SkillEvent)
            {
                SkillEvent se = (SkillEvent)(fromlogic.LogicalEvent);
                UISkillActive uisa = new UISkillActive();
                uisa.Skill = se.Skill;
                uisa.SkillActiver = se.Source;
                uisa.SkillTargets.Clear();
                uisa.SkillTargets.AddRange(se.Targets);
                skillactiveevents.Add(uisa);
            }
            else if (fromlogic.LogicalEvent is SkillCoolAnimEvent)
            {
                SkillCoolAnimEvent sc = (SkillCoolAnimEvent)(fromlogic.LogicalEvent);
                UISkillCoolAnimEvent uisc = new UISkillCoolAnimEvent();
                uisc.Skill = sc.Skill;
                uisc.Source = sc.Source;
                uisc.Comment0 = sc.Comment0;
                uisc.Comment1 = sc.Comment1;
                skillcoolevents.Add(uisc);
            }
            else if (fromlogic.LogicalEvent is StateChangeEvent)
            {
                StateChangeEvent sc = (StateChangeEvent)(fromlogic.LogicalEvent);
                UIStateChangeEvent uisc = new UIStateChangeEvent();
                uisc.OldState = sc.OldState;
                uisc.NewState = sc.NewState;
                uisc.StackDirection = sc.StackDirection;
                stateevents.Add(uisc);
            }
            else if (fromlogic.LogicalEvent is DamageDetermineEvent)
            {
                DamageDetermineEvent da = (DamageDetermineEvent)(fromlogic.LogicalEvent);
                UIDamageEvent uida = new UIDamageEvent();
                uida.Source = da.Source;
                uida.Target = da.Target;
                uida.DamageValue = da.DamageValue;
                uida.DamageType = da.DamageType;
                damageevents.Add(uida);
            }
            else if (fromlogic.LogicalEvent is HealDetermineEvent)
            {
                HealDetermineEvent he = (HealDetermineEvent)(fromlogic.LogicalEvent);
                UIHealEvent uihe = new UIHealEvent();
                uihe.Source = he.Source;
                uihe.Target = he.Target;
                uihe.HealValue = he.HealValue;
                uihe.HealType = he.HealType;
                healevents.Add(uihe);
            }
            else if (fromlogic.LogicalEvent is SwitchChairDoneEvent)
            {
                SwitchChairDoneEvent sc = (SwitchChairDoneEvent)(fromlogic.LogicalEvent);
                UISwitchChairEvent uisc = new UISwitchChairEvent();
                uisc.Source = sc.Source;
                uisc.Target = sc.Target;
                switchchairevents.Add(uisc);
            }
            else if (fromlogic.LogicalEvent is CardShowEvent)
            {
                CardShowEvent cs = (CardShowEvent)(fromlogic.LogicalEvent);
                UICardShowEvent uics = new UICardShowEvent();
                foreach (Card card in cs.ShowedCards)
                    uics.Cards.AddRange(card.GetInitialCards());
                cardshowevents.Add(uics);
            }
            else if (fromlogic.LogicalEvent is CardHideEvent)
            {
                CardHideEvent ch = (CardHideEvent)(fromlogic.LogicalEvent);
                UICardHideEvent uich = new UICardHideEvent();
                foreach (Card card in ch.ShowedCards)
                    uich.Cards.AddRange(card.GetInitialCards());
                cardhideevents.Add(uich);
            }
            else if (fromlogic.LogicalEvent is PointBattleAnimEvent)
            {
                PointBattleAnimEvent pbd = (PointBattleAnimEvent)(fromlogic.LogicalEvent);
                if (pbd.IsWin(pbd.Source))
                {
                    UICardWinEvent uiev = new UICardWinEvent(pbd.Source);
                    uiev.Cards.Add(pbd.SourceCard);
                    cardwinevents.Add(uiev);
                }
                else
                {
                    UICardLoseEvent uiev = new UICardLoseEvent(pbd.Source);
                    uiev.Cards.Add(pbd.SourceCard);
                    cardloseevents.Add(uiev);
                }
            }
        }
    }

    protected void UIEvent2CardViewPhase(UIEvent e)
    {
        if (e is UIEventGroup)
        {
            UIEventGroup group = (UIEventGroup)e;
            foreach (UIEvent sub in group.Items)
                UIEvent2CardViewPhase(sub);
        }
        else if (e is UIEventFromLogical)
        {
            UIEventFromLogical fromlogic = (UIEventFromLogical)e;
            if (fromlogic.LogicalEvent is StateChangeEvent)
            {
                StateChangeEvent sc = (StateChangeEvent)(fromlogic.LogicalEvent);
                if (sc.OldState != null && sc.NewState != null
                 && IsPlayerPhaseState(sc.OldState.KeyName)
                 && IsPlayerPhaseState(sc.NewState.KeyName))
                {
                    if (sc.OldState.Owner != sc.NewState.Owner)
                        CardBe.PhaseNow++;
                    else if (!sc.OldState.KeyName.Equals(sc.NewState.KeyName))
                        CardBe.PhaseNow++;
                }
            }
            else if (fromlogic.LogicalEvent is CardAnimEvent)
            {
                CardAnimEvent ca = (CardAnimEvent)(fromlogic.LogicalEvent);
                if (ca.Reason == null)
                    CardBe.PhaseNow++;
            }
            else if (fromlogic.LogicalEvent is SkillEvent)
            {
                SkillEvent se = (SkillEvent)(fromlogic.LogicalEvent);
                if (se.Reason == null)
                    CardBe.PhaseNow++;
            }
        }
    }

    protected bool IsPlayerPhaseState(string keyname)
    {
        switch (keyname)
        {
            case State.Begin:
            case State.Judge:
            case State.Draw:
            case State.UseCard:
            case State.Discard:
            case State.End:
                return true;
        }
        return false;
    }

    #region UI_Ask

    public void UI_Ask_Yes()
    {
        switch (Status)
        {
            case Enum_GameBoardAction.Asking:
                asked_yes = true;
                Status = Enum_GameBoardAction.None;
                console.WorldContiune();
                break;
            case Enum_GameBoardAction.CardSelecting:
                if (sc_cards != null)
                {
                    sc_cards.IsYes = true;
                    sc_cards.SelectedCards.Clear();
                    sc_cards.SelectedCards.AddRange(GetSelectedCards());
                    sc_cards.UsedConverter = skillconvert;
                    sc_cards.UsedConverterFromCard = skillconvertfrom;
                }
                Status = Enum_GameBoardAction.None;
                console.WorldContiune();
                break;
            case Enum_GameBoardAction.PlayerSelecting:
                if (sc_players != null)
                {
                    sc_players.IsYes = true;
                    sc_players.SelectedPlayers.Clear();
                    sc_players.SelectedPlayers.AddRange(GetSelectedPlayers());
                }
                Status = Enum_GameBoardAction.None;
                console.WorldContiune();
                break;
            case Enum_GameBoardAction.PlayerAndCardSelecting:
                if (sc_pncs != null)
                {
                    sc_pncs.IsYes = true;
                    sc_pncs.SelectedCards.Clear();
                    sc_pncs.SelectedCards.AddRange(GetSelectedCards());
                    sc_pncs.SelectedPlayers.Clear();
                    sc_pncs.SelectedPlayers.AddRange(GetSelectedPlayers());
                }
                Status = Enum_GameBoardAction.None;
                console.WorldContiune();
                break;
            case Enum_GameBoardAction.CardUsing:
                PostEventInUseCardState();
                Status = Enum_GameBoardAction.None;
                console.WorldContiune();
                break;
        }
    }

    public void UI_Ask_No()
    {
        if (App.IsVisible(UI_Yuji))
        {
            App.Hide(UI_Yuji);
            return;
        }
        if (skillconvert != null || skillinitative != null)
        {
            if (skillconvert != null)
            {
                UI_Skills.SetUncheck(skillconvert as Skill);
                skillconvert = null;
            }
            if (skillinitative != null)
            {
                UI_Skills.SetUncheck(skillinitative as Skill);
                skillinitative = null;
            }
            UpdateAllControlUIs();
            return;
        }
        switch (Status)
        {
            case Enum_GameBoardAction.Asking:
                asked_yes = false;
                Status = Enum_GameBoardAction.None;
                console.WorldContiune();
                break;
            case Enum_GameBoardAction.CardSelecting:
                if (sc_cards != null)
                {
                    sc_cards.IsYes = false;
                    sc_cards.SelectedCards.Clear();
                }
                Status = Enum_GameBoardAction.None;
                console.WorldContiune();
                break;
            case Enum_GameBoardAction.PlayerSelecting:
                if (sc_players != null)
                {
                    sc_players.IsYes = false;
                    sc_players.SelectedPlayers.Clear();
                }
                Status = Enum_GameBoardAction.None;
                console.WorldContiune();
                break;
            case Enum_GameBoardAction.PlayerAndCardSelecting:
                if (sc_pncs != null)
                {
                    sc_pncs.IsYes = false;
                    sc_pncs.SelectedCards.Clear();
                    sc_pncs.SelectedPlayers.Clear();
                }
                Status = Enum_GameBoardAction.None;
                console.WorldContiune();
                break;
            case Enum_GameBoardAction.CardUsing:
                PostLeaveEventInUseCardState();
                Status = Enum_GameBoardAction.None;
                console.WorldContiune();
                break;
        }
    }

    public void OnTimeout()
    {
        switch (Status)
        {
            case Enum_GameBoardAction.Asking:
                asked_yes = false;
                console.WorldContiune();
                break;
            case Enum_GameBoardAction.CardSelecting:
                CurrentPlayer.TrusteeshipConsole.SelectCards(sc_cards);
                Status = Enum_GameBoardAction.None;
                console.WorldContiune();
                break;
            case Enum_GameBoardAction.PlayerSelecting:
                CurrentPlayer.TrusteeshipConsole.SelectPlayers(sc_players);
                Status = Enum_GameBoardAction.None;
                console.WorldContiune();
                break;
            case Enum_GameBoardAction.PlayerAndCardSelecting:
                //CurrentPlayer.TrusteeshipConsole.SelectCardsAndPlayers(sc_pncs);
                Status = Enum_GameBoardAction.None;
                console.WorldContiune();
                break;
            case Enum_GameBoardAction.CardUsing:
                PostEventInUseCardState();
                Status = Enum_GameBoardAction.None;
                console.WorldContiune();
                break;
        }
    }

    #endregion

    #region UI_ListPanel

    public void UI_ListPanel_SelectedDone()
    {
        if (sc_list != null)
        {
            sc_list.IsYes = true;
            sc_list.SelectedItems.Clear();
            sc_list.SelectedItems.AddRange(UI_ListPanel.SelectedItems);
        }
        Status = Enum_GameBoardAction.None;
        console.WorldContiune();
    }

    #endregion

    #region UI_Skills

    public void UI_Skills_SkillChecked(Skill skill)
    {
        Context ctx = App.World.GetContext();
        Zone hand = CurrentPlayer.Zones.FirstOrDefault(_zone => _zone.KeyName?.Equals(Zone.Hand) == true);
        #region 转换技能
        if (skill is ISkillCardConverter)
        {
            ISkillCardConverter conv = (ISkillCardConverter)(skill);
            #region 多选转换技能
            if (conv is ISkillCardMultiConverter)
            {
                ISkillCardMultiConverter multi = (ISkillCardMultiConverter)(conv);
                // 可选的卡的列表。
                List<Card> cardlist = new List<Card>();
                // 出牌阶段，仅能选择从可以使用的牌类。
                #region 出牌阶段
                if (Status == Enum_GameBoardAction.CardUsing)
                {
                    // 枚举每种卡。
                    foreach (string cardtype in multi.GetCardTypes(ctx))
                    {
                        // 创建这个种类的虚拟卡，放置到手牌建立虚拟场景，检验其是否可以使用。
                        Card cardinst = App.World.GetCardInstance(cardtype);
                        if (cardinst == null) continue;
                        cardinst = cardinst.Clone();
                        bool canuse = false;
                        using (ZoneLock zoneenv = new ZoneLock(cardinst, hand))
                        {
                            if (multi is ISkillCardConverterMark)
                                ((ISkillCardConverterMark)multi).Mark(ctx, cardinst);
                            ConditionFilter condition = cardinst.UseCondition;
                            if (condition != null) condition = App.World.TryReplaceNewCondition(condition, null);
                            canuse = condition?.Accept(ctx) == true;
                        }
                        if (canuse) cardlist.Add(cardinst);
                    }
                }
                #endregion
                // 响应阶段，根据情况来定。
                #region 响应阶段
                else
                {
                    CardFilter cardfilter = GetSelectCardFilter();
                    // 响应的卡牌筛选器有要求列表，求两个列表的交集。
                    if (cardfilter is ICardFilterRequiredCardTypes)
                    {
                        ICardFilterRequiredCardTypes required = (ICardFilterRequiredCardTypes)cardfilter;
                        // 枚举每种卡。
                        foreach (string cardtype in multi.GetCardTypes(ctx))
                        {
                            // 不在要求列表中。
                            if (!required.RequiredCardTypes.Contains(cardtype)) continue;
                            // 加入到可选列表中。
                            cardlist.Add(App.World.GetCardInstance(cardtype));
                        }
                    }
                    // 未知响应，假设可以使用这个技能。
                    else
                    {
                        SetSkillConvert(conv, null);
                        return;
                    }
                }
                #endregion
                #region 直接选择/使用于吉面板
                // 没有可用的卡的种类，本次选择无效。
                if (cardlist.Count() == 0)
                {
                    UI_Skills.SetUncheck(skill);
                    return;
                }
                // 仅一个，设置此卡并开始转换。
                if (cardlist.Count() == 1)
                {
                    multi.SetSelectedCardType(ctx, cardlist[0].KeyName);
                    if (skillconvert != null)
                        UI_Skills.SetUncheck(skillconvert as Skill);
                    SetSkillConvert(conv, null);
                    return;
                }
                // 多种可选，使用于吉面板来选择。
                UI_Yuji.Skill = skill;
                UI_Yuji.SkillFromCard = null;
                UI_Yuji.CardList = cardlist;
                App.Show(UI_Yuji);
                return;
                #endregion
            }
            #endregion
            #region 普通转换技能
            SetSkillConvert(conv, null);
            #endregion
        }
        #endregion
        #region 初发技能
        else if (skill is ISkillInitative)
        {
            ISkillInitative init = (ISkillInitative)(skill);
            if (skillinitative != null)
                UI_Skills.SetUncheck(skillinitative as Skill);
            SetSkillInitative(init, null);
        }
        #endregion
    }

    public void UI_Skills_SkillUnchecked(Skill skill)
    {
        if (skill == UI_Yuji.Skill)
            App.Hide(UI_Yuji);
        if (skill == skillconvert)
            CancelSkillConvert();
        if (skill == skillinitative)
            CancelSkillInitative();
    }

    #endregion

    #region UI_Yuji

    public void UI_Yuji_Click(Card card)
    {
        ISkillCardMultiConverter multi = UI_Yuji.Skill as ISkillCardMultiConverter;
        Card fromcard = UI_Yuji.SkillFromCard;
        if (multi == null) return;
        App.Hide(UI_Yuji);
        multi.SetSelectedCardType(App.World.GetContext(), card.KeyName);
        SetSkillConvert(multi, fromcard);
    }

    #endregion

    #region UI_OutputBox

    public void UI_OutputBox_CommentEnter()
    {

    }

    public void UI_OutputBox_SwitchEmojiBox()
    {

    }

    #endregion

    #region UI_Emoji

    public void UI_Emoji_EmojiClick()
    {

    }

    #endregion

    #region CV_HandMatch

    private void BN_MyFist_MouseDown()
    {
        handgesture = HandMatchGesture.Fist;
        console.WorldContiune();
    }

    private void BN_MyCut_MouseDown()
    {
        handgesture = HandMatchGesture.Cut;
        console.WorldContiune();
    }

    private void BN_MyCloth_MouseDown()
    {
        handgesture = HandMatchGesture.Cloth;
        console.WorldContiune();
    }

    #endregion

    #endregion

}

public class GameBoardConsole : IPlayerConsole
{
    public GameBoardConsole(GameBoard _parent, Player _controller)
    {
        this.parent = _parent;
        this.controller = _controller;
        this.worldstop = new AutoResetEvent(false);
    }

    #region Number

    private GameBoard parent;
    public GameBoard Parent
    {
        get { return this.parent; }
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

    #endregion

    #region Method

    protected void WaitAndDispatch(Action action)
    {
        action?.Invoke();
        worldstop.WaitOne();
    }

    internal void WorldContiune()
    {
        worldstop.Set();
    }

    #endregion

    #region IPlayerConsole

    bool IPlayerConsole.Ask(Context ctx, string keyname, string message, int timeout)
    {
        WaitAndDispatch(() => { parent.BeginAsk(message); });
        return parent.GetAskedResult();
    }

    void IPlayerConsole.SelectCharactor(SelectCharactorBoardCore core)
    {
        WaitAndDispatch(() => { parent.BeginCharactorSelect(core); });
    }

    void IPlayerConsole.SelectCards(SelectCardBoardCore core)
    {
        WaitAndDispatch(() =>
        {
            //Trace.WriteLine(String.Format("[{0}:{1:d3}]{2}", DateTime.Now.ToLongTimeString(), DateTime.Now.Millisecond, core.Message));
            parent.BeginCardSelect(core);
        });
    }

    void IPlayerConsole.SelectPlayers(SelectPlayerBoardCore core)
    {
        WaitAndDispatch(() => { parent.BeginPlayerSelect(core); });
    }

    void IPlayerConsole.SelectDesktop(DesktopCardBoardCore core)
    {
        WaitAndDispatch(() => { parent.ShowDesktopBoard(core); });
        /*
        if (!core.IsAsync)
        {
            bool isyes = false;
            core.SelectedCards.Clear();
            core.SelectedCards.AddRange(parent.GetSelectedCards(out isyes));
            core.IsYes = isyes;
        }
        */
    }

    void IPlayerConsole.ControlDesktop(DesktopCardBoardCore core)
    {
        WaitAndDispatch(() => { parent.ControlDesktopBoard(core); });
        //bool isyes = false;
        //core.SelectedCards.Clear();
        //core.SelectedCards.AddRange(parent.GetSelectedCards(out isyes));
        //core.IsYes = isyes;
    }

    void IPlayerConsole.CloseDesktop(DesktopCardBoardCore core)
    {
        parent.CloseDesktopBoard(core);
    }

    void IPlayerConsole.SelectList(ListBoardCore core)
    {
        WaitAndDispatch(() => { parent.BeginListSelect(core); });
    }

    void IPlayerConsole.BreakLastAction()
    {
        parent.BreakLastAction();
    }

    TouhouSha.Core.Event IPlayerConsole.QuestEventInUseCardState(Context ctx)
    {
        WaitAndDispatch(() => { parent.BeginUseCardState(ctx); });
        return parent.GetEventInUseCardState();
    }

    HandMatchGesture IPlayerConsole.HandMatch()
    {
        WaitAndDispatch(() => { parent.BeginHandMatch(); });
        return parent.GetHandMatchResult();
    }

    void IPlayerConsole.OpenKOF(KOFSelectCore core)
    {
        WaitAndDispatch(() => { parent.ShowKOF(core); });
    }

    void IPlayerConsole.ControlKOF(KOFSelectCore core)
    {
        WaitAndDispatch(() => { parent.ControlKOF(core); });
    }

    void IPlayerConsole.CloseKOF(KOFSelectCore core)
    {
        WaitAndDispatch(() => { parent.CloseKOF(core); });
    }

    #endregion
}

public abstract class PlayerRegister : IPlayerRegister
{
    public abstract IPlayerConsole CreateConsole(Player player);
}

public class CurrentPlayerRegister : PlayerRegister
{
    public CurrentPlayerRegister(GameBoard _gameboard)
    {
        this.gameboard = _gameboard;
    }

    private GameBoard gameboard;

    public override IPlayerConsole CreateConsole(Player player)
    {
        return gameboard.Console = new GameBoardConsole(gameboard, player);
    }
}

public class AIPlayerRegister : PlayerRegister
{
    public override IPlayerConsole CreateConsole(Player player)
    {
        return new AIConsole(player);
    }
}

public class RemotePlayerRegister : PlayerRegister
{
    public RemotePlayerRegister(GameBoard _gameboard, string _punid)
    {
        this.gameboard = _gameboard;
        this.punid = _punid;
    }

    public override IPlayerConsole CreateConsole(Player player)
    {
        RemoteConsole console =  new RemoteConsole(player, punid);
        gameboard.RemoteConsoles.Add(console);
        gameboard.ConnectRemoteConsolesAsync();
        return console;
    }

    private GameBoard gameboard;

    private string punid;
    public string PunId => punid;
}

public enum Enum_GameBoardAction
{
    None,
    CharactorSelecting,
    CardUsing,
    CardSelecting,
    PlayerSelecting,
    PlayerAndCardSelecting,
    ListSelecting,
    Asking,
    HandMatching,
    KOFCharactorSelecting,
}

public enum Enum_ScreenCoolLabel
{
    GameStart,
    FirstBlood,
    DoubleKill,
    TripleKill,
    QuadraKill,
    PentaKill,
    Rampage,
    Godlike,
}

public interface IGameBoardArea
{
    bool KeptCards { get; }
    IList<Card> Cards { get; }
    Vector3? GetExpectedPosition(Card card);
}

public abstract class DispatcherTask
{
    public DispatcherTask(bool _worldwaiting)
    {
        this.worldwaiting = _worldwaiting;
    }

    private bool worldwaiting = false;
    public bool WorldWaiting { get { return this.worldwaiting; } }
}

public class DispatcherUIEvent : DispatcherTask
{
    public DispatcherUIEvent(UIEvent _ev) : base(true)
    {
        this.ev = _ev;
    }

    private UIEvent ev;
    public UIEvent Ev { get { return this.ev; } }
}

public class DispatcherGameStartup : DispatcherTask
{
    public DispatcherGameStartup(GameStartupEventArgs _ev) : base(true)
    {
        this.ev = _ev;
    }

    private GameStartupEventArgs ev;
    public GameStartupEventArgs Ev { get { return this.ev; } }
}

public class DispatcherPostComment : DispatcherTask
{
    public DispatcherPostComment(PlayerCommentEventArgs _ev) : base(false)
    {
        this.ev = _ev;
    }

    private PlayerCommentEventArgs ev;
    public PlayerCommentEventArgs Ev { get { return this.ev; } }

}

public class DispatcherAsk : DispatcherTask
{
    public DispatcherAsk(string _message) : base(false)
    {
        this.message = _message;
    }

    private string message;
    public string Message { get { return this.message; } }
}

public class DispatcherSelectCharactor : DispatcherTask
{
    public DispatcherSelectCharactor(SelectCharactorBoardCore _core) : base(false)
    {
        this.core = _core;
    }

    private SelectCharactorBoardCore core;
    public SelectCharactorBoardCore Core { get { return this.core; } }
}

public class DispatcherSelectCard : DispatcherTask
{
    public DispatcherSelectCard(SelectCardBoardCore _core) : base(false)
    {
        this.core = _core;
    }

    private SelectCardBoardCore core;
    public SelectCardBoardCore Core { get { return this.core; } }
}

public class DispatcherSelectPlayer : DispatcherTask
{
    public DispatcherSelectPlayer(SelectPlayerBoardCore _core) : base(false)
    {
        this.core = _core;
    }

    private SelectPlayerBoardCore core;
    public SelectPlayerBoardCore Core { get { return this.core; } }
}

public class DispatcherSelectPlayerAndCard : DispatcherTask
{
    public DispatcherSelectPlayerAndCard(SelectPlayerAndCardBoardCore _core) : base(false)
    {
        this.core = _core;
    }

    private SelectPlayerAndCardBoardCore core;
    public SelectPlayerAndCardBoardCore Core { get { return this.core; } }
}

public class DispatcherSelectDesktop : DispatcherTask
{
    public enum Enum_Action
    {
        Open,
        Control,
        Close,
    }

    public DispatcherSelectDesktop(DesktopCardBoardCore _core, Enum_Action _action) : base(false)
    {
        this.core = _core;
        this.action = _action;
    }

    private DesktopCardBoardCore core;
    public DesktopCardBoardCore Core { get { return this.core; } }

    private Enum_Action action;
    public Enum_Action Action { get { return this.action; } }
}

public class DispatcherSelectList : DispatcherTask
{
    public DispatcherSelectList(ListBoardCore _core) : base(false)
    {
        this.core = _core;
    }

    private ListBoardCore core;
    public ListBoardCore Core { get { return this.core; } }
}

public class DispatcherBreakAction : DispatcherTask
{
    public DispatcherBreakAction() : base(false)
    {

    }
}

public class DispatcherQuestUseCard : DispatcherTask
{
    public DispatcherQuestUseCard() : base(false)
    {
        
    }
}

public class DispatcherHandMatch : DispatcherTask
{
    public DispatcherHandMatch() : base(false)
    {

    }
}

public class DispatcherSelectKOF : DispatcherTask
{
    public enum Enum_Action
    {
        Open,
        Control,
        Close,
    }

    public DispatcherSelectKOF(KOFSelectCore _core, Enum_Action _action) : base(false)
    {
        this.core = _core;
        this.action = _action;
    }

    private KOFSelectCore core;
    public KOFSelectCore Core { get { return this.core; } }

    private Enum_Action action;
    public Enum_Action Action { get { return this.action; } }
    
}

